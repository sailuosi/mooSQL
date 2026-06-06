using System;
using System.Linq;
using System.Reflection;
using mooSQL.data;

namespace mooSQL.linq.Extensions
{
	using Common;
	using Data;
	using Mapping;
    using mooSQL.data;
    using mooSQL.data.model;
    using mooSQL.utils;
    using SqlQuery;

	static class MappingExtensions
	{


        public static ValueWord GetSqlValueFromObject(this DBInstance mappingSchema, EntityColumn columnDescriptor, object obj)
        {
            throw new NotImplementedException();
			//var providerValue = columnDescriptor.GetProviderValue(obj);

            //return new ValueWord(columnDescriptor.GetDbDataType(true), providerValue);
        }



		public static ValueWord GetSqlValue(this DBInstance mappingSchema, Type systemType, object? originalValue, DbDataType? columnType)
		{
			if (originalValue is DataParameter p)
				return new ValueWord(p.Value == null ? p.GetOrSetDbDataType(columnType) : p.DbDataType, p.Value);

			var underlyingType = systemType.UnwrapNullable();

			if (!mappingSchema.dialect.mapping.ValueToSQL.CanConvert(underlyingType,typeof(string)))
			{
				if (underlyingType.IsEnum )
				{
					if (originalValue != null || systemType == underlyingType)
					{
						var type = Converter.GetDefaultMappingFromEnumType(null, systemType)!;

						//if (Configuration.UseEnumValueNameForStringColumns && type == typeof(string) &&
						//    mappingSchema.GetMapValues(underlyingType)             == null)
						//	return new ValueWord(type, string.Format(CultureInfo.InvariantCulture, "{0}", originalValue));
						// Converter.ChangeType(originalValue, type, mappingSchema)
						var v = mappingSchema.dialect.mapping.ChangeTypeTo(originalValue, type);
                        return new ValueWord(type,v);
					}
				}
			}

			if (systemType == typeof(object) && originalValue != null)
				systemType = originalValue.GetType();

			var valueDbType = originalValue == null ? columnType ?? new DbDataType(systemType) : new DbDataType(systemType);

			return new ValueWord(valueDbType, originalValue);
		}




		public static DbFunc.ExpressionAttribute? GetExpressionAttribute(this MemberInfo member, DBInstance mappingSchema)
		{
			if (member is not MethodInfo and not PropertyInfo)
				return null;

			var attrs = member.GetCustomAttributes(typeof(DbFunc.ExpressionAttribute), inherit: false)
				.Cast<DbFunc.ExpressionAttribute>()
				.ToArray();

			return PickExpressionAttribute(attrs, GetDialectConfiguration(mappingSchema));
		}

		internal static string? GetDialectConfiguration(DBInstance db)
		{
			var dialectName = db.dialect?.GetType().Name;
			if (string.IsNullOrEmpty(dialectName))
				return null;

			if (dialectName.EndsWith("Dialect", StringComparison.Ordinal) && dialectName.Length > "Dialect".Length)
				return dialectName[..^"Dialect".Length];

			return dialectName;
		}

		internal static DbFunc.ExpressionAttribute? PickExpressionAttribute(
			DbFunc.ExpressionAttribute[] attrs,
			string? configuration)
		{
			if (attrs.Length == 0)
				return null;
			if (attrs.Length == 1)
				return attrs[0];

			DbFunc.ExpressionAttribute? fallback = null;
			foreach (var attr in attrs)
			{
				if (string.IsNullOrEmpty(attr.Configuration))
				{
					fallback ??= attr;
					continue;
				}

				if (configuration != null &&
				    string.Equals(attr.Configuration, configuration, StringComparison.OrdinalIgnoreCase))
					return attr;
			}

			return fallback ?? attrs[0];
		}

		public static DbFunc.TableFunctionAttribute? GetTableFunctionAttribute(this MemberInfo member, DBInstance mappingSchema)
		{
			//return mappingSchema.GetAttribute<DbFunc.TableFunctionAttribute>(member.ReflectedType!, member);
			return member.GetCustomAttribute<DbFunc.TableFunctionAttribute>();
		}
	}
}
