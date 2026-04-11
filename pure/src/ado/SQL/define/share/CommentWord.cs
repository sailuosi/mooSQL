using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// SQL注释
	/// </summary>
	public class CommentWord :Clause, ISQLNode
	{
        /// <inheritdoc />
        public override Clause Accept(ClauseVisitor visitor)
        {
			return visitor.VisitComment(this);
        }
#if DEBUG
        /// <summary>调试输出文本。</summary>
        public string DebugText => this.ToDebugString();
#endif
		/// <inheritdoc />
		public ClauseType NodeType => ClauseType.Comment;

		/// <summary>注释正文行（不含 <c>--</c> 前缀，生成时追加）。</summary>
		public List<string> Lines { get; }

		/// <summary>创建空注释块。</summary>
		public CommentWord():base(ClauseType.Comment,null)
		{
			Lines = new List<string>();
		}

		internal CommentWord(List<string> lines) : base(ClauseType.Comment, null)
        {
			Lines = lines;
		}

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			foreach (var part in Lines)
				writer
					.Append("-- ")
					.AppendLine(part);
			return writer;
		}
	}
}
