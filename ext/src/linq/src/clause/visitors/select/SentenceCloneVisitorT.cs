using mooSQL.data.model;
using System;

namespace mooSQL.linq.clause
{
	/// <summary>
	/// SentenceCloneVisitorT type.
	/// </summary>
	/// <summary>
	/// SentenceCloneVisitorT type.
	/// </summary>
	public class SentenceCloneVisitor<TContext> : SentenceCloneVisitorBase

	{
		TContext                            _context   = default!;
		Func<TContext, Clause, bool> _cloneFunc = default!;
		/// <summary>
		/// Clone
		/// </summary>

		public Clause Clone(Clause element, TContext context, Func<TContext, Clause, bool> cloneFunc)
		{
			_context   = context;
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}
/// <summary>
/// Cleanup
/// </summary>
	

		public override void Cleanup()
		{
			base.Cleanup();

			_cloneFunc = null!;
			_context   = default!;
		}

		//protected override bool ShouldReplace(ISQLNode element)
		//{
		//	if (base.ShouldReplace(element) && _cloneFunc(_context, element))
		//	{
		//		return true;
		//	}

		//	return false;
		//}

	}
}
