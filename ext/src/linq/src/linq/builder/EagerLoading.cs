using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace mooSQL.linq.Linq.Builder
{
	using mooSQL.linq.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;
    using mooSQL.data.model;
    using mooSQL.utils;
    using mooSQL.data;

    internal sealed class EagerLoading
	{
		static bool IsDetailType(Type type)
		{
			var isEnumerable = type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type);

			if (!isEnumerable && type.IsClass && type.IsGenericType && type.Name.StartsWith("<>"))
			{
				isEnumerable = type.GenericTypeArguments.Any(IsDetailType);
			}

			return isEnumerable;
		}


		public static Type GetEnumerableElementType(Type type, DBInstance mappingSchema)
		{
			if (!IsEnumerableType(type, mappingSchema))
				return type;
			if (type.IsArray)
				return type.GetElementType()!;
			if (typeof(IGrouping<,>).IsSameOrParentOf(type))
				return type.GetGenericArguments()[1];
			return type.GetGenericArguments()[0];
		}

		public static bool IsEnumerableType(Type type, DBInstance mappingSchema)
		{
			//if (mappingSchema.IsScalarType(type))
			//	return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
		}



	}
}
