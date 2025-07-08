using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	using Common;

	public class EvaluateContext
	{
		private Dictionary<ISQLNode, EvaluateResult>? _evaluationCache;

		public EvaluateContext(IReadOnlyParaValues? parameterValues = null)
		{
			ParameterValues = parameterValues;
		}

		public IReadOnlyParaValues? ParameterValues { get; }

		public bool IsParametersInitialized => ParameterValues != null;

        public bool TryGetValue(ISQLNode expr, [NotNullWhen(true)] out EvaluateResult? info)
		{
			if (_evaluationCache == null)
			{
				info = null;
				return false;
			}

			if (_evaluationCache.TryGetValue(expr, out var infoValue))
			{
				info = infoValue;
				return true;
			}

			info = null;
			return false;
		}

		public void Register(ISQLNode expr, object? value)
		{
			_evaluationCache ??= new (ObjectReferenceEqualityComparer<ISQLNode>.Default);
			_evaluationCache.Add(expr, new EvaluateResult(value, true));
		}

		public void RegisterError(ISQLNode expr)
		{
			_evaluationCache ??= new(ObjectReferenceEqualityComparer<ISQLNode>.Default);
			_evaluationCache.Add(expr, new EvaluateResult(null, false));
		}
	}

	public class EvaluateResult {
		public EvaluateResult(object? val,bool succ) {
			this.value = val;
			this.success = succ;
		}
        public object value;
		public bool success;

    }
}
