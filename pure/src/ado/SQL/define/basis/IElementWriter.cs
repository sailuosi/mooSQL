using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    public interface IElementWriter
    {
        bool AddVisited(ISQLNode value);

        void RemoveVisited(ISQLNode value);

        IElementWriter Append(string value);

        IElementWriter Append(char value);

        IElementWriter Append(int value);

        IElementWriter AppendLine(string value);

        IElementWriter AppendLine(char value);

        IElementWriter AppendLine();

        IElementWriter DebugAppendUniqueId<T>(T element, SelectQueryClause? selectQuery = null);

        /// <summary>输出元素并可附带行内 SQL 注释块。</summary>
        IElementWriter AppendTag<T>(T element, CommentWord? comment = null);

        IElementWriter AppendElement<T>(T? comment ) where T : ISQLNode;

        /// <summary>
        /// 缩进
        /// </summary>
        /// <returns></returns>
        int IncrementIndent();
        int DecrementIndent();

        IndentScope Indent();
        IndentScope IndentScope();
    }

    public readonly struct IndentScope : IDisposable
    {
        readonly IElementWriter _writer;

        public IndentScope(IElementWriter writer)
        {
            _writer = writer;
            writer.IncrementIndent();
        }

        public void Dispose()
        {
            _writer.DecrementIndent();
        }
    }
}
