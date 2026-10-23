using mooSQL.data.model;
using System;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// SentenceCloneVisitor type.
	/// </summary>
	/// <summary>
	/// SentenceCloneVisitor type.
	/// </summary>
	public class SentenceCloneVisitor : SentenceCloneVisitorBase
	{
		Func<Clause, bool>? _cloneFunc;
		/// <summary>
		/// Clone
		/// </summary>

		public Clause Clone(Clause element, Func<Clause, bool>? cloneFunc)
		{
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}
/// <summary>
/// Cleanup
/// </summary>
	

		public override void Cleanup()
		{
			base.Cleanup();
			_cloneFunc = null;
		}

		//protected override bool ShouldReplace(ISQLNode element)
		//{
		//	return base.ShouldReplace(element) || (_cloneFunc == null || _cloneFunc(element));
		//}

	}
}
