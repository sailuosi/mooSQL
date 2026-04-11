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
		/// <summary>按引用比较 SQL 节点的比较器。</summary>
		public static readonly IEqualityComparer<ISQLNode> ReferenceComparer = ObjectReferenceEqualityComparer<ISQLNode>.Default;

#if DEBUG
		static long IdCounter;

		/// <inheritdoc />
		public virtual string DebugText => this.ToString();

		/// <summary>调试：节点创建顺序编号。</summary>
		public long UniqueId { get; }

		/// <summary>由子类指定子句类型与 CLR 类型。</summary>
		protected SQLElement(ClauseType clauseType,Type type) : base(clauseType,type)
        {
			UniqueId = Interlocked.Increment(ref IdCounter);

			// 打断点用
			if (UniqueId == 0)
			{

			}
		}
#else
        /// <summary>由子类指定子句类型与 CLR 类型。</summary>
        protected SQLElement(ClauseType clauseType, Type type) : base(clauseType, type)
        {
        }

#endif

        /// <inheritdoc />
        public abstract ClauseType       NodeType { get; }


#if OVERRIDETOSTRING
		public override string ToString() => DebugText;
#endif
	}
}
