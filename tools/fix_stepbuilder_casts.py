# -*- coding: utf-8 -*-
"""Fix remaining StepBuilder vs SQLBuilder type mismatches after rename."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "pure" / "src" / "ado" / "builder"

# Host fields that always hold the user facade instance
FIELD_FIXES = [
    (BUILDER / "SQLKit" / "SqlGoup.cs", r"\bprivate StepBuilder root\b", "private SQLBuilder root"),
    (BUILDER / "SQLKit" / "WhereCollection.cs", r"\bpublic StepBuilder root\b", "public SQLBuilder root"),
    (BUILDER / "SQLKit" / "WhereFrag.cs", r"\bpublic StepBuilder root\b", "public SQLBuilder root"),
    (BUILDER / "SQLKit" / "WhereItem.cs", r"\bprivate StepBuilder root\b", "private SQLBuilder root"),
    (BUILDER / "SQLKit" / "MergeIntoBuilder.cs", r"\bStepBuilder parent\b", "SQLBuilder parent"),
    (BUILDER / "SQLKit" / "MergeIntoBuilder.cs", r"\bStepBuilder srcBuilder\b", "SQLBuilder srcBuilder"),
    (BUILDER / "SQLKit" / "MergeIntoBuilder.cs", r"\bStepBuilder onPart\b", "SQLBuilder onPart"),
    (BUILDER / "SQLKit" / "MergeBranch.cs", r"\bpublic StepBuilder Condtion\b", "public SQLBuilder Condtion"),
    (BUILDER / "SQLKit" / "MergeBranch.cs", r"\bpublic StepBuilder SetPart\b", "public SQLBuilder SetPart"),
    (BUILDER / "SQLKit" / "CTE" / "SqlCTE.cs", r"\bpublic StepBuilder builder\b", "public SQLBuilder builder"),
    (BUILDER / "SQLKit" / "CTE" / "RecurCTEBuilder.cs", r"\bStepBuilder\b", "SQLBuilder"),  # careful — may be too broad
]

def add_self_helper():
    path = BUILDER / "StepBuilder.cs"
    text = path.read_text(encoding="utf-8")
    if "SQLBuilder Self" in text or "AsFacade(" in text:
        return
    # Insert after class opening / near fields
    needle = "public partial class StepBuilder:IDisposable\n    {"
    insert = """public partial class StepBuilder:IDisposable
    {
        /// <summary>门面视图；运行时应始终为 <see cref=\"SQLBuilder\"/> 实例。</summary>
        protected SQLBuilder Self => (SQLBuilder)this;
"""
    if needle not in text:
        needle = "public partial class StepBuilder : IDisposable\n    {"
        insert = """public partial class StepBuilder : IDisposable
    {
        /// <summary>门面视图；运行时应始终为 <see cref=\"SQLBuilder\"/> 实例。</summary>
        protected SQLBuilder Self => (SQLBuilder)this;
"""
    if needle not in text:
        # try without newline variations
        m = re.search(r"public partial class StepBuilder\s*:\s*IDisposable\s*\{", text)
        if not m:
            raise SystemExit("cannot find StepBuilder class open")
        text = text[: m.end()] + "\n        /// <summary>门面视图；运行时应始终为 <see cref=\"SQLBuilder\"/> 实例。</summary>\n        protected SQLBuilder Self => (SQLBuilder)this;\n" + text[m.end():]
    else:
        text = text.replace(needle, insert, 1)
    # Replace return (SQLBuilder)this with return Self for consistency
    text = text.replace("return (SQLBuilder)this;", "return Self;")
    path.write_text(text, encoding="utf-8")
    print("added Self helper")


def fix_fields():
    # Targeted field type fixes without broad Recur replace
    pairs = [
        (BUILDER / "SQLKit" / "SqlGoup.cs", "private StepBuilder root", "private SQLBuilder root"),
        (BUILDER / "SQLKit" / "WhereCollection.cs", "public StepBuilder root", "public SQLBuilder root"),
        (BUILDER / "SQLKit" / "WhereFrag.cs", "public StepBuilder root", "public SQLBuilder root"),
        (BUILDER / "SQLKit" / "WhereItem.cs", "private StepBuilder root", "private SQLBuilder root"),
        (BUILDER / "SQLKit" / "MergeBranch.cs", "public StepBuilder Condtion", "public SQLBuilder Condtion"),
        (BUILDER / "SQLKit" / "MergeBranch.cs", "public StepBuilder SetPart", "public SQLBuilder SetPart"),
    ]
    for path, a, b in pairs:
        t = path.read_text(encoding="utf-8")
        if a in t:
            path.write_text(t.replace(a, b), encoding="utf-8")
            print("field", path.name, a, "->", b)

    # MergeIntoBuilder — read and fix property/field declarations
    p = BUILDER / "SQLKit" / "MergeIntoBuilder.cs"
    t = p.read_text(encoding="utf-8")
    t2 = t
    t2 = re.sub(r"\bStepBuilder\b(\s+parent\b)", r"SQLBuilder\1", t2)
    t2 = re.sub(r"\bStepBuilder\b(\s+srcBuilder\b)", r"SQLBuilder\1", t2)
    t2 = re.sub(r"\bStepBuilder\b(\s+onPart\b)", r"SQLBuilder\1", t2)
    # ctor param
    t2 = re.sub(r"MergeIntoBuilder\s*\(\s*StepBuilder\s+", "MergeIntoBuilder(SQLBuilder ", t2)
    t2 = re.sub(r"\bvoid\s+\w+\(.*StepBuilder\s+", lambda m: m.group(0).replace("StepBuilder", "SQLBuilder"), t2)
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("updated MergeIntoBuilder fields")

    p = BUILDER / "SQLKit" / "CTE" / "SqlCTE.cs"
    t = p.read_text(encoding="utf-8")
    t2 = t.replace("public StepBuilder builder", "public SQLBuilder builder")
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("updated SqlCTE")

    p = BUILDER / "SQLKit" / "CTE" / "RecurCTEBuilder.cs"
    t = p.read_text(encoding="utf-8")
    # only field/params that are the host builder
    t2 = t
    t2 = re.sub(r"\bStepBuilder\b(\s+\w*[Bb]uilder\w*)", r"SQLBuilder\1", t2)
    t2 = re.sub(r"\bStepBuilder\b(\s+whereRoot\b)", r"SQLBuilder\1", t2)
    t2 = re.sub(r"\bStepBuilder\b(\s+whereNext\b)", r"SQLBuilder\1", t2)
    t2 = re.sub(r"useBuilder\s*\(\s*StepBuilder\s+", "useBuilder(SQLBuilder ", t2)
    t2 = t2.replace("return (SQLBuilder)this;", "return this;")  # if any wrong
    # apply() returning host
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("updated RecurCTEBuilder")


def fix_stepbuilder_returns():
    """Replace remaining `return this` without cast in StepBuilder* and fix ternary returns."""
    for path in BUILDER.glob("StepBuilder*.cs"):
        t = path.read_text(encoding="utf-8")
        orig = t
        # any remaining bare return this;
        t = re.sub(r"^(\s*)return this;\s*$", r"\1return Self;", t, flags=re.M)
        # return this.xxx already fine
        # Patterns like: return opened ? this.where... : this;
        t = re.sub(r":\s*this\s*;", ": Self;", t)
        t = re.sub(r"\?\s*this\s*:", "? Self:", t)
        # return this.where -> keep
        # Cast method group / callback pass: someMethod(this) when needs SQLBuilder
        # Fix common: action(this) when this is StepBuilder in instance method — use Self
        # Only in files with CS1503 for Action — do broader: .Apply(this) keep; doSomeWhere(this) -> Self
        if path.name in ("StepBuilderWhere.cs", "StepBuilderSelect.cs", "StepBuilderSave.cs", "StepBuilderDymatic.cs", "StepBuilder.route.cs"):
            # doSomeWhere(this) / queryOther(this) / action(this) at call sites for Action<SQLBuilder>
            t = re.sub(r"\b(doSomeWhere|whereBuilder|doselect|doColSelect|childFromPart|doUnion|queryOther|buildRecur|action|doSelect|selectBuilder)\(this\)", r"\1(Self)", t)
            # opened gate returns: return this.where -> the method returns SQLBuilder already from where()
            # Fix: return current.xxx that returns StepBuilder? uncommon
            # Explicit: `return this;` already Self
            # Lines like `return this.where(...)` OK
            # `kit = this` assignments
            t = re.sub(r"=\s*this\s*;", "= Self;", t)
        if t != orig:
            path.write_text(t, encoding="utf-8")
            print("patched returns", path.name)


def fix_where_item_end():
    p = BUILDER / "SQLKit" / "WhereItem.cs"
    t = p.read_text(encoding="utf-8")
    # end() returns root which should be SQLBuilder now
    t2 = t.replace("return root;", "return (SQLBuilder)root;")
    # if root is already SQLBuilder, cast is redundant but OK — prefer just return root
    t2 = t.replace("return (SQLBuilder)root;", "return root;")
    # ensure end signature returns SQLBuilder
    t2 = re.sub(r"public\s+StepBuilder\s+end\s*\(", "public SQLBuilder end(", t2)
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("WhereItem end")


def fix_apart_apply():
    # IApartStep.Apply(StepBuilder) is fine; Replay kit type
    p = BUILDER / "apart" / "WhereStep.cs"
    t = p.read_text(encoding="utf-8")
    # Replay(SQLBuilder) preferred for Action consistency
    t2 = t.replace("Replay(List<WhereStep> steps, StepBuilder kit)", "Replay(List<WhereStep> steps, SQLBuilder kit)")
    t2 = t2.replace("Replay(IEnumerable<WhereStep> steps, StepBuilder kit)", "Replay(IEnumerable<WhereStep> steps, SQLBuilder kit)")
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("WhereStep Replay")

    p = BUILDER / "apart" / "ApartBuildScript.cs"
    t = p.read_text(encoding="utf-8")
    t2 = t.replace("void Apply(StepBuilder kit)", "void Apply(SQLBuilder kit)")
    t2 = t2.replace("ApplyTo(StepBuilder kit)", "ApplyTo(SQLBuilder kit)")
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("ApartBuildScript Apply")

    p = BUILDER / "apart" / "ApartEmitter.cs"
    t = p.read_text(encoding="utf-8")
    t2 = t.replace("Emit(StepBuilder source)", "Emit(SQLBuilder source)")
    if t2 != t:
        p.write_text(t2, encoding="utf-8")
        print("ApartEmitter")


def main():
    add_self_helper()
    # also replace casts in all StepBuilder partials
    for path in BUILDER.glob("StepBuilder*.cs"):
        t = path.read_text(encoding="utf-8")
        t2 = t.replace("return (SQLBuilder)this;", "return Self;")
        if t2 != t:
            path.write_text(t2, encoding="utf-8")
            print("Self replace", path.name)
    fix_fields()
    fix_stepbuilder_returns()
    fix_where_item_end()
    fix_apart_apply()
    print("done")


if __name__ == "__main__":
    main()
