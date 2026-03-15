using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using mooSQL.linq.SqlQuery.Visitors;

namespace mooSQL.linq.SqlProvider
{

	using DataProvider;

    using mooSQL.data;
    using mooSQL.data.model;


	public class OptimizationContext
	{
		private IQueryParametersNormalizer?                      _parametersNormalizer;
		private Dictionary<ParameterWord, ParameterWord>?          _parametersMap;
		private List<ParameterWord>?                              _actualParameters;
		private Dictionary<(DbDataType, object?), ParameterWord>? _dynamicParameters;

		public SQLProviderFlags?             SqlProviderFlags { get; }

		public DBInstance DBLive { get; }
		public SqlExpressionConvertVisitor   ConvertVisitor   { get; }
		public SqlExpressionOptimizerVisitor OptimizerVisitor { get; }

		readonly Func<IQueryParametersNormalizer>           _parametersNormalizerFactory;

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfo => 
			_transformationInfo ??= new SqlQueryVisitor.VisitorTransformationInfo();

		SqlQueryVisitor.IVisitorTransformationInfo? _transformationInfo;

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfoConvert => 
			_transformationInfoConvert ??= new SqlQueryVisitor.VisitorTransformationInfo();

		SqlQueryVisitor.IVisitorTransformationInfo? _transformationInfoConvert;


		public EvaluateContext EvaluationContext              { get; }
		public bool              IsParameterOrderDependent      { get; }
		public bool              IsAlreadyOptimizedAndConverted { get; }

		public bool HasParameters() => _actualParameters?.Count > 0;

		public IReadOnlyList<ParameterWord> GetParameters() => _actualParameters ?? new List<ParameterWord>();

		public ParameterWord AddParameter(ParameterWord parameter)
		{
			var returnValue = parameter;

			if (!IsParameterOrderDependent && _parametersMap?.TryGetValue(parameter, out var newParameter) == true)
			{
				returnValue = newParameter;
			}
			else
			{
				var newName = (_parametersNormalizer ??= _parametersNormalizerFactory()).Normalize(parameter.Name);

				if (IsParameterOrderDependent || newName != parameter.Name)
				{
					returnValue = new ParameterWord(parameter.Type, newName, parameter.Value)
					{
						AccessorId     = parameter.AccessorId,
						ValueConverter = parameter.ValueConverter,
						NeedsCast      = parameter.NeedsCast
					};
				}

				if (!IsParameterOrderDependent)
					(_parametersMap ??= new()).Add(parameter, returnValue);

				(_actualParameters ??= new()).Add(returnValue);
			}

			return returnValue;
		}

		public ParameterWord SuggestDynamicParameter(DbDataType dbDataType, object? value)
		{
			var key = (dbDataType, value);

			if (_dynamicParameters == null || !_dynamicParameters.TryGetValue(key, out var param))
			{
				// converting to SQL Parameter
				// real name (in case of conflicts) will be generated on later stage in AddParameter method
				param = new ParameterWord(dbDataType, "value", value);

				_dynamicParameters ??= new();
				_dynamicParameters.Add(key, param);
			}

			return param;
		}

		public void ClearParameters()
		{
			// must discard instance instead of Clean as it is returned by GetParameters
			_actualParameters     = null;
			_parametersNormalizer = null;
		}

		[return : NotNullIfNotNull(nameof(element))]
		public T OptimizeAndConvertAll<T>(T element, NullabilityContext nullabilityContext)
			where T : Clause
		{
			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null,  element as Clause, visitQueries : true, isInsideNot : false, reduceBinary: true);
			var result     = (T)ConvertVisitor.Convert(this, nullabilityContext, newElement, visitQueries : true, isInsideNot : false);

			return result;
		}

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
