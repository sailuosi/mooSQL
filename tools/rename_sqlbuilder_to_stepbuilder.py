# -*- coding: utf-8 -*-
"""P1: Rename SQLBuilder implementation -> StepBuilder; keep user-facing SQLBuilder as subclass."""
from __future__ import print_function
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "pure" / "src" / "ado" / "builder"

PARTIAL_MAP = {
    "SQLBuilder.cs": "StepBuilder.cs",
    "SQLBuilderWhere.cs": "StepBuilderWhere.cs",
    "SQLBuilderSelect.cs": "StepBuilderSelect.cs",
    "SQLBuilderSave.cs": "StepBuilderSave.cs",
    "SQLBuilderDymatic.cs": "StepBuilderDymatic.cs",
    "SQLBuilder.ext.cs": "StepBuilder.ext.cs",
    "SQLBuilder.route.cs": "StepBuilder.route.cs",
    "SQLBuilder.apart.cs": "StepBuilder.apart.cs",
    "SQLBuilder.shard.cs": "StepBuilder.shard.cs",
}

# Files under builder/ where the kernel type becomes StepBuilder
KERNEL_GLOBS = [
    "SQLBuilder*.cs",
    "SQLKit/**/*.cs",
    "apart/**/*.cs",
    "step/**/*.cs",
]


def collect_kernel_files():
    files = []
    for pattern in KERNEL_GLOBS:
        files.extend(BUILDER.glob(pattern))
    # unique, only files
    out = []
    seen = set()
    for f in files:
        if f.is_file() and f.resolve() not in seen:
            seen.add(f.resolve())
            out.append(f)
    return out


def transform_kernel_text(text: str) -> str:
    # 1) class rename
    text = text.replace("partial class SQLBuilder", "partial class StepBuilder")
    text = text.replace("class SQLBuilder", "class StepBuilder")  # safety

    # 2) blanket type rename in this file
    text = re.sub(r"\bSQLBuilder\b", "StepBuilder", text)

    # 3) user-facing callbacks stay Action<SQLBuilder>
    text = text.replace("Action<StepBuilder>", "Action<SQLBuilder>")
    text = text.replace("Action< StepBuilder >", "Action<SQLBuilder>")

    # 4) fluent / factory returns that should be the facade
    # public StepBuilder xxx( -> public SQLBuilder xxx(
    text = re.sub(
        r"(public\s+(?:(?:new|virtual|override|async)\s+)*)StepBuilder(\s+\w+\s*\()",
        r"\1SQLBuilder\2",
        text,
    )
    # protected/internal rare fluent — leave as StepBuilder unless clearly getBrother/copy
    text = text.replace("public StepBuilder getBrotherBuilder", "public SQLBuilder getBrotherBuilder")
    text = text.replace("public StepBuilder copy(", "public SQLBuilder copy(")
    text = text.replace("public StepBuilder useSQL(", "public SQLBuilder useSQL(")

    # 5) return this; in methods that now return SQLBuilder needs cast.
    # Safer global: replace "return this;" with cast when file is StepBuilder partial.
    # Only do simple `return this;` lines (common fluent pattern).
    text = re.sub(
        r"^(\s*)return this;\s*$",
        r"\1return (SQLBuilder)this;",
        text,
        flags=re.M,
    )

    # 6) new StepBuilder( -> new SQLBuilder( for brother/copy factories (user-facing instances)
    # Keep ability to new StepBuilder only if explicitly needed; prefer facade instances.
    text = text.replace("new StepBuilder()", "new SQLBuilder()")
    text = text.replace("new StepBuilder(", "new SQLBuilder(")

    # 7) cref / docs that said StepBuilder wrongly for user API — OK for now

    # 8) Fix over-replacement: interface/docs IApartStep.Apply(StepBuilder) is desired
    # Fix SQLApart / messages mentioning StepBuilder when they meant SQLBuilder — optional

    # 9) Undo cast on non-SQLBuilder returns? Methods returning void/SQLCmd/etc won't have return this cast issue.
    # Methods returning StepBuilder (internal) that still `return (SQLBuilder)this` — OK if instance is SQLBuilder.

    # 10) Fix: `partial class StepBuilder` file that had `SQLBuilder` in comments restored partially
    # ApartIncompatibleException message may say StepBuilder — fine

    return text


def rename_partial_files():
    for old, new in PARTIAL_MAP.items():
        src = BUILDER / old
        dst = BUILDER / new
        if not src.exists():
            print("skip missing", old)
            continue
        if dst.exists() and src.resolve() != dst.resolve():
            raise SystemExit("target exists: " + new)
        text = transform_kernel_text(src.read_text(encoding="utf-8"))
        dst.write_text(text, encoding="utf-8")
        if src.resolve() != dst.resolve():
            src.unlink()
        print("renamed", old, "->", new)


def transform_other_kernel_files():
    for f in collect_kernel_files():
        if f.name in PARTIAL_MAP.values() or f.name in PARTIAL_MAP:
            continue
        if f.name.startswith("StepBuilder"):
            continue
        original = f.read_text(encoding="utf-8")
        if "SQLBuilder" not in original:
            continue
        text = transform_kernel_text(original)
        if text != original:
            f.write_text(text, encoding="utf-8")
            print("updated", f.relative_to(ROOT))


def write_facade():
    path = BUILDER / "SQLBuilder.cs"
    if path.exists():
        # after rename, SQLBuilder.cs is gone (became StepBuilder.cs)
        pass
    content = r'''using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// SQL 编排门面。构造期步骤进入 <see cref="IStep"/> 队列，执行前 Flush 到基类 <see cref="StepBuilder"/> 完成真正构造。
    /// 当前阶段：继承 <see cref="StepBuilder"/> 以保持行为兼容；逐步将 public 构造方法改为入队实现。
    /// </summary>
    public class SQLBuilder : StepBuilder
    {
        private readonly List<IStep> _steps = new List<IStep>();
        private bool _dirty;
        private bool _materializing;

        /// <summary>是否正在将队列回放到基类（Apply 路径）。</summary>
        internal bool IsMaterializing => _materializing;

        /// <summary>当前编排步骤队列（只读）。</summary>
        internal IReadOnlyList<IStep> Steps => _steps;

        public SQLBuilder() : base() { }

        public SQLBuilder(string name) : base(name) { }

        public SQLBuilder(bool lazyInit) : base(lazyInit) { }

        public SQLBuilder(SQLExpression expression) : base(expression) { }

        /// <summary>入队一个编排步骤。</summary>
        protected SQLBuilder Enqueue(IStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (_materializing)
            {
                step.Apply(this);
                return this;
            }
            _steps.Add(step);
            _dirty = true;
            return this;
        }

        /// <summary>将步骤队列回放到基类构造实现。</summary>
        internal void EnsureMaterialized()
        {
            if (!_dirty) return;
            _materializing = true;
            try
            {
                base.clear();
                for (int i = 0; i < _steps.Count; i++)
                {
                    _steps[i].Apply(this);
                }
            }
            finally
            {
                _materializing = false;
                _dirty = false;
            }
        }

        /// <summary>清空编排队列并重置基类状态。</summary>
        public new SQLBuilder clear()
        {
            _steps.Clear();
            _dirty = false;
            base.clear();
            return this;
        }

        /// <summary>完全重置。</summary>
        public new SQLBuilder reset()
        {
            _steps.Clear();
            _dirty = false;
            base.reset();
            return this;
        }
    }
}
'''
    path.write_text(content, encoding="utf-8")
    print("wrote", path.relative_to(ROOT))


def write_istep():
    steps_dir = BUILDER / "steps"
    steps_dir.mkdir(exist_ok=True)
    path = steps_dir / "IStep.cs"
    content = r'''namespace mooSQL.data
{
    /// <summary>
    /// SQLBuilder 编排步骤：携带一次 public API 调用的参数，在 Flush 时作用于 <see cref="StepBuilder"/>。
    /// </summary>
    public interface IStep
    {
        /// <summary>将本步骤应用到构造宿主。</summary>
        void Apply(StepBuilder builder);
    }
}
'''
    path.write_text(content, encoding="utf-8")
    print("wrote", path.relative_to(ROOT))


def main():
    print("BUILDER", BUILDER)
    rename_partial_files()
    transform_other_kernel_files()
    write_istep()
    write_facade()
    print("done")


if __name__ == "__main__":
    main()
