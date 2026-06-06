using System;

namespace mooSQL.linq
{
	public enum ExprParameterKind
	{
		Default,
		Sequence,
		Values
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public class ExprParameterAttribute : Attribute
	{
		public string?           Name              { get; set; }
		public ExprParameterKind ParameterKind     { get; set; }
		public bool              DoNotParameterize { get; set; }

		public ExprParameterAttribute(string name)
		{
			Name = name;
		}

		public ExprParameterAttribute()
		{
		}
	}
}
