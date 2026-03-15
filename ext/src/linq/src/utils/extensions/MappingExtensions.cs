using System;
using System.Globalization;
using System.Reflection;
using System.Text;

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




		public static Sql.ExpressionAttribute? GetExpressionAttribute(this MemberInfo member, DBInstance mappingSchema)
		{
			//return mappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedType!, member);
			return member.GetCustomAttribute<Sql.ExpressionAttribute>();
		}

		public static Sql.TableFunctionAttribute? GetTableFunctionAttribute(this MemberInfo member, DBInstance mappingSchema)
		{
			//return mappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member.ReflectedType!, member);
			return member.GetCustomAttribute<Sql.TableFunctionAttribute>();
		}
	}
}
