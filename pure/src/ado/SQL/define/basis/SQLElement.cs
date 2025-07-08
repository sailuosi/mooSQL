using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;

namespace mooSQL.data.model
{


	/// <summary>
	/// 基础SQL节点，只有需要调试时添加方法使用.
	/// </summary>
	public abstract class SQLElement:Clause, ISQLNode
	{
		/// <summary>
		/// 引用比较器.
		/// </summary>
		public static readonly IEqualityComparer<ISQLNode> ReferenceComparer = ObjectReferenceEqualityComparer<ISQLNode>.Default;

#if DEBUG
		static long IdCounter;

		public virtual string DebugText => this.ToString();

		// 调试用，帮助理解创建时机
		public long UniqueId { get; }

		protected SQLElement(ClauseType clauseType,Type type) : base(clauseType,type)
        {
			UniqueId = Interlocked.Increment(ref IdCounter);

			// 打断点用
			if (UniqueId == 0)
			{

			}
		}
#else
        protected SQLElement(ClauseType clauseType, Type type) : base(clauseType, type)
        {
        }

#endif

        public abstract ClauseType       NodeType { get; }


#if OVERRIDETOSTRING
		public override string ToString() => DebugText;
#endif
	}
}
