using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{
	using Common;

	/// <summary>
	/// 类型 EvaluateContext。
	/// </summary>
	public class EvaluateContext
	{
		private Dictionary<ISQLNode, EvaluateResult>? _evaluationCache;

		/// <summary>
		/// 初始化 EvaluateContext（构造）。
		/// </summary>
		public EvaluateContext(IReadOnlyParaValues? parameterValues = null)
		{
			ParameterValues = parameterValues;
		}

		/// <summary>
		/// 属性 ParameterValues（IReadOnlyParaValues?）。
		/// </summary>
		public IReadOnlyParaValues? ParameterValues { get; }

		/// <summary>
		/// 字段 IsParametersInitialized（bool）。
		/// </summary>
		public bool IsParametersInitialized => ParameterValues != null;

        /// <summary>
        /// 尝试GetValue。
        /// </summary>
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

		/// <summary>
		/// Register 方法。
		/// </summary>
		public void Register(ISQLNode expr, object? value)
		{
			_evaluationCache ??= new (ObjectReferenceEqualityComparer<ISQLNode>.Default);
			_evaluationCache.Add(expr, new EvaluateResult(value, true));
		}

		/// <summary>
		/// RegisterError 方法。
		/// </summary>
		public void RegisterError(ISQLNode expr)
		{
			_evaluationCache ??= new(ObjectReferenceEqualityComparer<ISQLNode>.Default);
			_evaluationCache.Add(expr, new EvaluateResult(null, false));
		}
	}

	/// <summary>
	/// 类型 EvaluateResult。
	/// </summary>
	public class EvaluateResult {
		/// <summary>
		/// 构造函数。
		/// </summary>
		public EvaluateResult(object? val,bool succ) {
			this.value = val;
			this.success = succ;
		}
        /// <summary>
        /// 字段 value（object）。
        /// </summary>
        public object value;
		/// <summary>
		/// 字段 success（bool）。
		/// </summary>
		public bool success;

    }
}