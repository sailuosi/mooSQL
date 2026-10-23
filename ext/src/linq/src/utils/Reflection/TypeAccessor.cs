using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace mooSQL.linq.utils
{
	/// <summary>
	/// TypeAccessor type.
	/// </summary>
	[DebuggerDisplay("Type = {" + nameof(Type) + "}")]
	public abstract class TypeAccessor
	{
		#region Protected Emit Helpers
		/// <summary>
		/// AddMember
		/// </summary>

		protected void AddMember(MemberAccessor member)
		{
			if (member == null) throw new ArgumentNullException(nameof(member));

			Members.Add(member);
			_membersByName[member.MemberInfo.Name] = member;
		}

		#endregion

		#region CreateInstance
		/// <summary>
		/// Creates CreateInstance.
		/// </summary>

		[DebuggerStepThrough]
		public virtual object CreateInstance()
		{
			throw new SooQueryException($"The '{Type.Name}' type must have public default or init constructor.");
		}
		/// <summary>
		/// Creates CreateInstanceEx.
		/// </summary>

		[DebuggerStepThrough]
		public object CreateInstanceEx()
		{
			return ObjectFactory != null ? ObjectFactory.CreateInstance(this) : CreateInstance();
		}

		#endregion

		#region Public Members
		/// <summary>
		/// Gets or sets ObjectFactory.
		/// </summary>

		public IObjectFactory?         ObjectFactory { get; set; }
		/// <summary>
		/// Type
		/// </summary>
		/// <summary>
		/// Gets or sets Type.
		/// </summary>
		public abstract Type           Type          { get; }

		#endregion

		#region Items
		/// <summary>
		/// Gets or sets Members.
		/// </summary>

		public List<MemberAccessor>    Members       { get; } = new();

		readonly ConcurrentDictionary<string,MemberAccessor> _membersByName = new();
		/// <summary>
		/// this[string]
		/// </summary>

		public MemberAccessor this[string memberName] =>
			_membersByName.GetOrAdd(memberName, name =>
			{
				var ma = new MemberAccessor(this, name, null);
				Members.Add(ma);
				return ma;
			});
		/// <summary>
		/// Gets GetMemberByName.
		/// </summary>

		public MemberAccessor? GetMemberByName(string memberName)
		{
			return _membersByName.TryGetValue(memberName, out var accessor) ? accessor : null;
		}
		/// <summary>
		/// this[int]
		/// </summary>

		public MemberAccessor this[int index] => Members[index];

		#endregion

		#region Static Members

		static readonly ConcurrentDictionary<Type,TypeAccessor> _accessors = new();
		/// <summary>
		/// Gets GetAccessor.
		/// </summary>

		public static TypeAccessor GetAccessor(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (_accessors.TryGetValue(type, out var accessor))
				return accessor;

			var accessorType = typeof(TypeAccessor<>).MakeGenericType(type);

			accessor = (TypeAccessor)Activator.CreateInstance(accessorType, true)!;

			_accessors[type] = accessor;

			return accessor;
		}
		/// <summary>
		/// Gets GetAccessor&lt;T&gt;.
		/// </summary>

		public static TypeAccessor<T> GetAccessor<T>()
		{
			if (_accessors.TryGetValue(typeof(T), out var accessor))
				return (TypeAccessor<T>)accessor;
			return (TypeAccessor<T>)(_accessors[typeof(T)] = new TypeAccessor<T>());
		}

		#endregion
	}
}
