using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace mooSQL.data.model
{
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public class CTEClause : SQLElement
	{
		public static int CteIDCounter;

		public List<FieldWord> Fields { get; internal set; }

		public int          CteID       { get; } = Interlocked.Increment(ref CteIDCounter);

		public string?      Name        { get; set; }
		public SelectQueryClause? Body        { get; set; }
		public Type         ObjectType  { get; set; }
		public bool         IsRecursive { get; set; }

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

        public void Init(
			SelectQueryClause?          body,
			ICollection<FieldWord> fields)
		{
			Body       = body;
			Fields     = fields.ToList();
		}

		public override ClauseType NodeType => ClauseType.CteClause;

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
