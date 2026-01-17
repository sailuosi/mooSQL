using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace mooSQL.data.model
{


	public class SqlParameterValues : IReadOnlyParaValues
	{
		public static readonly IReadOnlyParaValues Empty = new SqlParameterValues();

		private Dictionary<ParameterWord, SQLParameterValue>? _valuesByParameter;
		private Dictionary<int, SQLParameterValue>?          _valuesByAccessor;

		public void AddValue(ParameterWord parameter, object? providerValue, DbDataType dbDataType)
		{
			_valuesByParameter ??= new ();

			var parameterValue = new SQLParameterValue(providerValue, dbDataType);

			_valuesByParameter.Remove(parameter);
			_valuesByParameter.Add(parameter, parameterValue);

			if (parameter.AccessorId != null)
			{
				_valuesByAccessor  ??= new ();
				_valuesByAccessor.Remove(parameter.AccessorId.Value);
				_valuesByAccessor.Add(parameter.AccessorId.Value, parameterValue);
			}
		}

		public void SetValue(ParameterWord parameter, object? value)
		{
			_valuesByParameter ??= new ();
			if (!_valuesByParameter.TryGetValue(parameter, out var parameterValue))
			{
				parameterValue = new SQLParameterValue(value, parameter.Type);
				_valuesByParameter.Add(parameter, parameterValue);
			}
			else
			{
				_valuesByParameter.Remove(parameter);
				_valuesByParameter.Add(parameter, new SQLParameterValue(value, parameterValue.DbDataType));
			}

			if (parameter.AccessorId != null)
			{
				_valuesByAccessor ??= new ();
				if (!_valuesByAccessor.TryGetValue(parameter.AccessorId.Value, out parameterValue))
				{
					parameterValue = new SQLParameterValue(value, parameter.Type);
					_valuesByAccessor.Add(parameter.AccessorId.Value, parameterValue);
				}
				else
				{
					_valuesByAccessor.Remove(parameter.AccessorId.Value);
					_valuesByAccessor.Add(parameter.AccessorId.Value, new SQLParameterValue(value, parameterValue.DbDataType));
				}
			}
		}

		public bool TryGetValue(ParameterWord parameter,  out SQLParameterValue? value)
		{
			value = null;
			if (_valuesByParameter?.TryGetValue(parameter, out value) == false
			    && parameter.AccessorId != null && _valuesByAccessor?.TryGetValue(parameter.AccessorId.Value, out value) == false)
			{
				return false;
			}

			return value != null;
		}


	}
}
