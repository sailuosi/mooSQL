#!/usr/bin/env python3
"""Controlled renames for Ext LINQ de-Linq2DB migration."""
import os
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def mv(src, dst):
    s, d = ROOT / src, ROOT / dst
    if s.exists():
        d.parent.mkdir(parents=True, exist_ok=True)
        if d.exists():
            if s.is_dir():
                for item in s.iterdir():
                    shutil.move(str(item), str(d / item.name))
                s.rmdir()
            else:
                return
        else:
            s.rename(d)
        print(f"mv {src} -> {dst}")

def rename_sql_files():
    dbf = ROOT / "src/api/dbfunc"
    if not dbf.exists():
        dbf = ROOT / "src/api/DbFunc"
    if not dbf.exists():
        return
    for f in list(dbf.glob("Sql.*")):
        f.rename(dbf / f.name.replace("Sql.", "DbFunc.", 1))
    sc = dbf / "Sql.cs"
    if sc.exists():
        sc.rename(dbf / "DbFunc.cs")

# Phase 1: directories
mv("src/outcast", "src/api")
mv("src/api/Sql", "src/api/dbfunc")
rename_sql_files()

# merge buildContext into clauseContext
bc = ROOT / "src/linq/builder/buildContext"
cc = ROOT / "src/linq/builder/clauseContext"
if bc.exists():
    cc.mkdir(parents=True, exist_ok=True)
    for f in bc.iterdir():
        dest = cc / f.name
        if not dest.exists():
            f.rename(dest)
    bc.rmdir()

# file renames
file_renames = [
    ("src/linq/builder/clauseContext/BuildContextBase.cs", "src/linq/builder/clauseContext/ClauseContextBase.cs"),
    ("src/linq/builder/interfaces/IBuildContext.cs", "src/linq/builder/interfaces/IClauseContext.cs"),
    ("src/linq/builder/BuildContextDebuggingHelper.cs", "src/linq/builder/ClauseContextDebuggingHelper.cs"),
    ("src/linq/builder/LoadWithInfo.cs", "src/linq/builder/IncludeInfo.cs"),
    ("src/linq/builder/clauseContext/LoadWithContext.cs", "src/linq/builder/clauseContext/IncludeContext.cs"),
    ("src/linq/builder/clauseSqlTranslator/ClauseSqlTranslator.LoadWith.cs", "src/linq/builder/clauseSqlTranslator/ClauseSqlTranslator.Includes.cs"),
    ("src/linq/builder/clauseSqlTranslator/ClauseSqlTranslator.SqlBuilder.MakeExpression.cs",
     "src/linq/builder/clauseSqlTranslator/ClauseSqlTranslator.SqlBuilder.BuildProjection.cs"),
    ("translator/ClauseMethodVisitor.LoadWith.cs", "translator/ClauseMethodVisitor.Includes.cs"),
    ("src/api/root/extensions/LinqExtensions.LoadWith.cs", "src/api/root/extensions/LinqExtensions.Includes.cs"),
    ("src/api/root/ILoadWithQueryable.cs", "src/api/root/IIncludeQueryable.cs"),
    ("core/ExtLinq编译层开发指南-双访问器与MakeExpression.md", "core/ExtLinq编译层开发指南-双访问器与BuildProjection.md"),
]
for a, b in file_renames:
    sa, sb = ROOT / a, ROOT / b
    if sa.exists() and not sb.exists():
        sb.parent.mkdir(parents=True, exist_ok=True)
        sa.rename(sb)

CONTENT = [
    (r"\bIBuildContext\b", "IClauseContext"),
    (r"\bBuildContextBase\b", "ClauseContextBase"),
    (r"\bBuildContextDebuggingHelper\b", "ClauseContextDebuggingHelper"),
    (r"\bMakeExpression\b", "BuildProjection"),
    (r"\bLoadWithInfo\b", "IncludeInfo"),
    (r"\bLoadWithContext\b", "IncludeContext"),
    (r"\bILoadWithQueryable\b", "IIncludeQueryable"),
    (r"\bLoadWithQueryable\b", "IncludeQueryable"),
    (r"buildContext/", "clauseContext/"),
    (r"outcast/", "api/"),
    (r"partial class Sql\b", "partial class DbFunc"),
    (r"static partial class Sql\b", "static partial class DbFunc"),
    (r"\bLoadWithAsTable\b", "IncludesAsTable"),
    (r"\bLoadWithInternal\b", "IncludeInternal"),
    (r"\bLoadWithCall\b", "IncludesCall"),
    (r"\bThenLoadCall\b", "ThenIncludeCall"),
    (r"\bLoadWithAsTableCall\b", "IncludesAsTableCall"),
    (r"\bLoadWithInternalCall\b", "IncludeInternalCall"),
    (r"\bVisitLoadWith\b", "VisitIncludes"),
    (r"\bVisitThenLoad\b", "VisitThenInclude"),
    (r"\bVisitLoadWithAsTable\b", "VisitIncludesAsTable"),
    (r"\bVisitLoadWithInternal\b", "VisitIncludeInternal"),
    (r"\bVisitLoadWithCore\b", "VisitIncludeCore"),
    (r"\bBuildLoadWith\b", "BuildInclude"),
    (r"\bCanBuildLoadWith\b", "CanBuildInclude"),
    (r"\bRegisterLoadWithNavColumns\b", "RegisterIncludeNavColumns"),
    (r"\bGetTableLoadWith\b", "GetTableIncludes"),
    (r"\bLastLoadWithInfo\b", "LastIncludeInfo"),
    (r"\bLoadWithRoot\b", "IncludeRoot"),
    (r"\bLoadWithPath\b", "IncludePath"),
    (r"\bMergeLoadWith\b", "MergeInclude"),
    (r"#region MakeExpression \(from linq2db", "#region BuildProjection"),
    (r"自 linq2db 移植", "Clause 编译层实现"),
    (r"linq2db 移植", "Clause 编译"),
    (r"替代 linq2db DI 注册", "按方言解析 MemberTranslator"),
    (r"Contains global linq2db settings", "Contains global Ext LINQ settings"),
    (r"for all linq2db internal await", "for all Ext LINQ internal await"),
    (r"This API supports the linq2db infrastructure", "Internal expression infrastructure API"),
]

PUBLIC = [(r"\bLoadWith\b", "Includes"), (r"\bThenLoad\b", "ThenInclude")]

for dirpath, _, files in os.walk(ROOT):
    if "tools" in dirpath.replace("\\", "/").split("/"):
        continue
    for fn in files:
        if not fn.endswith((".cs", ".md")):
            continue
        p = Path(dirpath) / fn
        t = p.read_text(encoding="utf-8")
        o = t
        for pat, repl in CONTENT:
            t = re.sub(pat, repl, t)
        if fn == "LinqExtensions.Includes.cs":
            for pat, repl in PUBLIC:
                t = re.sub(pat, repl, t)
        if t != o:
            p.write_text(t, encoding="utf-8")

print("migration script complete")
