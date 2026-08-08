# -*- coding: utf-8 -*-
"""IStep.Apply(StepBuilder) -> Apply(SQLBuilder); steps hit Inner; drop double Attach."""
from __future__ import print_function
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "pure" / "src" / "ado" / "builder"
STEPS = BUILDER / "steps"


def rewrite_istep():
    (STEPS / "IStep.cs").write_text(
        '''namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder 编排步骤：携带一次 public API 调用的参数，在 Flush 时作用于门面，
    /// 步骤实现通过 <see cref="SQLBuilder.Inner"/> 写入内核，避免构造 API 重入入队。
    /// </summary>
    public interface IStep
    {
        /// <summary>将本步骤应用到编排门面（内部写 <see cref="SQLBuilder.Inner"/>）。</summary>
        void Apply(SQLBuilder builder);
    }
}
''',
        encoding="utf-8",
    )
    print("IStep")


def rewrite_simple_steps():
    for path in STEPS.rglob("*.cs"):
        if path.name == "IStep.cs":
            continue
        text = path.read_text(encoding="utf-8")
        orig = text

        # Expression-bodied simple Apply
        text = re.sub(
            r"public void Apply\(StepBuilder builder\)\s*=>\s*builder\.",
            "public void Apply(SQLBuilder builder) => builder.Inner.",
            text,
        )

        # Block Apply that still has Attach wrapper -> simplify later
        text = text.replace(
            "public void Apply(StepBuilder builder)",
            "public void Apply(SQLBuilder builder)",
        )

        if text != orig:
            path.write_text(text, encoding="utf-8")
            print("step", path.relative_to(STEPS))


def simplify_action_steps():
    """Replace Attach adapter bodies with direct Inner + Action<SQLBuilder>."""
    for path in STEPS.rglob("*Act*.cs"):
        text = path.read_text(encoding="utf-8")
        if "SQLBuilder.Attach" not in text and "builder.Inner." not in text:
            # may already be expression-bodied to Inner from rewrite_simple
            pass

        # Pattern: Apply block with Attach
        def repl_block(m):
            indent = m.group(1)
            body = m.group(2)
            # Find builder.METHOD( ... inner => { Attach... } )
            mm = re.search(
                r"builder\.(?:Inner\.)?(\w+)(?:<[^>]+>)?\((.*)\);\s*$",
                body.strip(),
                re.S,
            )
            if not mm:
                # try multi-line builder.method(
                mm = re.search(
                    r"builder\.(?:Inner\.)?(\w+)(?:<[^>]+>)?\(\s*(.*?)\s*\)\s*;\s*$",
                    body.strip(),
                    re.S,
                )
            if not mm:
                return m.group(0)
            method = mm.group(1)
            args = mm.group(2).strip()
            # Extract action field from Attach block
            am = re.search(
                r"(\w+)\(facade\);\s*\n\s*facade\.EnsureMaterialized\(\);",
                args,
            )
            same = re.search(
                r"(\w+)\(facade\);\s*\n\s*\}\s*\)",
                args,
            ) and "EnsureMaterialized" not in args

            # Rebuild args: replace the whole lambda with the action field name
            field = None
            fm = re.search(r"(\_\w+)\(facade\)", args)
            if fm:
                field = fm.group(1)
            if not field:
                return m.group(0)

            # prefix args before lambda
            # split by top-level comma before "inner =>"
            idx = args.find("inner =>")
            if idx < 0:
                idx = args.find("inner=>")
            if idx > 0:
                prefix = args[:idx].rstrip().rstrip(",").strip()
                if prefix:
                    call = f"builder.Inner.{method}({prefix}, {field});"
                else:
                    call = f"builder.Inner.{method}({field});"
            else:
                call = f"builder.Inner.{method}({field});"

            return (
                f"{indent}public void Apply(SQLBuilder builder) => {call}"
            )

        new_text = re.sub(
            r"([ \t]*)public void Apply\(SQLBuilder builder\)\s*\{(.*?)\n[ \t]*\}",
            repl_block,
            text,
            flags=re.S,
        )
        if new_text != text:
            path.write_text(new_text, encoding="utf-8")
            print("simplified", path.name)
        elif "Apply(SQLBuilder builder)" in text and "builder." in text and "builder.Inner." not in text:
            # block Apply that calls builder.xxx without Inner — fix
            t2 = re.sub(
                r"builder\.(?!Inner\.)",
                "builder.Inner.",
                text,
            )
            # but don't change SQLBuilder.Attach etc - already gone
            if t2 != text:
                path.write_text(t2, encoding="utf-8")
                print("innerized", path.name)


def rewrite_action_steps_manual():
    """Hand-write known Action step files cleanly."""
    specs = {
        "where/WhereActStep.cs": (
            "WhereActStep",
            "Action<SQLBuilder> _whereBuilder",
            "whereBuilder",
            "builder.Inner.where(_whereBuilder);",
        ),
        "where/WhereORActStep.cs": (
            "WhereORActStep",
            "Action<SQLBuilder> _whereBuilder",
            "whereBuilder",
            "builder.Inner.whereOR(_whereBuilder);",
        ),
        "where/AndActStep.cs": (
            "AndActStep",
            "Action<SQLBuilder> _doSomeWhere",
            "doSomeWhere",
            "builder.Inner.and(_doSomeWhere);",
        ),
        "where/OrActStep.cs": (
            "OrActStep",
            "Action<SQLBuilder> _doSomeWhere",
            "doSomeWhere",
            "builder.Inner.or(_doSomeWhere);",
        ),
        "where/WhereExistActStep.cs": (
            "WhereExistActStep",
            "Action<SQLBuilder> _doselect",
            "doselect",
            "builder.Inner.whereExist(_doselect);",
        ),
        "where/WhereNotExistActStep.cs": (
            "WhereNotExistActStep",
            "Action<SQLBuilder> _doselect",
            "doselect",
            "builder.Inner.whereNotExist(_doselect);",
        ),
        "where/WhereInstringActStep.cs": (
            "WhereInstringActStep",
            None,
            None,
            "builder.Inner.whereIn(_key, _doselect);",
            "string _key; Action<SQLBuilder> _doselect",
            "string key, Action<SQLBuilder> doselect",
            "_key = key; _doselect = doselect;",
        ),
        "where/WhereNotInstringActStep.cs": (
            "WhereNotInstringActStep",
            None,
            None,
            "builder.Inner.whereNotIn(_key, _doselect);",
            "string _key; Action<SQLBuilder> _doselect",
            "string key, Action<SQLBuilder> doselect",
            "_key = key; _doselect = doselect;",
        ),
        "where/WherestringActStep.cs": (
            "WherestringActStep",
            None,
            None,
            "builder.Inner.where(_key, _doselect);",
            "string _key; Action<SQLBuilder> _doselect",
            "string key, Action<SQLBuilder> doselect",
            "_key = key; _doselect = doselect;",
        ),
        "where/WherestringstringActStep.cs": (
            "WherestringstringActStep",
            None,
            None,
            "builder.Inner.where(_key, _op, _doselect);",
            "string _key; string _op; Action<SQLBuilder> _doselect",
            "string key, string op, Action<SQLBuilder> doselect",
            "_key = key; _op = op; _doselect = doselect;",
        ),
        "select/SelectstringActStep.cs": (
            "SelectstringActStep",
            None,
            None,
            "builder.Inner.select(_asName, _doColSelect);",
            "string _asName; Action<SQLBuilder> _doColSelect",
            "string asName, Action<SQLBuilder> doColSelect",
            "_asName = asName; _doColSelect = doColSelect;",
        ),
        "select/SelectWithActStep.cs": (
            "SelectWithActStep",
            "Action<SQLBuilder> _queryOther",
            "queryOther",
            "builder.Inner.selectWith(_queryOther);",
        ),
        "from/FromstringActStep.cs": (
            "FromstringActStep",
            None,
            None,
            "builder.Inner.from(_asName, _childFromPart);",
            "string _asName; Action<SQLBuilder> _childFromPart",
            "string asName, Action<SQLBuilder> childFromPart",
            "_asName = asName; _childFromPart = childFromPart;",
        ),
        "from/LeftJoinstringActStep.cs": (
            "LeftJoinstringActStep",
            None,
            None,
            "builder.Inner.leftJoin(_joinSQLString, _childFromPart);",
            "string _joinSQLString; Action<SQLBuilder> _childFromPart",
            "string joinSQLString, Action<SQLBuilder> childFromPart",
            "_joinSQLString = joinSQLString; _childFromPart = childFromPart;",
        ),
        "from/RightJoinstringActStep.cs": (
            "RightJoinstringActStep",
            None,
            None,
            "builder.Inner.rightJoin(_joinSQLString, _childFromPart);",
            "string _joinSQLString; Action<SQLBuilder> _childFromPart",
            "string joinSQLString, Action<SQLBuilder> childFromPart",
            "_joinSQLString = joinSQLString; _childFromPart = childFromPart;",
        ),
        "from/InnerJoinstringActStep.cs": (
            "InnerJoinstringActStep",
            None,
            None,
            "builder.Inner.innerJoin(_joinSQLString, _childFromPart);",
            "string _joinSQLString; Action<SQLBuilder> _childFromPart",
            "string joinSQLString, Action<SQLBuilder> childFromPart",
            "_joinSQLString = joinSQLString; _childFromPart = childFromPart;",
        ),
        "from/JoinstringstringActStep.cs": (
            "JoinstringstringActStep",
            None,
            None,
            "builder.Inner.join(_joinKey, _joinSQLString, _childFromPart);",
            "string _joinKey; string _joinSQLString; Action<SQLBuilder> _childFromPart",
            "string joinKey, string joinSQLString, Action<SQLBuilder> childFromPart",
            "_joinKey = joinKey; _joinSQLString = joinSQLString; _childFromPart = childFromPart;",
        ),
        "merge/MergeUsingstringActStep.cs": (
            "MergeUsingstringActStep",
            None,
            None,
            "builder.Inner.mergeUsing(_asName, _buildSelect);",
            "string _asName; Action<SQLBuilder> _buildSelect",
            "string asName, Action<SQLBuilder> buildSelect",
            "_asName = asName; _buildSelect = buildSelect;",
        ),
        "union/WithSelectstringActStep.cs": (
            "WithSelectstringActStep",
            None,
            None,
            "builder.Inner.withSelect(_name, _doselect);",
            "string _name; Action<SQLBuilder> _doselect",
            "string name, Action<SQLBuilder> doselect",
            "_name = name; _doselect = doselect;",
        ),
        "union/WithAsstringActStep.cs": (
            "WithAsstringActStep",
            None,
            None,
            "builder.Inner.withAs(_name, _selectBuilder);",
            "string _name; Action<SQLBuilder> _selectBuilder",
            "string name, Action<SQLBuilder> selectBuilder",
            "_name = name; _selectBuilder = selectBuilder;",
        ),
        "union/UnionActStep.cs": (
            "UnionActStep",
            "Action<SQLBuilder> _doUnion",
            "doUnion",
            "builder.Inner.union(_doUnion);",
        ),
        "union/UnionAsAction_SqlGoupStep.cs": (
            "UnionAsAction_SqlGoupStep",
            "Action<SqlGoup> _dogroup",
            "dogroup",
            "builder.Inner.unionAs(_dogroup);",
        ),
        "union/WithRecurstringAction_RecurCTEBuilderStep.cs": (
            "WithRecurstringAction_RecurCTEBuilderStep",
            None,
            None,
            "builder.Inner.withRecur(_name, _buildRecur);",
            "string _name; Action<RecurCTEBuilder> _buildRecur",
            "string name, Action<RecurCTEBuilder> buildRecur",
            "_name = name; _buildRecur = buildRecur;",
        ),
    }

    for rel, spec in specs.items():
        name = spec[0]
        apply_line = spec[3]
        if spec[1] is not None:
            field_decl = f"private readonly {spec[1]};"
            ctor_param = spec[1].replace("Action<SQLBuilder> ", "Action<SQLBuilder> ").replace("Action<SqlGoup> ", "Action<SqlGoup> ")
            # field like "Action<SQLBuilder> _whereBuilder"
            field_name = spec[1].split()[-1]
            param_name = spec[2]
            param_type = " ".join(spec[1].split()[:-1])
            content = f'''using System;

namespace mooSQL.data
{{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class {name} : IStep
    {{
        private readonly {param_type} {field_name};

        public {name}({param_type} {param_name})
        {{
            {field_name} = {param_name};
        }}

        public void Apply(SQLBuilder builder) => {apply_line}
    }}
}}
'''
        else:
            fields = spec[4]
            ctor_params = spec[5]
            ctor_body = spec[6]
            field_lines = "\n        ".join(
                f"private readonly {f.strip()};" for f in fields.split(";") if f.strip()
            )
            assign_lines = "\n            ".join(
                a.strip() for a in ctor_body.split(";") if a.strip()
            )
            content = f'''using System;

namespace mooSQL.data
{{
    /// <summary>对应 SQLBuilder 编排步骤。</summary>
    public sealed class {name} : IStep
    {{
        {field_lines}

        public {name}({ctor_params})
        {{
            {assign_lines}
        }}

        public void Apply(SQLBuilder builder) => {apply_line}
    }}
}}
'''
        path = STEPS / rel
        path.write_text(content, encoding="utf-8")
        print("wrote", rel)


def fix_facade():
    # EnsureMaterialized + Enqueue
    p = BUILDER / "SQLBuilder.cs"
    t = p.read_text(encoding="utf-8")
    t = t.replace("_steps[i].Apply(_inner);", "_steps[i].Apply(this);")
    # Remove ToKitAction helper
    t = re.sub(
        r"\s*/// <summary>内核 Action 适配到 Kit.*?</summary>\s*"
        r"internal static System\.Action<SQLBuilder> ToKitAction\(System\.Action<StepBuilder> act\)\s*"
        r"\{\s*if \(act == null\) return null;\s*return facade => act\(facade\.Inner\);\s*\}\s*",
        "\n",
        t,
        flags=re.S,
    )
    p.write_text(t, encoding="utf-8")
    print("SQLBuilder.cs")

    p = BUILDER / "SQLBuilder.defer.cs"
    t = p.read_text(encoding="utf-8")
    t = t.replace("step.Apply(_inner);", "step.Apply(this);")
    t = t.replace("走内核，不重入门面", "写 Inner，不重入入队")
    p.write_text(t, encoding="utf-8")
    print("defer.cs")


def fix_stepbuilder_actions():
    """Action<StepBuilder> -> Action<SQLBuilder>; brother call sites Attach once."""
    for path in BUILDER.glob("StepBuilder*.cs"):
        t = path.read_text(encoding="utf-8")
        orig = t
        t = t.replace("Action<StepBuilder>", "Action<SQLBuilder>")
        t = t.replace("SQLBuilder.ToKitAction(", "(")
        # broken: current.where((doselect)) from ToKitAction removal if was ToKitAction(x) -> (x) OK
        # current.whereIn(key, (doselect)) 
        t = t.replace("current.whereIn(key, (", "current.whereIn(key, ")
        t = t.replace("current.where(key, \" NOT IN \", (", "current.where(key, \" NOT IN \", ")
        t = t.replace("current.whereExist((", "current.whereExist(")
        t = t.replace("current.where(\"\", \" NOT EXISTS \", (", "current.where(\"\", \" NOT EXISTS \", ")
        t = t.replace("current.where(key, op, (", "current.where(key, op, ")
        t = t.replace("current.where(key, \"=\", (", "current.where(key, \"=\", ")
        t = t.replace("current.where((", "current.where(")
        # fix extra closing parens from ToKitAction(x) -> (x) leaving ))
        # e.g. current.where(whereBuilder)); 
        t = re.sub(
            r"current\.(whereIn|whereExist|where)\(([^;]*?)\)\);",
            lambda m: f"current.{m.group(1)}({m.group(2)});",
            t,
        )

        if t != orig:
            path.write_text(t, encoding="utf-8")
            print("actions", path.name)

    # Patch brother Action invocations to Attach once
    patch_brother_calls()


def invoke_facade(bro_expr, action_expr, materializing=False):
    if materializing:
        return (
            f"{{\n"
            f"                var __facade = SQLBuilder.Attach({bro_expr}, materializing: true);\n"
            f"                {action_expr}(__facade);\n"
            f"            }}"
        )
    return (
        f"{{\n"
        f"                var __facade = SQLBuilder.Attach({bro_expr});\n"
        f"                {action_expr}(__facade);\n"
        f"                __facade.EnsureMaterialized();\n"
        f"            }}"
    )


def patch_brother_calls():
    """StepBuilder methods that call Action on StepBuilder brother/self."""
    # Select
    p = BUILDER / "StepBuilderSelect.cs"
    t = p.read_text(encoding="utf-8")

    replacements = [
        (
            """            var kit = this.getBrotherBuilder();
            doselect(kit);""",
            """            var kit = this.getBrotherBuilder();
            {
                var __facade = SQLBuilder.Attach(kit);
                doselect(__facade);
                __facade.EnsureMaterialized();
            }""",
        ),
        (
            """            var ckit = this.getBrotherBuilder();
            doColSelect(ckit);""",
            """            var ckit = this.getBrotherBuilder();
            {
                var __facade = SQLBuilder.Attach(ckit);
                doColSelect(__facade);
                __facade.EnsureMaterialized();
            }""",
        ),
        (
            """            var ckit = this.getBrotherBuilder();
            childFromPart(ckit);""",
            """            var ckit = this.getBrotherBuilder();
            {
                var __facade = SQLBuilder.Attach(ckit);
                childFromPart(__facade);
                __facade.EnsureMaterialized();
            }""",
        ),
        (
            """            doUnion(this);""",
            """            {
                var __facade = SQLBuilder.Attach(this, materializing: true);
                doUnion(__facade);
            }""",
        ),
    ]
    for a, b in replacements:
        if a in t:
            t = t.replace(a, b)
            print("patched select brother")
        else:
            print("MISS select", a[:50])
    p.write_text(t, encoding="utf-8")

    # Where OR / and / or actions
    p = BUILDER / "StepBuilderWhere.cs"
    t = p.read_text(encoding="utf-8")
    # whereOR
    old = """            var bro = this.getBrotherBuilder();
            bro.or();
            whereBuilder(bro);
            var t = bro.buildWhereContent();"""
    new = """            var bro = this.getBrotherBuilder();
            bro.or();
            {
                var __facade = SQLBuilder.Attach(bro);
                whereBuilder(__facade);
                __facade.EnsureMaterialized();
            }
            var t = bro.buildWhereContent();"""
    if old in t:
        t = t.replace(old, new)
        print("whereOR")
    else:
        print("MISS whereOR")

    # or(Action) / and(Action) - read patterns
    t2 = re.sub(
        r"(doSomeWhere)\(this\);",
        r"{ var __facade = SQLBuilder.Attach(this, materializing: true); \1(__facade); }",
        t,
    )
    if t2 != t:
        t = t2
        print("and/or self")

    # Clean any leftover ToKitAction
    t = t.replace("SQLBuilder.ToKitAction", "/*removed*/")
    if "/*removed*/" in t:
        print("WARNING leftover ToKitAction markers")
        t = re.sub(r"/\*removed\*/\(([^)]+)\)", r"\1", t)

    p.write_text(t, encoding="utf-8")

    # Save mergeUsing
    p = BUILDER / "StepBuilderSave.cs"
    t = p.read_text(encoding="utf-8")
    if "buildSelect(" in t and "Attach" not in t:
        # find mergeUsing body
        t2 = re.sub(
            r"(buildSelect)\((\w+)\);",
            r"{ var __facade = SQLBuilder.Attach(\2); \1(__facade); __facade.EnsureMaterialized(); }",
            t,
        )
        if t2 != t:
            p.write_text(t2, encoding="utf-8")
            print("mergeUsing")

    # Dymatic selectWith
    p = BUILDER / "StepBuilderDymatic.cs"
    t = p.read_text(encoding="utf-8")
    if "queryOther(" in t and "Action<SQLBuilder>" in t:
        # likely queryOther(this) or similar - check
        pass


def fix_withas_and_remaining():
    p = BUILDER / "StepBuilderSelect.cs"
    t = p.read_text(encoding="utf-8")
    # withAs may call selectBuilder on something
    # withSelect already patched
    # leftJoin(string, Action) may delegate to join
    p.write_text(t, encoding="utf-8")


def update_generator():
    p = ROOT / "tools" / "gen_sqlbuilder_steps.py"
    if not p.exists():
        return
    t = p.read_text(encoding="utf-8")
    t2 = t.replace(
        "void Apply(StepBuilder builder)",
        "void Apply(SQLBuilder builder)",
    )
    t2 = t2.replace(
        "=> builder.",
        "=> builder.Inner.",
    )
    # careful - might break other things in generator templates
    if "Apply(SQLBuilder builder) => builder.Inner." not in t2:
        t2 = t.replace(
            'f"        public void Apply(StepBuilder builder) => builder.{{name}}',
            'f"        public void Apply(SQLBuilder builder) => builder.Inner.{{name}}',
        )
        # try common template patterns
        t2 = re.sub(
            r"Apply\(StepBuilder builder\) => builder\.",
            "Apply(SQLBuilder builder) => builder.Inner.",
            t,
        )
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("updated generator")


def main():
    rewrite_istep()
    rewrite_simple_steps()
    rewrite_action_steps_manual()
    fix_facade()
    fix_stepbuilder_actions()
    update_generator()
    print("DONE")


if __name__ == "__main__":
    main()
