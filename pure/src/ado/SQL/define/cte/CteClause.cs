using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace mooSQL.data.model
{
	/// <summary>公用表表达式（CTE）单条定义：名称、列、查询体与递归标志。</summary>
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public class CTEClause : SQLElement
	{
		/// <summary>用于分配 <see cref="CteID"/> 的全局计数器。</summary>
		public static int CteIDCounter;

		/// <summary>显式列名列表（可为空则推导）。</summary>
		public List<FieldWord> Fields { get; internal set; }

		/// <summary>该 CTE 实例的稳定调试编号。</summary>
		public int          CteID       { get; } = Interlocked.Increment(ref CteIDCounter);

		/// <summary>CTE 名称。</summary>
		public string?      Name        { get; set; }
		/// <summary>AS 子句后的 SELECT 体。</summary>
		public SelectQueryClause? Body        { get; set; }
		/// <summary>映射的 CLR 实体类型。</summary>
		public Type         ObjectType  { get; set; }
		/// <summary>是否为递归 CTE。</summary>
		public bool         IsRecursive { get; set; }

		/// <summary>使用查询体与元数据构造 CTE。</summary>
		public CTEClause(
			SelectQueryClause? body,
			Type         objectType,
			bool         isRecursive,
			string?      name) : base(ClauseType.CteClause, null)
        {
			ObjectType  = objectType ?? throw new ArgumentNullException(nameof(objectType));
			Body        = body;
			IsRecursive = isRecursive;
			Name        = name;
			Fields      = new ();
		}

        /// <summary>使用已有列集合构造。</summary>
        public CTEClause(
			SelectQueryClause?          body,
			IEnumerable<FieldWord> fields,
			Type                  objectType,
			bool                  isRecursive,
			string?               name) : base(ClauseType.CteClause, null)
        {
			Body        = body;
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;

			Fields      = fields.ToList();
		}

        /// <summary>仅指定类型与名称，稍后用 <see cref="Init"/> 填充体与列。</summary>
        public CTEClause(
			Type    objectType,
			bool    isRecursive,
			string? name) : base(ClauseType.CteClause, null)
        {
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;
			Fields      = new ();
		}

        /// <summary>延迟设置查询体与输出列。</summary>
        public void Init(
			SelectQueryClause?          body,
			ICollection<FieldWord> fields)
		{
			Body       = body;
			Fields     = fields.ToList();
		}

		/// <inheritdoc />
		public override ClauseType NodeType => ClauseType.CteClause;

		/// <inheritdoc />
		public IElementWriter ToString(IElementWriter writer)
		{
			return writer
					.DebugAppendUniqueId(this)
				.Append("CTE(")
				.Append(CteID)
				.Append(", \"")
				.Append(Name)
				.Append("\")")
				;
		}
	}
}
