# -*- coding: utf-8 -*-
"""SQLBuilder: inheritance -> composition over StepBuilder."""
from __future__ import print_function
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "pure" / "src" / "ado" / "builder"


def transform_stepbuilder_file(text: str) -> str:
    text = re.sub(
        r"\s*/// <summary>门面视图.*?</summary>\s*protected internal SQLBuilder Self => \(SQLBuilder\)this;\s*",
        "\n",
        text,
        count=1,
        flags=re.S,
    )
    # also plain Self property without summary
    text = re.sub(
        r"\s*protected internal SQLBuilder Self => \(SQLBuilder\)this;\s*",
        "\n",
        text,
    )
    text = text.replace("return Self;", "return this;")
    text = text.replace("? Self:", "? this:")
    text = text.replace(": Self;", ": this;")
    text = text.replace("= Self;", "= this;")
    text = text.replace("(Self)", "(this)")
    text = text.replace("(Self,", "(this,")
    text = text.replace(", Self)", ", this)")
    text = text.replace("Emit(Self)", "Emit(this)")
    text = text.replace("ApplyTo(Self)", "ApplyTo(this)")
    text = text.replace("doUnion(Self)", "doUnion(this)")
    text = text.replace("union(Self,", "union(this,")

    # fluent returns
    text = re.sub(
        r"public\s+SQLBuilder(\s+\w+(?:<[^>]+>)?\s*\()",
        r"public StepBuilder\1",
        text,
    )
    # private/internal helpers that returned SQLBuilder via Self
    text = re.sub(
        r"(internal|private|protected)\s+SQLBuilder(\s+\w+\s*\()",
        r"\1 StepBuilder\2",
        text,
    )

    text = text.replace("Action<SQLBuilder>", "Action<StepBuilder>")
    text = text.replace("new SQLBuilder()", "new StepBuilder()")
    text = text.replace("new SQLBuilder(", "new StepBuilder(")

    # Factory helpers still return the public facade type
    text = text.replace(
        "public StepBuilder useSQL(bool useTransaction=true)",
        "public SQLBuilder useSQL(bool useTransaction=true)",
    )
    text = text.replace(
        "public StepBuilder useSQL(bool useTransaction = true)",
        "public SQLBuilder useSQL(bool useTransaction = true)",
    )

    return text


def transform_kit_file(text: str, path: Path) -> str:
    reps = [
        (r"\bprivate SQLBuilder root\b", "private StepBuilder root"),
        (r"\bpublic SQLBuilder root\b", "public StepBuilder root"),
        (r"\bSQLBuilder parent\b", "StepBuilder parent"),
        (r"\bSQLBuilder srcBuilder\b", "StepBuilder srcBuilder"),
        (r"\bSQLBuilder onPart\b", "StepBuilder onPart"),
        (r"\bpublic SQLBuilder Condtion\b", "public StepBuilder Condtion"),
        (r"\bpublic SQLBuilder SetPart\b", "public StepBuilder SetPart"),
        (r"\bpublic SQLBuilder builder\b", "public StepBuilder builder"),
        (r"\bprivate SQLBuilder builder\b", "private StepBuilder builder"),
        (r"WhereCollection\(SQLBuilder", "WhereCollection(StepBuilder"),
        (r"public SQLBuilder root;", "public StepBuilder root;"),
        (r"WhereItem\(SQLBuilder", "WhereItem(StepBuilder"),
        (r"private SQLBuilder root;", "private StepBuilder root;"),
        (r"SqlGoup\(([^,\n]+),([^,\n]+),SQLBuilder", r"SqlGoup(\1,\2,StepBuilder"),
        (r"useBuilder\(SQLBuilder", "useBuilder(StepBuilder"),
        (r"doUnion\(SQLBuilder", "doUnion(StepBuilder"),
        (r"union\(SQLBuilder root", "union(StepBuilder root"),
        (r"MergeIntoBuilder\(SQLBuilder", "MergeIntoBuilder(StepBuilder"),
        (r"CloneRouteFrom\(SQLBuilder", "CloneRouteFrom(StepBuilder"),
        (r"public SQLBuilder apply\(\)", "public StepBuilder apply()"),
    ]
    for pat, rep in reps:
        text = re.sub(pat, rep, text)

    # Action callbacks: wrap brother with Attach + EnsureMaterialized
    # Pattern: doselect(builder) / doWhere(builder) / action(...) after getBrotherBuilder
    if "getBrotherBuilder()" in text and "Action<SQLBuilder>" in text:
        # Keep Action<SQLBuilder> for public Kit APIs; adapt call sites
        text = patch_kit_action_calls(text)

    return text


def patch_kit_action_calls(text: str) -> str:
    """After getBrotherBuilder, invoke Action via Attach facade."""
    # Common pattern in SqlGoup:
    #   var builder = root.getBrotherBuilder();
    #   doselect(builder);
    #   field.value = " ("+ builder.toSelect().sql+") ";
    patterns = [
        (
            r"(var builder = root\.getBrotherBuilder\(\);\s*)"
            r"(doselect|doWhere|doSelect|action)\(builder\);",
            r"\1var facade = SQLBuilder.Attach(builder);\n            \2(facade);\n            facade.EnsureMaterialized();",
        ),
        (
            r"(var builder = root\.getBrotherBuilder\(\);\s*)"
            r"(\w+)\(builder\);",
            r"\1var facade = SQLBuilder.Attach(builder);\n            \2(facade);\n            facade.EnsureMaterialized();",
        ),
    ]
    for pat, rep in patterns:
        text = re.sub(pat, rep, text)

    # MergeBranch / MergeInto: action(SetPart) etc.
    text = re.sub(
        r"(\s+)(action|doSelect)\((this\.(SetPart|Condtion|srcBuilder|onPart))\);",
        r"\1{\n\1    var facade = SQLBuilder.Attach(\3);\n\1    \2(facade);\n\1    facade.EnsureMaterialized();\n\1}",
        text,
    )
    return text


def transform_apart_file(text: str) -> str:
    # Emit reads kernel
    text = re.sub(r"Emit\(SQLBuilder source\)", "Emit(StepBuilder source)", text)
    # Apply targets facade (enqueue)
    text = text.replace("void Apply(StepBuilder kit)", "void Apply(SQLBuilder kit)")
    text = text.replace("ApplyTo(StepBuilder kit)", "ApplyTo(SQLBuilder kit)")
    text = re.sub(
        r"Replay\(([^,]+),\s*StepBuilder kit\)",
        r"Replay(\1, SQLBuilder kit)",
        text,
    )
    # Don't convert Action in apart
    return text


def patch_action_steps():
    steps_dir = BUILDER / "steps"
    for path in steps_dir.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        if "Action<SQLBuilder>" not in text and "Action<" not in text:
            continue
        if "Apply(StepBuilder" not in text:
            continue
        # Keep Action<SQLBuilder> fields
        text = text.replace("Action<StepBuilder>", "Action<SQLBuilder>")

        # Expression-bodied Apply -> block with Attach
        def repl(m):
            indent = m.group(1)
            call = m.group(2).strip()
            # builder.method(args);
            mm = re.match(r"builder\.(\w+)(?:<[^>]+>)?\((.*)\);?$", call)
            if not mm:
                return m.group(0)
            method, args = mm.group(1), mm.group(2)
            parts = [a.strip() for a in split_args(args)]
            if not parts:
                return m.group(0)
            # find Action field among args (starts with _)
            action_idx = None
            for i, p in enumerate(parts):
                if p.startswith("_") and ("Act" in path.name or True):
                    # last underscore arg that looks like action field
                    action_idx = i
            if action_idx is None:
                action_idx = len(parts) - 1
            action_arg = parts[action_idx]
            # same-instance methods: union
            same_instance = method in ("union",) and len(parts) == 1

            new_parts = []
            for i, p in enumerate(parts):
                if i == action_idx:
                    if same_instance:
                        new_parts.append(
                            "inner =>\n"
                            f"{indent}    {{\n"
                            f"{indent}        var facade = SQLBuilder.Attach(inner, materializing: true);\n"
                            f"{indent}        {action_arg}(facade);\n"
                            f"{indent}    }}"
                        )
                    else:
                        new_parts.append(
                            "inner =>\n"
                            f"{indent}    {{\n"
                            f"{indent}        var facade = SQLBuilder.Attach(inner);\n"
                            f"{indent}        {action_arg}(facade);\n"
                            f"{indent}        facade.EnsureMaterialized();\n"
                            f"{indent}    }}"
                        )
                else:
                    new_parts.append(p)
            arglist = ", ".join(new_parts)
            return (
                f"{indent}public void Apply(StepBuilder builder)\n"
                f"{indent}{{\n"
                f"{indent}    builder.{method}({arglist});\n"
                f"{indent}}}"
            )

        new_text = re.sub(
            r"([ \t]*)public void Apply\(StepBuilder builder\)\s*=>\s*(builder\.[^;]+);",
            repl,
            text,
        )
        if new_text != text:
            path.write_text(new_text, encoding="utf-8")
            print("patched step", path.relative_to(BUILDER))


def split_args(s: str):
    parts, cur, depth = [], [], 0
    for ch in s:
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
    return parts


def strip_new_and_base_from_defer():
    for name in ("SQLBuilder.defer.cs", "SQLBuilder.defer.api.cs", "SQLBuilder.defer.exec.cs"):
        path = BUILDER / name
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8")
        text2 = re.sub(r"\bpublic new ", "public ", text)
        text2 = text2.replace("base.", "_inner.")
        # Enqueue Apply comments
        text2 = text2.replace("走基类，不重入门面", "走内核，不重入门面")
        text2 = text2.replace("委托基类", "委托内核")
        text2 = text2.replace("基类内部", "内核内部")
        if text2 != text:
            path.write_text(text2, encoding="utf-8")
            print("updated", name)


def write_sqlbuilder_core():
    (BUILDER / "SQLBuilder.cs").write_text(
        '''using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// SQL 编排门面。构造期步骤进入 <see cref="IStep"/> 队列；
    /// 真正构造由内部 <see cref="StepBuilder"/> 完成（见 <c>SQLBuilder.defer.cs</c>）。
    /// </summary>
    public partial class SQLBuilder : IDisposable
    {
        private readonly StepBuilder _inner;
        private readonly List<IStep> _steps = new List<IStep>();
        private bool _dirty;
        private bool _materializing;

        /// <summary>是否正在将队列回放到内核（Apply 路径）。</summary>
        internal bool IsMaterializing => _materializing;

        /// <summary>当前编排步骤队列（只读）。</summary>
        internal IReadOnlyList<IStep> Steps => _steps;

        /// <summary>内核构造器（物化目标）。</summary>
        internal StepBuilder Inner => _inner;

        public SQLBuilder()
        {
            _inner = new StepBuilder();
        }

        public SQLBuilder(string name)
        {
            _inner = new StepBuilder(name);
        }

        public SQLBuilder(bool lazyInit)
        {
            _inner = new StepBuilder(lazyInit);
        }

        public SQLBuilder(SQLExpression expression)
        {
            _inner = new StepBuilder(expression);
        }

        /// <summary>附着已有内核（子查询 / Action 回放）。</summary>
        internal SQLBuilder(StepBuilder inner, bool materializing = false)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _materializing = materializing;
        }

        /// <summary>将内核包装为门面；materializing 时入队即刻 Apply。</summary>
        public static SQLBuilder Attach(StepBuilder inner, bool materializing = false)
        {
            return new SQLBuilder(inner, materializing);
        }

        /// <summary>将步骤队列回放到内核构造实现（脏时执行）。</summary>
        public void EnsureMaterialized()
        {
            if (!_dirty) return;
            _materializing = true;
            try
            {
                _inner.clear();
                for (int i = 0; i < _steps.Count; i++)
                {
                    _steps[i].Apply(_inner);
                }
            }
            finally
            {
                _materializing = false;
                _dirty = false;
            }
        }

        /// <summary>清空编排队列并重置内核状态。</summary>
        public SQLBuilder clear()
        {
            _steps.Clear();
            _dirty = false;
            _inner.clear();
            return this;
        }

        /// <summary>完全重置。</summary>
        public SQLBuilder reset()
        {
            _steps.Clear();
            _dirty = false;
            _inner.reset();
            return this;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}
''',
        encoding="utf-8",
    )
    print("wrote SQLBuilder.cs")


def write_proxy():
    """Forward config/exec helpers not covered by defer.* onto _inner."""
    # Hand-maintained list of common forwards (return-this fluent + pass-through)
    proxy = r'''using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using mooSQL.data.builder;
using mooSQL.data.model;

namespace mooSQL.data
{
    /// <summary>
    /// 组合转发：配置 / 事务 / 路由 / 非构造出口属性，一律落到 <see cref="_inner"/>。
    /// </summary>
    public partial class SQLBuilder
    {
        // ---- 常用属性 / 字段 ----
        public DBInstance DBLive { get => _inner.DBLive; }
        public MooClient MooClient { get => _inner.MooClient; }
        public MooClient Client { get => _inner.Client; }
        public Dialect Dialect { get => _inner.Dialect; }
        public DBExecutor Executor { get => _inner.Executor; }
        public SQLExpression expression { get => _inner.expression; set => _inner.expression = value; }
        public int position { get => _inner.position; set => _inner.position = value; }
        public string Signal { get => _inner.Signal; set => _inner.Signal = value; }
        public SQLMakeUps _MakeUps { get => _inner._MakeUps; set => _inner._MakeUps = value; }
        public SqlGoup preSQL { get => _inner.preSQL; set => _inner.preSQL = value; }
        public string paraSeed { get => _inner.paraSeed; }
        public int level { get => _inner.level; set => _inner.level = value; }
        public string name { get => _inner.name; set => _inner.name = value; }
        public Paras ps { get => _inner.ps; set => _inner.ps = value; }
        public string preWhere { get => _inner.preWhere; set => _inner.preWhere = value; }
        public string paraRule { get => _inner.paraRule; set => _inner.paraRule = value; }
        public int ColumnCount { get => _inner.ColumnCount; }
        public int FromCount { get => _inner.FromCount; }
        public int InsertRowIndex { get => _inner.InsertRowIndex; }
        internal SqlGoup current { get => _inner.current; set => _inner.current = value; }

        // ---- 配置 / 事务 / 工厂 ----
        public SQLBuilder configClear(CleanWay way) { _inner.configClear(way); return this; }
        public SQLBuilder useSignal(string signalName) { _inner.useSignal(signalName); return this; }
        public SQLBuilder resetSignal() { _inner.resetSignal(); return this; }
        public SQLBuilder setPosition(int position) { _inner.setPosition(position); return this; }
        public bool containSetColumn(string name) => _inner.containSetColumn(name);
        public SQLBuilder print(Action<string> onPrint) { _inner.print(onPrint); return this; }
        public SQLBuilder setCacheHolder(ISooCache cacher) { _inner.setCacheHolder(cacher); return this; }
        public SQLBuilder setDBInstance(DBInstance db) { _inner.setDBInstance(db); return this; }
        public SQLBuilder beginTransaction() { _inner.beginTransaction(); return this; }
        public SQLBuilder beginTransaction(IsolationLevel lv) { _inner.beginTransaction(lv); return this; }
        public SQLBuilder useTransaction(DBExecutor executor) { _inner.useTransaction(executor); return this; }
        public void commit(bool autoRollBack = true) => _inner.commit(autoRollBack);
        public string SqlFilter(string source, bool onlyWrite) => _inner.SqlFilter(source, onlyWrite);
        public string addPara(string key, Object val) => _inner.addPara(key, val);
        public List<string> addListPara(IEnumerable<object> list, string prefix) => _inner.addListPara(list, prefix);
        public SQLBuilder setCache(string key, int timeout) { _inner.setCache(key, timeout); return this; }
        public SQLBuilder setSeed(string seed) { _inner.setSeed(seed); return this; }

        public SQLBuilder getBrotherBuilder() => Attach(_inner.getBrotherBuilder());
        public SQLBuilder copy() => Attach(_inner.copy());

        public SQLBuilder useSQL(bool useTransaction = true) => _inner.useSQL(useTransaction);
        public DDLBuilder useDDL() => _inner.useDDL();
        public SQLSentence useSentence() => _inner.useSentence();

        public MergeIntoBuilder mergeInto(string tbName, string asName = null) => _inner.mergeInto(tbName, asName);

        // ---- Apart：物化后读写内核；useApart 重放到门面以入队 ----
        public SQLBuilder record()
        {
            EnsureMaterialized();
            _inner.record();
            return this;
        }

        public SQLApart stop()
        {
            EnsureMaterialized();
            return _inner.stop();
        }

        public SQLApart toApart()
        {
            EnsureMaterialized();
            return _inner.toApart();
        }

        public SQLBuilder useApart(SQLApart apart)
        {
            if (apart == null) throw new ArgumentNullException(nameof(apart));
            apart.Script.ApplyTo(this);
            return this;
        }
    }
}
'''
    # Fix StepBuilder.record/useApart after transform — they return StepBuilder and ApplyTo(this) needs SQLBuilder
    # We'll fix StepBuilder.apart separately to use Attach for ApplyTo or only Emit on kernel.
    (BUILDER / "SQLBuilder.proxy.cs").write_text(proxy, encoding="utf-8")
    print("wrote SQLBuilder.proxy.cs")


def fix_stepbuilder_apart():
    path = BUILDER / "StepBuilder.apart.cs"
    text = path.read_text(encoding="utf-8")
    # record/useApart on kernel: ApplyTo needs SQLBuilder — use Attach for useApart; record returns this
    text = '''using System;

namespace mooSQL.data
{
    public partial class StepBuilder
    {
        /// <summary>
        /// 开启录播：返回独立影子 Builder，链式调用仅写入该影子，不污染当前实例；
        /// 以 <see cref="stop"/> 结束并得到 <see cref="SQLApart"/>，再通过门面 <c>useApart</c> 复用。
        /// </summary>
        public StepBuilder record()
        {
            this.current.wherePart.steps.start();
            return this;
        }

        /// <summary>
        /// 结束 <see cref="record"/> 录播链，将期间步骤捕获为 <see cref="SQLApart"/>。
        /// </summary>
        public SQLApart stop()
        {
            this.current.wherePart.steps.stop();
            return toApart();
        }

        /// <summary>
        /// 将当前构建状态捕获为可复用碎片（API 步骤脚本）。
        /// </summary>
        public SQLApart toApart()
        {
            var script = ApartEmitter.Emit(this);
            var dbType = ResolveDbType();
            return new SQLApart(script, dbType);
        }

        /// <summary>
        /// 内核侧重放：经 Attach 门面调用公开 API（供非门面路径）。
        /// </summary>
        public StepBuilder useApart(SQLApart apart)
        {
            if (apart == null)
                throw new ArgumentNullException(nameof(apart));
            EnsureApartCompatible(apart);
            var facade = SQLBuilder.Attach(this, materializing: true);
            apart.Script.ApplyTo(facade);
            return this;
        }

        internal SqlCTE ApartGetCte() => CTECollection;

        private void EnsureApartCompatible(SQLApart apart)
        {
            var target = ResolveDbType();
            if (apart.SourceDbType != target)
                throw new ApartIncompatibleException(apart.SourceDbType, target);
        }

        private DataBaseType ResolveDbType()
        {
            if (DBLive?.config != null)
                return DBLive.config.dbType;
            return DataBaseType.MSSQL;
        }
    }
}
'''
    path.write_text(text, encoding="utf-8")
    print("rewrote StepBuilder.apart.cs")


def fix_enqueue_in_defer():
    path = BUILDER / "SQLBuilder.defer.cs"
    text = path.read_text(encoding="utf-8")
    # Enqueue Apply(this) during materializing must Apply(_inner)
    text = text.replace("step.Apply(this);", "step.Apply(_inner);")
    path.write_text(text, encoding="utf-8")
    print("fixed Enqueue Apply target")


def patch_proxy_for_route_and_dymatic():
    """Scan StepBuilder* for public methods returning StepBuilder / other still missing; append stubs if needed.
    For now rely on compile errors to extend SQLBuilder.proxy.cs / route forwards.
    """
    # Forward route/shard/dymatic methods that are public on StepBuilder and not in defer
    # We'll build from compile later; add common ones from missing list.
    extra = r'''
namespace mooSQL.data
{
    public partial class SQLBuilder
    {
        // generated extras filled after first compile if needed
    }
}
'''
    pass


def main():
    # 1) StepBuilder* kernel
    for f in BUILDER.glob("StepBuilder*.cs"):
        if f.name == "StepBuilder.apart.cs":
            continue  # rewritten below
        orig = f.read_text(encoding="utf-8")
        text = transform_stepbuilder_file(orig)
        if text != orig:
            f.write_text(text, encoding="utf-8")
            print("kernel", f.name)

    fix_stepbuilder_apart()

    # 2) Kit
    for f in BUILDER.glob("SQLKit/**/*.cs"):
        orig = f.read_text(encoding="utf-8")
        text = transform_kit_file(orig, f)
        # WhereCollection ReplaySteps stays SQLBuilder
        if f.name == "WhereCollection.cs":
            text = text.replace(
                "internal void ReplaySteps(StepBuilder kit)",
                "internal void ReplaySteps(SQLBuilder kit)",
            )
        if text != orig:
            f.write_text(text, encoding="utf-8")
            print("kit", f.relative_to(BUILDER))

    # 3) apart
    for f in (BUILDER / "apart").glob("**/*.cs"):
        orig = f.read_text(encoding="utf-8")
        text = transform_apart_file(orig)
        # also convert any remaining Self if any
        if text != orig:
            f.write_text(text, encoding="utf-8")
            print("apart", f.name)

    # 4) facade core + proxy
    write_sqlbuilder_core()
    write_proxy()
    strip_new_and_base_from_defer()
    fix_enqueue_in_defer()

    # 5) Action steps
    patch_action_steps()

    print("DONE phase1")


if __name__ == "__main__":
    main()
