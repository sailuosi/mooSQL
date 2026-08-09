# -*- coding: utf-8 -*-
"""Migrate IStep implementations to StepBase + Id/Kind/HasSql/ContributeStructuralHash."""
from __future__ import print_function
import re
from pathlib import Path
from collections import defaultdict

ROOT = Path(__file__).resolve().parents[1] / "pure" / "src" / "ado" / "builder" / "steps"

FOLDER_BASE = {
    "select": 0x010000,
    "from": 0x020000,
    "where": 0x030000,
    "set": 0x040000,
    "union": 0x050000,
    "merge": 0x060000,
    "misc": 0x070000,
    "control": 0x070100,
    "subquery": 0x010200,
}

CONTROL_WHERE_PREFIX = ("And", "Or", "Sink", "Rise", "Pin", "Not")

HAS_SQL_FALSE_FORCE = {
    "ConfigSetNullUpdateSetNullOptionStep",
    "IfsboolStep",
    "NewRowStep",
    "AddRowStep",
}


def infer_kind(folder, cls):
    n = cls.lower()
    if "clearwhere" in n:
        return "ClearWhere"
    if "clearselect" in n:
        return "ClearSelect"
    if "clearpage" in n:
        return "ClearPage"
    if "orderby" in n or "orderby" in cls.lower():
        if "order" in n:
            return "OrderBy"
    if "Orderby" in cls or "OrderBy" in cls:
        return "OrderBy"
    if "groupby" in n:
        return "GroupBy"
    if folder == "select" and n.startswith("having"):
        return "Having"
    if folder == "where":
        if cls.startswith("Where") or cls.startswith("where"):
            return "Where"
        if any(cls.startswith(p) for p in CONTROL_WHERE_PREFIX):
            return "WhereControl"
        return "Where"
    if folder == "select":
        if cls.startswith("From"):
            return "From"
        if "distinct" in n:
            return "Distinct"
        if any(x in n for x in ("skip", "take", "top", "setpage")):
            return "TopSkipTake"
        if "rownumber" in n:
            return "RowNumber"
        if any(x in n for x in ("prefix", "subfix", "copypre", "selectwith", "selectsummary")):
            return "SelectMisc"
        return "Select"
    if folder == "from":
        if "pivot" in n or "unpivot" in n:
            return "PivotUnpivot"
        if "join" in n:
            return "Join"
        return "From"
    if folder == "set":
        if "settable" in n:
            return "SetTable"
        if "newrow" in n or "addrow" in n:
            return "SetRow"
        if "configsetnull" in n:
            return "Other"
        return "Set"
    if folder == "union":
        if "with" in n or "recur" in n:
            return "Cte"
        return "Union"
    if folder == "merge":
        return "Merge"
    if folder == "misc":
        if any(cls.startswith(p) for p in CONTROL_WHERE_PREFIX):
            return "WhereControl"
        return "Other"
    if folder == "control":
        return "Control"
    if folder == "subquery":
        if "where" in n:
            return "Where"
        if "from" in n:
            return "From"
        if "join" in n:
            return "Join"
        if "with" in n:
            return "Cte"
        return "Select"
    return "Other"


def default_has_sql(kind, cls):
    if cls in HAS_SQL_FALSE_FORCE:
        return False
    return kind not in (
        "WhereControl",
        "ClearWhere",
        "ClearSelect",
        "ClearPage",
        "Control",
        "SetRow",
    )


def find_class_spans(text):
    """List of dicts: name, start, brace_open_end, body_end."""
    matches = list(
        re.finditer(
            r"public\s+sealed\s+class\s+(\w+)(?:<[^>]+>)?\s*:\s*IStep\s*\{",
            text,
        )
    )
    spans = []
    for i, m in enumerate(matches):
        body_start = m.end()
        if i + 1 < len(matches):
            body_end = matches[i + 1].start()
        else:
            body_end = len(text)
        spans.append(
            {
                "name": m.group(1),
                "start": m.start(),
                "decl_end": m.end(),  # after opening {
                "body_end": body_end,
            }
        )
    return spans


def collect_fields(body):
    return re.findall(
        r"private\s+readonly\s+([\w.<>,\s\[\]]+?)\s+(_\w+)\s*;",
        body,
    )


def pick_struct_fields(fields):
    skip_names = {
        "_val",
        "_value",
        "_values",
        "_vals",
        "_minValue",
        "_maxValue",
        "_OIDs",
        "_paras",
        "_childSteps",
        "_steps",
        "_frag",
        "_bag",
        "_item",
    }
    bool_int_ok = {
        "_paramed",
        "_isUnionAll",
        "_wrapSelect",
        "_thenDelete",
        "_isOr",
        "_updatable",
        "_insertable",
        "_skip",
        "_take",
        "_num",
        "_size",
        "_maxLength",
        "_SinkMode",
    }
    adds = []
    for typ, name in fields:
        typ = " ".join(typ.split())
        if name in skip_names:
            continue
        if typ in ("object", "Object"):
            continue
        if typ in ("string", "String"):
            adds.append(name)
        elif typ in ("bool", "Boolean", "int", "Int32", "int?", "Int32?"):
            if name in bool_int_ok:
                adds.append(name)
    seen = set()
    out = []
    for x in adds:
        if x not in seen:
            seen.add(x)
            out.append(x)
    return out


def find_collection_field(fields):
    for typ, name in fields:
        t = typ.replace(" ", "")
        if any(x in t for x in ("IEnumerable", "List<", "IList", "[]")):
            return name
    return None


def has_sql_collection_block(coll_name):
    return (
        "        protected override bool HasSql\n"
        "        {\n"
        "            get\n"
        "            {\n"
        "                if (%s == null) return false;\n"
        "                var e = %s as System.Collections.IEnumerable;\n"
        "                if (e == null) return true;\n"
        "                var it = e.GetEnumerator();\n"
        "                try { return it.MoveNext(); }\n"
        "                finally\n"
        "                {\n"
        "                    var d = it as System.IDisposable;\n"
        "                    if (d != null) d.Dispose();\n"
        "                }\n"
        "            }\n"
        "        }\n"
    ) % (coll_name, coll_name)


def migrate_file(path, id_counter):
    text = path.read_text(encoding="utf-8")
    spans = find_class_spans(text)
    if not spans:
        return 0

    folder = path.parent.name
    pieces = []
    last = 0
    count = 0

    for sp in spans:
        pieces.append(text[last : sp["start"]])
        cls = sp["name"]
        body = text[sp["decl_end"] : sp["body_end"]]
        fields = collect_fields(body)
        struct_fields = pick_struct_fields(fields)

        kind = infer_kind(folder, cls)
        if "Orderby" in cls or "OrderBy" in cls:
            kind = "OrderBy"
        if cls == "IfsboolStep":
            kind = "Control"

        base = FOLDER_BASE.get(folder, 0x080000)
        id_counter[folder] += 1
        sid = base + id_counter[folder]
        has_sql = default_has_sql(kind, cls)

        coll = None
        if re.search(r"WhereIn|WhereNotIn", cls):
            coll = find_collection_field(fields)

        # rebuild declaration
        old_decl = text[sp["start"] : sp["decl_end"]]
        new_decl = re.sub(r":\s*IStep\s*\{$", ": StepBase {", old_decl)
        header = new_decl + "\n"
        header += "        public override int Id { get { return %d; } }\n" % sid
        header += "        public override StepKind Kind { get { return StepKind.%s; } }\n" % kind
        if coll:
            header += has_sql_collection_block(coll)
        elif not has_sql:
            header += "        protected override bool HasSql { get { return false; } }\n"

        new_body = body
        if struct_fields and "ContributeStructuralHash" not in new_body:
            method = (
                "\n        protected override void ContributeStructuralHash(ref ScriptHash hc)\n"
                "        {\n"
            )
            for name in struct_fields:
                method += "            hc.Add(%s);\n" % name
            method += "        }\n"
            new_body2, nsub = re.subn(
                r"(\n\s*public\s+void\s+Apply\s*\()",
                method + r"\1",
                new_body,
                count=1,
            )
            if nsub:
                new_body = new_body2

        pieces.append(header + new_body)
        last = sp["body_end"]
        count += 1

    pieces.append(text[last:])
    new_text = "".join(pieces)
    if new_text != text:
        path.write_text(new_text, encoding="utf-8")
    return count


def main():
    skip_names = {
        "IStep.cs",
        "StepBase.cs",
        "StepKind.cs",
        "ScriptHash.cs",
        "StepHashMarks.cs",
    }
    files = sorted(p for p in ROOT.rglob("*.cs") if p.name not in skip_names)
    id_counter = defaultdict(int)
    total = 0
    for f in files:
        total += migrate_file(f, id_counter)

    print("migrated classes:", total)
    untouched = []
    ids = []
    for f in files:
        text = f.read_text(encoding="utf-8")
        if re.search(r":\s*IStep\b", text):
            untouched.append(str(f.relative_to(ROOT)))
        ids.extend(
            int(x)
            for x in re.findall(
                r"public override int Id \{ get \{ return (\d+); \} \}", text
            )
        )
    print("still IStep:", len(untouched))
    for u in untouched:
        print(" ", u)
    print("ids:", len(ids), "unique:", len(set(ids)) == len(ids))


if __name__ == "__main__":
    main()
