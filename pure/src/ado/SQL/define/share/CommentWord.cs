using System.Collections.Generic;

namespace mooSQL.data.model
{
	/// <summary>
	/// SQL注释
	/// </summary>
	public class CommentWord :Clause, ISQLNode
	{
        public override Clause Accept(ClauseVisitor visitor)
        {
			return visitor.VisitComment(this);
        }
#if DEBUG
        public string DebugText => this.ToDebugString();
#endif
		public ClauseType NodeType => ClauseType.Comment;

		public List<string> Lines { get; }

		public CommentWord():base(ClauseType.Comment,null)
		{
			Lines = new List<string>();
		}

		internal CommentWord(List<string> lines) : base(ClauseType.Comment, null)
        {
			Lines = lines;
		}

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
