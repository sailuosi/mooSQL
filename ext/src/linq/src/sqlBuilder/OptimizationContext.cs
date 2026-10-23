using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using mooSQL.linq.clause;

namespace mooSQL.linq.provider
{
    using mooSQL.data;
    using mooSQL.data.model;
	/// <summary>
	/// OptimizationContext type.
	/// </summary>

	public class OptimizationContext
	{
		private Dictionary<ParameterWord, ParameterWord>?          _parametersMap;
		private List<ParameterWord>?                              _actualParameters;
		private Dictionary<(DbDataType, object?), ParameterWord>? _dynamicParameters;
		/// <summary>
		/// Gets or sets SqlProviderFlags.
		/// </summary>

		public SQLProviderFlags?             SqlProviderFlags { get; }
		/// <summary>
		/// Gets or sets DBLive.
		/// </summary>

		public DBInstance DBLive { get; }
		/// <summary>
		/// ConvertVisitor
		/// </summary>
		/// <summary>
		/// Gets or sets ConvertVisitor.
		/// </summary>
		public SqlExpressionConvertVisitor   ConvertVisitor   { get; }
		/// <summary>
		/// OptimizerVisitor
		/// </summary>
		/// <summary>
		/// Gets or sets OptimizerVisitor.
		/// </summary>
		public SqlExpressionOptimizerVisitor OptimizerVisitor { get; }
		/// <summary>
		/// TransformationInfo
		/// </summary>

		public SentenceVisitor.IVisitorTransformationInfo TransformationInfo => 
			_transformationInfo ??= new SentenceVisitor.VisitorTransformationInfo();

		SentenceVisitor.IVisitorTransformationInfo? _transformationInfo;
		/// <summary>
		/// TransformationInfoConvert
		/// </summary>

		public SentenceVisitor.IVisitorTransformationInfo TransformationInfoConvert => 
			_transformationInfoConvert ??= new SentenceVisitor.VisitorTransformationInfo();

		SentenceVisitor.IVisitorTransformationInfo? _transformationInfoConvert;
		/// <summary>
		/// Gets or sets EvaluationContext.
		/// </summary>

		public EvaluateContext EvaluationContext              { get; }
		/// <summary>
		/// IsParameterOrderDependent
		/// </summary>
		/// <summary>
		/// Gets or sets IsParameterOrderDependent.
		/// </summary>
		public bool              IsParameterOrderDependent      { get; }
		/// <summary>
		/// IsAlreadyOptimizedAndConverted
		/// </summary>
		/// <summary>
		/// Gets or sets IsAlreadyOptimizedAndConverted.
		/// </summary>
		public bool              IsAlreadyOptimizedAndConverted { get; }
		/// <summary>
		/// HasParameters
		/// </summary>

		public bool HasParameters() => _actualParameters?.Count > 0;
		/// <summary>
		/// Gets GetParameters.
		/// </summary>

		public IReadOnlyList<ParameterWord> GetParameters() => _actualParameters ?? new List<ParameterWord>();
		/// <summary>
		/// AddParameter
		/// </summary>

		public ParameterWord AddParameter(ParameterWord parameter)
		{
			var returnValue = parameter;

			if (!IsParameterOrderDependent && _parametersMap?.TryGetValue(parameter, out var newParameter) == true)
			{
				returnValue = newParameter;
			}
			else
			{
				if (!IsParameterOrderDependent)
					(_parametersMap ??= new()).Add(parameter, returnValue);

				(_actualParameters ??= new()).Add(returnValue);
			}

			return returnValue;
		}
		/// <summary>
		/// SuggestDynamicParameter
		/// </summary>

		public ParameterWord SuggestDynamicParameter(DbDataType dbDataType, object? value)
		{
			var key = (dbDataType, value);

			if (_dynamicParameters == null || !_dynamicParameters.TryGetValue(key, out var param))
			{
				param = new ParameterWord(dbDataType, "value", value);

				_dynamicParameters ??= new();
				_dynamicParameters.Add(key, param);
			}

			return param;
		}
		/// <summary>
		/// ClearParameters
		/// </summary>

		public void ClearParameters()
		{
			_actualParameters = null;
		}
		/// <summary>
		/// OptimizeAndConvertAll&lt;T&gt;
		/// </summary>

		[return : NotNullIfNotNull(nameof(element))]
		public T OptimizeAndConvertAll<T>(T element, NullabilityContext nullabilityContext)
			where T : Clause
		{
			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null,  element as Clause, visitQueries : true, isInsideNot : false, reduceBinary: true);
			var result     = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement, visitQueries : true, isInsideNot : false);

			return result;
		}
		/// <summary>
		/// OptimizeAndConvert&lt;T&gt;
		/// </summary>

		[return: NotNullIfNotNull(nameof(element))]
		public T? OptimizeAndConvert<T>(T? element, NullabilityContext nullabilityContext, bool isInsideNot)
			where T : Clause
		{
			if (IsAlreadyOptimizedAndConverted)
				return element;

			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, element as Clause, visitQueries : false, isInsideNot, reduceBinary : false);
			var result     = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement, false, isInsideNot);

			return result;
		}
		/// <summary>
		/// Optimize&lt;T&gt;
		/// </summary>

		[return: NotNullIfNotNull(nameof(element))]
		public T? Optimize<T>(T? element, NullabilityContext nullabilityContext, bool isInsideNot, bool reduceBinary)
			where T : Clause
		{
			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null,  element as Clause, false, isInsideNot, reduceBinary);

			return newElement as T;
		}
	}
}
