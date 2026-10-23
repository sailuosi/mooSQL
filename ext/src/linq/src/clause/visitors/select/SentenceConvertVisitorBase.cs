using mooSQL.data.model;
using System.Collections.Generic;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// SentenceConvertVisitorBase type.
	/// </summary>
	/// <summary>
	/// SentenceConvertVisitorBase type.
	/// </summary>
	public abstract class SentenceConvertVisitorBase : SentenceVisitor
	{
		/// <summary>
		/// SentenceConvertVisitorBase type.
		/// </summary>
		/// <summary>
		/// SentenceConvertVisitorBase type.
		/// </summary>
		protected SentenceConvertVisitorBase(bool allowMutation, IVisitorTransformationInfo? transformationInfo) : base(allowMutation ? VisitMode.Modify : VisitMode.Transform, transformationInfo)
		{
		}
		/// <summary>
		/// AllowMutation
		/// </summary>

		public bool AllowMutation => VisitingMode == VisitMode.Modify;
		/// <summary>
		/// Gets or sets WithStack.
		/// </summary>

		public bool WithStack { get; protected set; }
		/// <summary>
		/// Gets or sets Stack.
		/// </summary>

		public List<ISQLNode>? Stack         { get; protected set; }
		/// <summary>
		/// ParentElement
		/// </summary>
		/// <summary>
		/// ParentElement
		/// </summary>
		public ISQLNode?       ParentElement => Stack?.Count > 0 ? Stack[Stack.Count-1] : null;
/// <summary>
/// Visit
/// </summary>
	

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
		/// <summary>
		/// ConvertElement
		/// </summary>

		public abstract Clause ConvertElement(Clause element);
		/// <summary>
		/// PerformConvert
		/// </summary>

		public Clause PerformConvert(Clause element)
		{
			return ProcessElement(element);
		}
	}
}
