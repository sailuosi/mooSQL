# -*- coding: utf-8 -*-
"""Generate IStep classes + SQLBuilder.defer.api.cs for simple (non-Action) fluent APIs."""
from __future__ import print_function
import re
from pathlib import Path
from collections import OrderedDict

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "pure" / "src" / "ado" / "builder"
STEPS = BUILDER / "steps"
DEFER_API = BUILDER / "SQLBuilder.defer.api.cs"

# Methods already hand-written in SQLBuilder.defer.cs / existing steps
SKIP_METHODS = {
    ("select", ("string",)),
    ("from", ("string",)),
    ("distinct", ()),
    ("orderBy", ("string",)),
    ("setPage", ("int?", "int?")),
    ("where", ("string",)),
    ("where", ("string", "object")),
    ("where", ("string", "object", "string")),
    ("where", ("string", "object", "string", "bool")),
    # A 类同实例 Action：门面编排期展开（见 SQLBuilder.defer.cs），不生成 ActStep
    ("selectWith", ("Action<SQLBuilder>",)),
    ("mergeUsing", ("string", "Action<SQLBuilder>")),
    ("or", ("Action<SQLBuilder>",)),
    ("and", ("Action<SQLBuilder>",)),
}

# Names that are meta/config/exec — do not generate construction steps
SKIP_NAMES = {
    "clear", "reset", "clearSelect", "clearWhere", "clearPage",
    "copy", "getBrotherBuilder", "useSQL", "useDDL", "useSentence",
    "print", "configClear", "useSignal", "resetSignal", "setPosition",
    "setDBInstance", "setCache", "setCacheHolder", "setSeed",
    "beginTransaction", "useTransaction", "useDeferred",
    "record", "stop", "toApart", "useApart",
    "useMaster", "useReadReplica", "useReadPolicy", "useDualWrite",
    "useFailover", "useRoute", "resetRoute", "useTarget",
    "mergeInto", "withRecurTo", "start",  # return other types
    "addInsert", "addUpdate", "addUpdateFrom",  # side-effect pool
    "popPreWhere",
}

TYPE_ALIAS = {
    "Object": "object",
    "String": "string",
    "Boolean": "bool",
    "Int32": "int",
    "Int64": "long",
}


def normalize_type(t: str) -> str:
    t = " ".join(t.replace("?", " ?").split()).replace(" ?", "?")
    t = t.replace("params ", "")
    for a, b in TYPE_ALIAS.items():
        t = re.sub(r"\b" + a + r"\b", b, t)
    return t


def parse_params(param_src: str):
    if not param_src.strip():
        return []
    # split by comma respecting generics/arrays — simple approach
    parts = []
    depth = 0
    cur = []
    for ch in param_src:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        if ch == "," and depth == 0:
            parts.append("".join(cur).strip())
            cur = []
        else:
            cur.append(ch)
    if cur:
        parts.append("".join(cur).strip())

    result = []
    for p in parts:
        if not p:
            continue
        is_params = p.startswith("params ")
        p = re.sub(r"^params\s+", "", p)
        default = None
        if "=" in p:
            # split default (respecting quotes roughly)
            mdef = re.match(r"^(.+?)\s*=\s*(.+)$", p)
            if mdef:
                p = mdef.group(1).strip()
                default = mdef.group(2).strip()
        # last token is name
        tokens = p.split()
        if len(tokens) < 2:
            continue
        name = tokens[-1].strip(",")
        typ = normalize_type(" ".join(tokens[:-1]))
        result.append((typ, name, is_params, default))
    return result


def sig_key(name, params):
    return (name, tuple(p[0] for p in params))


def param_types(params):
    return tuple(p[0] for p in params)


def family_dir(name: str) -> str:
    if name.startswith("where") or name in ("and", "or", "not", "sink", "rise", "pin", "sinkOR", "sinkNot", "sinkNotOR"):
        return "where"
    if name.startswith("set") or name in ("newRow", "addRow", "configSetNull", "setToNull", "setI", "setU", "setTable"):
        return "set"
    if name.startswith("join") or name.endswith("Join") or name in ("from", "fromFormat", "pivot", "unpivot"):
        return "from"
    if name.startswith("select") or name in ("distinct", "top", "skip", "take", "skipTake", "orderBy", "orderby",
                                               "groupBy", "having", "rowNumber", "rowNumberUse", "setPage",
                                               "prefix", "subfix", "copyPreSelect", "copyPreFrom", "copyPreWere"):
        return "select"
    if name.startswith("with") or name.startswith("union") or name.startswith("toggle"):
        return "union"
    if name.startswith("merge"):
        return "merge"
    if name.startswith("ifs"):
        return "control"
    return "misc"


def step_class_name(name: str, params) -> str:
    """Unique class name per overload."""
    base = name[0].upper() + name[1:] if name else "Step"
    if not params:
        return base + "Step"
    hints = []
    for typ, pname, *_rest in params:
        t = typ.replace("?", "N").replace("[]", "Arr").replace("<", "_").replace(">", "").replace(",", "_").replace(" ", "")
        # shorten common
        t = t.replace("IEnumerable_", "Enum").replace("IEnumerable", "Enum")
        t = t.replace("List_", "List").replace("Action_SQLBuilder", "Act")
        hints.append(t if len(t) < 24 else pname[0].upper() + pname[1:])
    hint = "".join(hints)
    if len(hint) > 48:
        hint = "".join(pname[0].upper() + pname[1:3] for _, pname, *_r in params)
    return base + hint + "Step"


def extract_methods():
    files = list(BUILDER.glob("StepBuilder*.cs"))
    text = "\n".join(f.read_text(encoding="utf-8") for f in files)
    # match methods including generics on method name
    pat = re.compile(
        r"^\s*public\s+SQLBuilder\s+(\w+)(?:<([^>]+)>)?\s*\(([^)]*)\)",
        re.M,
    )
    found = OrderedDict()
    for m in pat.finditer(text):
        name, gparams, param_src = m.group(1), m.group(2), m.group(3)
        if name in SKIP_NAMES:
            continue
        params = parse_params(param_src)
        # skip Func; bare Action (ifs) stays orchestration-time
        if any(p[0].startswith("Func") for p in params):
            continue
        if any(p[0] == "Action" for p in params):
            continue
        type_tuple = tuple(normalize_type(p[0]) for p in params)
        key2 = (name, type_tuple)
        if key2 in SKIP_METHODS:
            continue
        # store with generic info
        found[key2] = {
            "name": name,
            "generics": gparams,  # e.g. "T" or None
            "params": [(normalize_type(t), n, ip, d) for t, n, ip, d in params],
            "raw_params": param_src.strip(),
        }
    return found


def csharp_param_list(params, with_defaults=True):
    parts = []
    for typ, name, is_params, default in params:
        prefix = "params " if is_params else ""
        piece = f"{prefix}{typ} {name}"
        if with_defaults and default is not None:
            piece += f" = {default}"
        parts.append(piece)
    return ", ".join(parts)


def csharp_arg_list(params):
    return ", ".join(p[1] for p in params)


def write_step(meta):
    name = meta["name"]
    params = meta["params"]
    generics = meta["generics"]
    cls = step_class_name(name, params)
    fam = family_dir(name)
    out_dir = STEPS / fam
    out_dir.mkdir(parents=True, exist_ok=True)
    path = out_dir / f"{cls}.cs"

    fields = []
    ctor_assign = []
    ctor_params = []
    for typ, pname, is_params, _default in params:
        field = "_" + pname
        # params array stored as array type
        ftyp = typ
        fields.append(f"        private readonly {ftyp} {field};")
        ctor_params.append(f"{('params ' if is_params else '')}{typ} {pname}")
        ctor_assign.append(f"            {field} = {pname};")

    apply_args = ", ".join("_" + p[1] for p in params)
    gen_decl = f"<{generics}>" if generics else ""
    # Apply cannot be generic easily if step is non-generic class holding T
    # For generic methods, make the Step class generic too
    class_gen = f"<{generics}>" if generics else ""
    constraints = ""
    if generics and "where" in (meta.get("constraints") or ""):
        constraints = " " + meta["constraints"]

    usings = ""
    joined = " ".join(t for t, _, _ in params)
    if "IEnumerable" in joined or "List<" in joined or "Guid" in joined:
        usings = "using System;\nusing System.Collections;\nusing System.Collections.Generic;\n\n"
    elif "object" in joined or "Type" in joined or "bool" in joined:
        usings = "using System;\n\n"

    if not params:
        # singleton-friendly but still new each time is fine
        body = f'''{usings}namespace mooSQL.data
{{
    /// <summary>对应 SQLBuilder.{name}().</summary>
    public sealed class {cls} : IStep
    {{
        public static readonly {cls} Instance = new {cls}();
        private {cls}() {{ }}
        public void Apply(SQLBuilder builder) => builder.Inner.{name}();
    }}
}}
'''
        facade_call = f"{cls}.Instance"
    else:
        body = f'''{usings}namespace mooSQL.data
{{
    /// <summary>对应 SQLBuilder.{name}{gen_decl}(...).</summary>
    public sealed class {cls}{class_gen} : IStep{constraints}
    {{
{chr(10).join(fields)}

        public {cls}({", ".join(ctor_params)})
        {{
{chr(10).join(ctor_assign)}
        }}

        public void Apply(SQLBuilder builder) => builder.Inner.{name}{gen_decl}({apply_args});
    }}
}}
'''
        args = csharp_arg_list(params)
        if generics:
            facade_call = f"new {cls}<{generics}>({args})"
        else:
            facade_call = f"new {cls}({args})"

    path.write_text(body, encoding="utf-8")
    return cls, facade_call, meta, path


def write_facade(entries):
    """entries: list of (meta, cls, facade_call)"""
    lines = []
    lines.append("using System;")
    lines.append("using System.Collections;")
    lines.append("using System.Collections.Generic;")
    lines.append("")
    lines.append("namespace mooSQL.data")
    lines.append("{")
    lines.append("    /// <summary>")
    lines.append("    /// 自动生成：简单构造 API 的门面入队（由 tools/gen_sqlbuilder_steps.py 生成）。")
    lines.append("    /// </summary>")
    lines.append("    public partial class SQLBuilder")
    lines.append("    {")

    # group by family comment
    by_fam = OrderedDict()
    for meta, cls, call in entries:
        fam = family_dir(meta["name"])
        by_fam.setdefault(fam, []).append((meta, cls, call))

    for fam, items in by_fam.items():
        lines.append(f"        // ---- {fam} ----")
        for meta, cls, call in items:
            name = meta["name"]
            params = meta["params"]
            generics = meta["generics"]
            gen_decl = f"<{generics}>" if generics else ""
            plist = csharp_param_list(params, with_defaults=True)
            sig = f"public new SQLBuilder {name}{gen_decl}({plist})"
            lines.append(f"        {sig} => Enqueue({call});")
            lines.append("")
        lines.append("")

    lines.append("    }")
    lines.append("}")
    DEFER_API.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("wrote", DEFER_API.relative_to(ROOT), "methods", len(entries))


def main():
    methods = extract_methods()
    print("candidates", len(methods))
    entries = []
    seen_cls = set()
    for key, meta in methods.items():
        cls = step_class_name(meta["name"], meta["params"])
        # disambiguate collisions
        orig = cls
        i = 2
        while cls in seen_cls:
            cls = orig.replace("Step", f"{i}Step")
            i += 1
        seen_cls.add(cls)
        # temporarily override name function by patching meta for write
        path_cls, call, meta, path = write_step(meta)
        # if collision renamed, rewrite file with new class name
        if path_cls != cls:
            # re-write with forced class name
            meta2 = dict(meta)
            # monkey: set step_class_name by writing manually
            pass
        # Fix: write_step uses its own naming — ensure unique by checking
        actual_cls = step_class_name(meta["name"], meta["params"])
        if actual_cls != path_cls:
            actual_cls = path_cls
        # detect duplicate class files overwriting — use unique names in write_step
        entries.append((meta, path_cls, call if "Instance" in call or path_cls in call else call))
        # fix call to use returned cls
        if meta["params"]:
            args = csharp_arg_list(meta["params"])
            if meta["generics"]:
                call = f"new {path_cls}<{meta['generics']}>({args})"
            else:
                call = f"new {path_cls}({args})"
        else:
            call = f"{path_cls}.Instance"
        entries[-1] = (meta, path_cls, call)
        print(" ", path_cls)

    # Re-run write_step with uniqueness
    # Clear and regenerate properly
    regenerate_unique(methods)


def regenerate_unique(methods):
    # remove previously generated under steps except IStep and hand-written first batch
    keep = {
        "IStep.cs",
        "SelectStep.cs", "FromStep.cs", "DistinctStep.cs", "OrderByStep.cs", "SetPageStep.cs",
        "WhereKeyValOpParamedStep.cs", "WhereKeyValStep.cs", "WhereRawStep.cs",
    }
    for p in STEPS.rglob("*.cs"):
        if p.name in keep:
            continue
        # only delete generated marker files or all non-keep in steps?
        # safer: delete all except keep
        p.unlink()
        print("removed", p.name)

    seen = set(keep)
    # also reserve class names of kept
    reserved = {
        "SelectStep", "FromStep", "DistinctStep", "OrderByStep", "SetPageStep",
        "WhereKeyValOpParamedStep", "WhereKeyValStep", "WhereRawStep",
    }
    entries = []
    for key, meta in methods.items():
        cls = step_class_name(meta["name"], meta["params"])
        if cls in reserved:
            cls = cls.replace("Step", "GenStep")
        n = 2
        base = cls
        while cls in reserved:
            cls = base.replace("Step", f"{n}Step") if base.endswith("Step") else base + str(n)
            n += 1
        reserved.add(cls)

        # write with forced class name
        path = write_step_named(meta, cls)
        if meta["params"]:
            args = csharp_arg_list(meta["params"])
            if meta["generics"]:
                call = f"new {cls}<{meta['generics']}>({args})"
            else:
                call = f"new {cls}({args})"
        else:
            call = f"{cls}.Instance"
        entries.append((meta, cls, call))
        print("gen", cls)

    write_facade(entries)


def write_step_named(meta, cls):
    name = meta["name"]
    params = meta["params"]
    generics = meta["generics"]
    fam = family_dir(name)
    out_dir = STEPS / fam
    out_dir.mkdir(parents=True, exist_ok=True)
    path = out_dir / f"{cls}.cs"

    fields = []
    ctor_assign = []
    ctor_params = []
    for typ, pname, is_params, _default in params:
        field = "_" + pname
        fields.append(f"        private readonly {typ} {field};")
        ctor_params.append(f"{('params ' if is_params else '')}{typ} {pname}")
        ctor_assign.append(f"            {field} = {pname};")

    apply_args = ", ".join("_" + p[1] for p in params)
    gen_decl = f"<{generics}>" if generics else ""
    class_gen = f"<{generics}>" if generics else ""

    usings = "using System;\nusing System.Collections;\nusing System.Collections.Generic;\n\n"

    if not params:
        body = f'''{usings}namespace mooSQL.data
{{
    /// <summary>对应 SQLBuilder.{name}().</summary>
    public sealed class {cls} : IStep
    {{
        public static readonly {cls} Instance = new {cls}();
        private {cls}() {{ }}
        public void Apply(SQLBuilder builder) => builder.Inner.{name}();
    }}
}}
'''
    else:
        body = f'''{usings}namespace mooSQL.data
{{
    /// <summary>对应 SQLBuilder.{name}{gen_decl}(...).</summary>
    public sealed class {cls}{class_gen} : IStep
    {{
{chr(10).join(fields)}

        public {cls}({", ".join(ctor_params)})
        {{
{chr(10).join(ctor_assign)}
        }}

        public void Apply(SQLBuilder builder) => builder.Inner.{name}{gen_decl}({apply_args});
    }}
}}
'''
    path.write_text(body, encoding="utf-8")
    return path


if __name__ == "__main__":
    methods = extract_methods()
    print("candidates", len(methods))
    regenerate_unique(methods)
