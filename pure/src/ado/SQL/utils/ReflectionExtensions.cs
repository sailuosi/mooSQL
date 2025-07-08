using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;



namespace mooSQL.data.Extensions
{

    using mooSQL.utils;

    public static class ReflectionExtensions
	{
		#region Type extensions
		public static MemberInfo[] GetPublicInstanceMembersEx(this Type type)
		{
			return type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
		}

		public static MemberInfo[] GetPublicInstanceValueMembers(this Type type)
		{
			if (type.IsInterface)
				return GetInterfacePublicInstanceValueMembers(type);

			var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
				.Where(m => m.IsFieldEx() || m.IsPropertyEx() && ((PropertyInfo)m).GetIndexParameters().Length == 0)
				.ToArray();

			var baseType = type.BaseType;
			if (baseType == null || baseType == typeof(object) || baseType == typeof(ValueType))
				return members;

			// in the case of inheritance, we want to:
			//  - list base class members first
			//  - remove shadowed members (new modifier)
			//	- preserve the order of GetMembers() inside the same type declared type

			var results = new List<MemberInfo>(members.Length);
			var seen    = new HashSet<string>();
			for (var t = type; t != typeof(object) && t != typeof(ValueType); t = t.BaseType!)
			{
				// iterating in reverse order because we will reverse it
				// again in the end to list base class members first
				for (var j = members.Length - 1; j >= 0; j--)
				{
					var m = members[j];
					if (m.DeclaringType == t && seen.Add(m.Name))
					{
						results.Add(m);
					}
				}
			}

			results.Reverse();

			return results.ToArray();
		}

		private static MemberInfo[] GetInterfacePublicInstanceValueMembers(Type type)
		{
			var members = type
				.GetMembers(BindingFlags.Instance | BindingFlags.Public)
				.Where(m => m.IsFieldEx() || m.IsPropertyEx() && ((PropertyInfo)m).GetIndexParameters().Length == 0);

			var interfaces = type.GetInterfaces();
			if (interfaces.Length == 0)
				return members.ToArray();
			else
			{
				var results = members.ToList();
				var seen    = new HashSet<string>(results.Select(m => m.Name));

				foreach (var iface in interfaces)
				{
					foreach (var member in iface
						.GetMembers(BindingFlags.Instance | BindingFlags.Public)
						.Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property && ((PropertyInfo)m).GetIndexParameters().Length == 0))
					{
						if (seen.Add(member.Name))
							results.Add(member);
					}
				}

				return results.ToArray();
			}
		}

		public static MemberInfo[] GetStaticMembersEx(this Type type, string name)
		{
			return type.GetMember(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		}



        private static readonly ConcurrentDictionary<string, MemberInfo?> _getMemberExCache = new();
		/// <summary>
		/// Returns <see cref="MemberInfo"/> of <paramref name="type"/> described by <paramref name="memberInfo"/>
		/// It us useful when member's declared and reflected types are not the same.
		/// </summary>
		/// <remarks>This method searches only properties, fields and methods</remarks>
		/// <param name="type"><see cref="Type"/> to find member info</param>
		/// <param name="memberInfo"><see cref="MemberInfo"/> </param>
		/// <returns><see cref="MemberInfo"/> or null</returns>
		public static MemberInfo? GetMemberEx(this Type type, MemberInfo memberInfo)
		{
			var key= type.Name+"."+memberInfo.Name;
			return _getMemberExCache.GetOrAdd(key, key =>
			{
				

                if (memberInfo.ReflectedType == type)
					return memberInfo;

				if (memberInfo.IsPropertyEx())
				{
					var props = type.GetProperties();

					PropertyInfo? foundByName = null;
					foreach (var prop in props)
					{
						if (prop.Name == memberInfo.Name)
						{
							foundByName ??= prop;
							if (prop.GetMemberType() == memberInfo.GetMemberType())
							{
								return prop;
							}
						}
					}

					return foundByName;
				}

				if (memberInfo.IsFieldEx())
					return type.GetField(memberInfo.Name);

				if (memberInfo.IsMethodEx())
					return type.GetMethodEx(memberInfo.Name, ((MethodInfo)memberInfo).GetParameters().Select(_ => _.ParameterType).ToArray());

				return null;
			});
		}

		public static MethodInfo? GetMethodEx(this Type type, string name)
		{
			return type.GetMethod(name);
		}

		/// <summary>
		/// Gets method by name, input parameters and return type.
		/// Usefull for method overloads by return type, like op_Explicit/op_Implicit conversions.
		/// </summary>
		public static MethodInfo? GetMethodEx(this Type type, Type returnType, string name, params Type[] types)
		{
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (method.Name == name && method.ReturnType == returnType)
				{
					var parameters = method.GetParameters();
					if (parameters.Length != types.Length)
						continue;

					var found = true;
					for (var i = 0; i < types.Length; i++)
					{
						if (types[i] != parameters[i].ParameterType)
						{
							found = false;
							break;
						}
					}

					if (found)
						return method;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets generic method.
		/// </summary>
		public static MethodInfo? GetMethodEx(this Type type, string name, int genericParametersCount, params Type[] types)
		{
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (method.IsGenericMethod
					&& method.Name == name
					&& method.GetGenericMethodDefinition().GetGenericArguments().Length == genericParametersCount)
				{
					var parameters = method.GetParameters();
					if (parameters.Length == types.Length)
					{
						var found = true;
						for (var i = 0; i < types.Length; i++)
						{
							if (!parameters[i].ParameterType.IsGenericParameter && parameters[i].ParameterType != types[i])
							{
								found = false;
								break;
							}
						}

						if (found)
							return method;
					}
				}
			}

			return null;
		}

		public static MethodInfo? GetMethodEx(this Type type, string name, params Type[] types)
		{
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
		}

		public static MethodInfo? GetPublicInstanceMethodEx(this Type type, string name, params Type[] types)
		{
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, types, null);
		}

		public static ConstructorInfo? GetDefaultConstructorEx(this Type type)
		{
			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				Type.EmptyTypes,
				null);
		}

		public static TypeCode GetTypeCodeEx(this Type type)
		{
			return Type.GetTypeCode(type);
		}





		public static PropertyInfo[] GetPropertiesEx(this Type type)
		{
			return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		internal static IEnumerable<PropertyInfo> GetDeclaredPropertiesEx(this Type type)
		{
			foreach (var pi in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
				if (pi.DeclaringType == type && pi.GetIndexParameters().Length == 0)
					yield return pi;
		}

		public static PropertyInfo[] GetNonPublicPropertiesEx(this Type type)
		{
			return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static MemberInfo[] GetInstanceMemberEx(this Type type, string name)
		{
			return type.GetMember(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static MemberInfo[] GetPublicMemberEx(this Type type, string name)
		{
			return type.GetMember(name);
		}

		// GetInterfaceMap could fail in AOT builds
		// - CoreRT runtime could miss this method implementation
		// - NativeAOT builds at least up to .NET 8 doesn't support interfaces with statics or/and default implementations
		// for such cases we return empty stub
		public static InterfaceMapping GetInterfaceMapEx(this Type type, Type interfaceType)
		{
			try
			{
				return type.GetInterfaceMap(interfaceType);
			}
			// PNSE: corert
			// NSE: NativeAOUT
			catch (Exception ex) when (ex is NotSupportedException or PlatformNotSupportedException)
			{
				return new InterfaceMapping()
				{
					TargetType       = type,
					InterfaceType    = interfaceType,
					TargetMethods    = [],
					InterfaceMethods = []
				};
			}
		}



		public static IEnumerable<Type> GetDefiningTypes(this Type child, MemberInfo member)
		{
			if (member.IsPropertyEx())
			{
				var prop = (PropertyInfo)member;
				member = prop.GetGetMethod()!;
			}

			foreach (var inf in child.GetInterfaces())
			{
				var pm = child.GetInterfaceMapEx(inf);

				for (var i = 0; i < pm.TargetMethods.Length; i++)
				{
					var method = pm.TargetMethods[i];

					if (!method.IsStatic && (method == member || (method.DeclaringType == member.DeclaringType && method.Name == member.Name)))
						yield return inf;
				}
			}

			yield return member.DeclaringType!;
		}



		public static Type? GetGenericType(this Type genericType, Type type)
		{
			if (genericType == null) throw new ArgumentNullException(nameof(genericType));

			while (type != typeof(object))
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
					return type;

				if (genericType.IsInterface)
				{
					foreach (var interfaceType in type.GetInterfaces())
					{
						var gType = GetGenericType(genericType, interfaceType);

						if (gType != null)
							return gType;
					}
				}

				if (type.BaseType == null)
					break;

				type = type.BaseType;
			}

			return null;
		}

		public static IEnumerable<Type> GetGenericTypes(this Type genericType, Type type)
		{
			if (genericType == null) throw new ArgumentNullException(nameof(genericType));

			while (type != typeof(object))
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
					yield return type;

				if (genericType.IsInterface)
				{
					foreach (var interfaceType in type.GetInterfaces())
					{
						foreach (var gType in GetGenericTypes(genericType, interfaceType))
							yield return gType;
					}
				}

				if (type.BaseType == null)
					break;

				type = type.BaseType;
			}
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="list">A <see cref="System.Object"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(this IEnumerable? list)
		{
			var typeOfObject = typeof(object);

			if (list == null)
				return typeOfObject;

			if (list is Array)
				return list.GetType().GetElementType()!;

			var type = list.GetType();

			if (list is IList || list is ITypedList || list is IListSource)
			{
				PropertyInfo? last = null;

				foreach (var pi in type.GetPropertiesEx())
				{
					if (pi.GetIndexParameters().Length > 0 && pi.PropertyType != typeOfObject)
					{
						if (pi.Name == "Item")
							return pi.PropertyType;

						last = pi;
					}
				}

				if (last != null)
					return last.PropertyType;
			}

			if (list is IList list1)
			{
				foreach (var o in list1)
					if (o != null && o.GetType() != typeOfObject)
						return o.GetType();
			}
			else
			{
				foreach (var o in list)
					if (o != null && o.GetType() != typeOfObject)
						return o.GetType();
			}

			return typeOfObject;
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="listType">A <see cref="Type"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(this Type listType)
		{
			if (listType.IsGenericType)
			{
				var elementTypes = listType.GetGenericArguments(typeof(IList<>));

				if (elementTypes != null)
					return elementTypes[0];
			}

			if (typeof(IList).      IsSameOrParentOf(listType) ||
				typeof(ITypedList). IsSameOrParentOf(listType) ||
				typeof(IListSource).IsSameOrParentOf(listType))
			{
				var elementType = listType.GetElementType();

				if (elementType != null)
					return elementType;

				PropertyInfo? last = null;

				foreach (var pi in listType.GetPropertiesEx())
				{
					if (pi.GetIndexParameters().Length > 0 && pi.PropertyType != typeof(object))
					{
						if (pi.Name == "Item")
							return pi.PropertyType;

						last = pi;
					}
				}

				if (last != null)
					return last.PropertyType;
			}

			return typeof(object);
		}

		public static bool IsEnumerableType(this Type type, Type elementType)
		{
			return typeof(IEnumerable<>).GetGenericTypes(type)
				.Any(t => t.GetGenericArguments()[0].IsSameOrParentOf(elementType));
		}

		public static bool IsGenericEnumerableType(this Type type)
		{
			if (type.IsGenericType)
				if (typeof(IEnumerable<>).IsSameOrParentOf(type))
					return true;
			return false;
		}

		static readonly ConcurrentDictionary<Type,Type?> _getItemTypeCache = new ();

		public static Type? GetItemType(this Type? type)
		{
			if (type == null)
				return null;

			return _getItemTypeCache.GetOrAdd(type, static t =>
			{
				if (t == typeof(object))
					return null;

				if (t.IsArray)
					return t.GetElementType();

				if (t.IsGenericType)
					foreach (var aType in t.GetGenericArguments())
						if (typeof(IEnumerable<>).MakeGenericType(new[] {aType})
							.IsAssignableFrom(t))
							return aType;

				var interfaces = t.GetInterfaces();

				if (interfaces != null && interfaces.Length > 0)
				{
					foreach (var iType in interfaces)
					{
						var eType = iType.GetItemType();

						if (eType != null)
							return eType;
					}
				}

				return t.BaseType.GetItemType();
			});
		}



		///<summary>
		/// Returns an array of Type objects that represent the type arguments
		/// of a generic type or the type parameters of a generic type definition.
		///</summary>
		/// <param name="type">A <see cref="Type"/> instance.</param>
		///<param name="baseType">Non generic base type.</param>
		///<returns>An array of Type objects that represent the type arguments
		/// of a generic type. Returns an empty array if the current type is not a generic type.</returns>
		public static Type[]? GetGenericArguments(this Type type, Type baseType)
		{
			var baseTypeName = baseType.Name;

			for (var t = type; t != typeof(object) && t != null; t = t.BaseType)
			{
				if (t.IsGenericType)
				{
					if (baseType.IsGenericTypeDefinition)
					{
						if (t.GetGenericTypeDefinition() == baseType)
							return t.GetGenericArguments();
					}
					else if (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName)
					{
						return t.GetGenericArguments();
					}
				}
			}

			foreach (var t in type.GetInterfaces())
			{
				if (t.IsGenericType)
				{
					if (baseType.IsGenericTypeDefinition)
					{
						if (t.GetGenericTypeDefinition() == baseType)
							return t.GetGenericArguments();
					}
					else if (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName)
					{
						return t.GetGenericArguments();
					}
				}
			}

			return null;
		}





		interface IGetDefaultValueHelper
		{
			object? GetDefaultValue();
		}

		sealed class GetDefaultValueHelper<T> : IGetDefaultValueHelper
		{
			public object? GetDefaultValue()
			{
				return default(T)!;
			}
		}



		public static EventInfo? GetEventEx(this Type type, string eventName)
		{
			return type.GetEvent(eventName);
		}

#endregion

		#region MethodInfo extensions

		//[return: NotNullIfNotNull(nameof(method))]
		public static PropertyInfo? GetPropertyInfo(this MethodInfo? method)
		{
			if (method != null)
			{
				var type = method.DeclaringType!;

				foreach (var info in type.GetPropertiesEx())
				{
					if (info.CanRead && method == info.GetGetMethod(true))
						return info;

					if (info.CanWrite && method == info.GetSetMethod(true))
						return info;
				}
			}

			return null;
		}

		#endregion

		#region MemberInfo extensions



		static readonly Dictionary<Type,HashSet<Type>> _castDic = new ()
		{
			{ typeof(decimal), new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char)                } },
			{ typeof(double),  new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
			{ typeof(float),   new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
			{ typeof(ulong),   new HashSet<Type> { typeof(byte),  typeof(ushort), typeof(uint),  typeof(char)                                                                                        } },
			{ typeof(long),    new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(char)                                             } },
			{ typeof(uint),    new HashSet<Type> { typeof(byte),  typeof(ushort), typeof(char)                                                                                                       } },
			{ typeof(int),     new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(char)                                                                        } },
			{ typeof(ushort),  new HashSet<Type> { typeof(byte),  typeof(char)                                                                                                                       } },
			{ typeof(short),   new HashSet<Type> { typeof(byte)                                                                                                                                      } }
		};

		public static bool CanConvertTo(this Type fromType, Type toType)
		{
			if (fromType == toType)
				return true;

			if (toType.IsAssignableFrom(fromType))
				return true;

			if (_castDic.TryGetValue(toType, out var types) && types.Contains(fromType))
				return true;

			foreach (var m in fromType.GetMethods())
			{
				if (m.IsStatic && m.IsPublic && m.ReturnType == toType && (m.Name == "op_Implicit" || m.Name == "op_Explicit"))
					return true;
			}

			return false;
		}

		public static bool EqualsTo(this MemberInfo? member1, MemberInfo? member2, Type? declaringType = null)
		{
			if (ReferenceEquals(member1, member2))
				return true;

			if (member1 == null || member2 == null)
				return false;

			if (member1.Name == member2.Name && member1.DeclaringType == member2.DeclaringType)
				return true;

			if (member1 is not PropertyInfo || member2 is not PropertyInfo)
				return false;

			if (!member1.DeclaringType!.IsInterface && !member2.DeclaringType!.IsInterface)
			{
				if (member1.Name != member2.Name)
					return false;

				// Looks like it will not handle "new" properties case properly
				var isSubclass = member1.DeclaringType!.IsSameOrParentOf(member2.DeclaringType!) ||
								 member2.DeclaringType!.IsSameOrParentOf(member1.DeclaringType!);

				return isSubclass;
			}

			if (member1.DeclaringType!.IsInterface && member2.DeclaringType!.IsInterface)
				return false;

			// interface vs class property comparison inhuman logic
			// we probably will be able to implement it in more clear way after
			// https://github.com/dotnet/runtime/issues/81299
			// implemented, but for now we need to use partial name match for implicit implementations
			if (declaringType == null || declaringType.IsInterface)
				declaringType = member2.DeclaringType!.IsInterface ? member1.DeclaringType! : member2.DeclaringType!;

			// member1 should reference class property
			// member2 should reference interface property
			if (member1.DeclaringType!.IsInterface)
				//(member1, member2) = (member2, member1);

			if (!member2.DeclaringType!.IsSameOrParentOf(declaringType))
				return false;

			// we use ".<PROPERTY_NAME>" suffix to match implicit implementations by name
			// it potentially could lead to name conflicts but it's best we can do as full name generation logic is not easy
			if (member1.Name == member2.Name || member1.Name.EndsWith($".{member2.Name}"))
			{
				var getter1 = ((PropertyInfo)member1).GetMethod!;
				var getter2 = ((PropertyInfo)member2).GetMethod!;

				var map = declaringType.GetInterfaceMapEx(member2.DeclaringType!);

				for (var i = 0; i < map.InterfaceMethods.Length; i++)
					if (!map.InterfaceMethods[i].IsStatic &&
						map.InterfaceMethods[i] == getter2 &&
						(map.TargetMethods[i] == getter1 ||
						(map.TargetMethods[i].Name == getter1.Name && map.TargetMethods[i].DeclaringType == getter1.DeclaringType)))
						return true;

				// (see Issue4031_Case01 test)
				// This code tries to handle very special case when class implements interface
				// using members of base class, declared in another assembly
				// in such cases compiler generates proxy property accessors without property on target class
				//
				// In that case targetMethod reference proxy method, but member1 property references real getter
				// from base type, which results in failed comparison above
				var accessorNameEnd = $".{getter1.Name}";
				for (var i = 0; i < map.InterfaceMethods.Length; i++)
				{
					if (declaringType == map.TargetMethods[i].DeclaringType && map.TargetMethods[i].Name.EndsWith(accessorNameEnd))
					{
						// now we need to check that target method has no property to avoid false matches
						var targetMethod = map.TargetMethods[i];
						var isProxy      = true;

						foreach (var pi in targetMethod.DeclaringType!.GetDeclaredPropertiesEx())
						{
							if (pi.GetMethod == targetMethod)
							{
								isProxy = false;
								break;
							}
						}

						if (isProxy)
							return true;
					}
				}
			}

			return false;
		}

		#endregion



		internal static MemberInfo GetMemberOverride(this Type type, MemberInfo mi)
		{
			if (mi.DeclaringType == type)
				return mi;

			if (mi is MethodInfo method)
			{
				var baseDefinition = method.GetBaseDefinition();

				foreach (var m in type.GetMethods())
					if (m.GetBaseDefinition() == baseDefinition)
						return m;
			}
			else if (mi is PropertyInfo property)
			{
				if (property.GetMethod != null)
				{
					var baseDefinition = property.GetMethod.GetBaseDefinition();

					foreach (var p in type.GetProperties())
						if (p.GetMethod?.GetBaseDefinition() == baseDefinition)
							return p;
				}
				if (property.SetMethod != null)
				{
					var baseDefinition = property.SetMethod.GetBaseDefinition();

					foreach (var p in type.GetProperties())
						if (p.SetMethod?.GetBaseDefinition() == baseDefinition)
							return p;
				}
			}

			return mi;
		}

		static ConcurrentDictionary<MethodInfo, MethodInfo> _methodDefinitionCache = new ();

		internal static MethodInfo GetGenericMethodDefinitionCached(this MethodInfo method)
		{
			if (!method.IsGenericMethod || method.IsGenericMethodDefinition)
				return method;

			return _methodDefinitionCache.GetOrAdd(method, static mi => mi.GetGenericMethodDefinition());
		}

		/// <summary>
		/// Checks that source type <paramref name="targetType"/> has setter for <paramref name="property"/>.
		/// Supports non-public setters and read-only interface property implementations with setter.
		/// In other words, checks that property on <paramref name="targetType"/> is writeable.
		/// </summary>
		/// <param name="property">Replaces with implementation property if original property is readonly interface property.</param>
		internal static bool HasSetter(this Type targetType, ref PropertyInfo property)
		{
			if (property.SetMethod != null)
				return true;

			if (property.GetMethod == null)
				return false;

			// search for interface property implementation
			if (targetType != property.DeclaringType && targetType.IsClass && property.DeclaringType!.IsInterface)
			{
				var map = targetType.GetInterfaceMapEx(property.DeclaringType!);
				for (var i = 0; i < map.InterfaceMethods.Length; i++)
				{
					if (!map.InterfaceMethods[i].IsStatic && map.InterfaceMethods[i] == property.GetMethod)
					{
						// find implementation property and check if it has setter
						foreach (var prop in map.TargetType.GetProperties())
							if (prop.GetMethod == map.TargetMethods[i])
							{
								property = prop;
								return true;
							}
					}
				}
			}

			return false;
		}
	}
}
