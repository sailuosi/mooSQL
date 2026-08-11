using System;
using mooSQL.data;

namespace mooSQL.data
{
    /// <summary>
    /// Clip 层窗口函数：直接借用 <see cref="SQLBuilder"/> 的 <see cref="WindowBuilder"/>。
    /// </summary>
    public partial class SQLClip
    {
        /// <summary>窗口函数：<c>func OVER (...)</c>。</summary>
        public WindowBuilder window(string functionSql)
        {
            return Context.Builder.window(functionSql);
        }

        /// <summary>仅构建 <c>OVER (...)</c>。</summary>
        public WindowBuilder over()
        {
            return Context.Builder.over();
        }

        /// <summary><see cref="window"/> 别名。</summary>
        public WindowBuilder over(string functionSql)
        {
            return Context.Builder.over(functionSql);
        }

        /// <summary><c>ROW_NUMBER() OVER (...)</c>。</summary>
        public WindowBuilder windowRowNumber() => Context.Builder.windowRowNumber();

        /// <summary><c>RANK() OVER (...)</c>。</summary>
        public WindowBuilder windowRank() => Context.Builder.windowRank();

        /// <summary><c>DENSE_RANK() OVER (...)</c>。</summary>
        public WindowBuilder windowDenseRank() => Context.Builder.windowDenseRank();

        /// <summary>构建窗口表达式并加入 SELECT。</summary>
        public SQLClip selectWindow(string functionSql, Action<WindowBuilder> build, string alias)
        {
            Context.Builder.selectWindow(functionSql, build, alias);
            return this;
        }

        /// <summary>构建 <c>ROW_NUMBER()</c> 窗口并加入 SELECT。</summary>
        public SQLClip selectRowNumber(Action<WindowBuilder> build, string alias)
        {
            Context.Builder.selectRowNumber(build, alias);
            return this;
        }
    }
}
