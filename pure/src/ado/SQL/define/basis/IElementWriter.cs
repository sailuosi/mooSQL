using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    /// <summary>将 SQL 抽象语法输出为文本（或调试缓冲）的写入器。</summary>
    public interface IElementWriter
    {
        /// <summary>标记节点已访问（防循环）。</summary>
        bool AddVisited(ISQLNode value);

        /// <summary>撤销访问标记。</summary>
        void RemoveVisited(ISQLNode value);

        /// <summary>追加字符串片段。</summary>
        IElementWriter Append(string value);

        /// <summary>追加单个字符。</summary>
        IElementWriter Append(char value);

        /// <summary>追加整数字面量文本。</summary>
        IElementWriter Append(int value);

        /// <summary>追加一行文本（含换行）。</summary>
        IElementWriter AppendLine(string value);

        /// <summary>追加一行（单字符）。</summary>
        IElementWriter AppendLine(char value);

        /// <summary>仅追加换行。</summary>
        IElementWriter AppendLine();

        /// <summary>调试输出时附带节点唯一 Id。</summary>
        IElementWriter DebugAppendUniqueId<T>(T element, SelectQueryClause? selectQuery = null);

        /// <summary>输出元素并可附带行内 SQL 注释块。</summary>
        IElementWriter AppendTag<T>(T element, CommentWord? comment = null);

        /// <summary>输出子节点元素。</summary>
        IElementWriter AppendElement<T>(T? comment ) where T : ISQLNode;

        /// <summary>增加缩进层级。</summary>
        /// <returns>新的缩进级别。</returns>
        int IncrementIndent();
        /// <summary>减少缩进层级。</summary>
        /// <returns>新的缩进级别。</returns>
        int DecrementIndent();

        /// <summary>创建可递增缩进的区域（需配合 using）。</summary>
        IndentScope Indent();
        /// <summary>同 <see cref="Indent"/>。</summary>
        IndentScope IndentScope();
    }

    /// <summary>在释放时恢复缩进的缩进作用域。</summary>
    public readonly struct IndentScope : IDisposable
    {
        readonly IElementWriter _writer;

        /// <summary>进入作用域并增加缩进。</summary>
        public IndentScope(IElementWriter writer)
        {
            _writer = writer;
            writer.IncrementIndent();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _writer.DecrementIndent();
        }
    }
}
