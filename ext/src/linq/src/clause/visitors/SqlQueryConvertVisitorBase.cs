using mooSQL.data.model;
using System.Collections.Generic;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public abstract class SqlQueryConvertVisitorBase : SqlQueryVisitor
	{
		protected SqlQueryConvertVisitorBase(bool allowMutation, IVisitorTransformationInfo? transformationInfo) : base(allowMutation ? VisitMode.Modify : VisitMode.Transform, transformationInfo)
		{
		}

		public bool AllowMutation => VisitingMode == VisitMode.Modify;

		public bool WithStack { get; protected set; }

		public List<ISQLNode>? Stack         { get; protected set; }
		public ISQLNode?       ParentElement => Stack?.Count > 0 ? Stack[Stack.Count-1] : null;

		public override Clause? Visit(Clause? element)
		{
			if (element == null)
				return null;

			if (WithStack)
			{
				Stack ??= new List<ISQLNode>();
				Stack.Add(element);
			}

			if (GetReplacement(element, out var replacement))
				return replacement;

			var newElement = base.Visit(element);

			if (!ReferenceEquals(newElement, element))
			{
				NotifyReplaced(newElement, element);
			}

			var convertedElement = ConvertElement(newElement);

			if (!ReferenceEquals(convertedElement, newElement))
			{
				NotifyReplaced(convertedElement, newElement);

				// do convert again
				convertedElement = Visit(convertedElement);
			}

			if (WithStack)
			{
				Stack?.RemoveAt(Stack.Count - 1);
			}

			return convertedElement;
		}

		public abstract Clause ConvertElement(Clause element);

		public Clause PerformConvert(Clause element)
		{
			return ProcessElement(element);
		}
	}
}
