// Slimmed from EF Core SharedTypeExtensions — only members used by Ext LINQ.
// Source: https://github.com/dotnet/efcore/blob/main/src/Shared/SharedTypeExtensions.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace mooSQL.linq
{
#pragma warning disable RS0030

	[DebuggerStepThrough]
	internal static class SharedTypeExtensions
	{
		private static readonly Dictionary<Type, string> BuiltInTypeNames = new()
		{
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(char), "char" },
			{ typeof(decimal), "decimal" },
			{ typeof(double), "double" },
			{ typeof(float), "float" },
			{ typeof(int), "int" },
			{ typeof(long), "long" },
			{ typeof(object), "object" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(short), "short" },
			{ typeof(string), "string" },
			{ typeof(uint), "uint" },
			{ typeof(ulong), "ulong" },
			{ typeof(ushort), "ushort" },
			{ typeof(void), "void" }
		};

		public static Type UnwrapNullableType(this Type type)
			=> Nullable.GetUnderlyingType(type) ?? type;

		public static bool IsNullableValueType(this Type type)
			=> type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

		public static bool IsNullableType(this Type type)
			=> !type.IsValueType || type.IsNullableValueType();

		public static bool IsAnonymousType(this Type type)
			=> type.Name.StartsWith("<>", StringComparison.Ordinal)
			   && type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false).Length > 0
			   && type.Name.Contains("AnonymousType");

		public static Type? TryGetSequenceType(this Type type)
			=> type.TryGetElementType(typeof(IEnumerable<>))
#if NET5_0_OR_GREATER
				?? type.TryGetElementType(typeof(IAsyncEnumerable<>))
#endif
				;

		public static Type? TryGetElementType(this Type type, Type interfaceOrBaseType)
		{
			if (type.IsGenericTypeDefinition)
				return null;

			var types = GetGenericTypeImplementations(type, interfaceOrBaseType);

			Type? singleImplementation = null;
			foreach (var implementation in types)
			{
				if (singleImplementation == null)
					singleImplementation = implementation;
				else
				{
					singleImplementation = null;
					break;
				}
			}

			return singleImplementation?.GenericTypeArguments.FirstOrDefault();
		}

		public static IEnumerable<Type> GetGenericTypeImplementations(this Type type, Type interfaceOrBaseType)
		{
			var typeInfo = type.GetTypeInfo();
			if (!typeInfo.IsGenericTypeDefinition)
			{
				var baseTypes = interfaceOrBaseType.GetTypeInfo().IsInterface
					? typeInfo.ImplementedInterfaces
					: type.GetBaseTypes();
				foreach (var baseType in baseTypes)
				{
					if (baseType.IsGenericType
						&& baseType.GetGenericTypeDefinition() == interfaceOrBaseType)
					{
						yield return baseType;
					}
				}

				if (type.IsGenericType
					&& type.GetGenericTypeDefinition() == interfaceOrBaseType)
				{
					yield return type;
				}
			}
		}

		public static IEnumerable<Type> GetBaseTypes(this Type type)
		{
			var currentType = type.BaseType;

			while (currentType != null)
			{
				yield return currentType;
				currentType = currentType.BaseType;
			}
		}

		public static string DisplayName(this Type type, bool fullName = true, bool compilable = false)
		{
			var stringBuilder = new StringBuilder();
			ProcessType(stringBuilder, type, fullName, compilable);
			return stringBuilder.ToString();
		}

		private static void ProcessType(StringBuilder builder, Type type, bool fullName, bool compilable)
		{
			if (type.IsGenericType)
			{
				var genericArguments = type.GetGenericArguments();
				ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName, compilable);
			}
			else if (type.IsArray)
			{
				ProcessArrayType(builder, type, fullName, compilable);
			}
			else if (BuiltInTypeNames.TryGetValue(type, out var builtInName))
			{
				builder.Append(builtInName);
			}
			else if (!type.IsGenericParameter)
			{
				if (compilable)
				{
					if (type.IsNested)
					{
						ProcessType(builder, type.DeclaringType!, fullName, compilable);
						builder.Append('.');
					}
					else if (fullName)
					{
						builder.Append(type.Namespace).Append('.');
					}

					builder.Append(type.Name);
				}
				else
				{
					builder.Append(fullName ? type.FullName : type.Name);
				}
			}
		}

		private static void ProcessArrayType(StringBuilder builder, Type type, bool fullName, bool compilable)
		{
			var innerType = type;
			while (innerType.IsArray)
				innerType = innerType.GetElementType()!;

			ProcessType(builder, innerType, fullName, compilable);

			while (type.IsArray)
			{
				builder.Append('[');
				builder.Append(',', type.GetArrayRank() - 1);
				builder.Append(']');
				type = type.GetElementType()!;
			}
		}

		private static void ProcessGenericType(
			StringBuilder builder,
			Type type,
			Type[] genericArguments,
			int length,
			bool fullName,
			bool compilable)
		{
			if (type.IsConstructedGenericType
				&& type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				ProcessType(builder, type.UnwrapNullableType(), fullName, compilable);
				builder.Append('?');
				return;
			}

			var offset = type.IsNested ? type.DeclaringType!.GetGenericArguments().Length : 0;

			if (compilable)
			{
				if (type.IsNested)
				{
					ProcessType(builder, type.DeclaringType!, fullName, compilable);
					builder.Append('.');
				}
				else if (fullName)
				{
					builder.Append(type.Namespace);
					builder.Append('.');
				}
			}
			else
			{
				if (fullName)
				{
					if (type.IsNested)
					{
						ProcessGenericType(builder, type.DeclaringType!, genericArguments, offset, fullName, compilable);
						builder.Append('+');
					}
					else
					{
						builder.Append(type.Namespace);
						builder.Append('.');
					}
				}
			}

			var genericPartIndex = type.Name.IndexOf('`');
			if (genericPartIndex <= 0)
			{
				builder.Append(type.Name);
				return;
			}

			builder.Append(type.Name, 0, genericPartIndex);
			builder.Append('<');

			for (var i = offset; i < length; i++)
			{
				ProcessType(builder, genericArguments[i], fullName, compilable);
				if (i + 1 == length)
					continue;

				builder.Append(',');
				if (!genericArguments[i + 1].IsGenericParameter)
					builder.Append(' ');
			}

			builder.Append('>');
		}
	}
}
