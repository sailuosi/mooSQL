using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Expressions
{
	using Common;

	/// <summary>
	/// 驱动特有类型的定义，由驱动继承以扩展
	/// </summary>
	public class TypeWrapper
	{
		// ReSharper disable InconsistentNaming
		// Names mangled to do not create collision with Wrapped class
		/// <summary>
		/// 获取驱动支持的对象.
		/// </summary>
		public object instance_ { get; } = null!;
		// ReSharper restore InconsistentNaming

		/// <summary>
		/// Provides access to delegates, created from expressions, defined in wrapper class using
		/// following property and type mappings, configured for <see cref="TypeMapper"/>:
		/// <code>
		/// private static IEumerable&lt;T&gt; Wrappers { get; }
		/// </code>
		/// where T could be <see cref="LambdaExpression"/> or <see>Tuple&lt;LambdaExpression, bool&gt;</see>.
		/// Boolean flag means that mapping expression compilation allowed to fail if it is set to <c>true</c>.
		/// This could be used to map optional API, that present only in specific versions of provider.
		/// If wrapper doesn't need any wrapper delegates, this property could be ommited.
		/// </summary>
		protected Delegate[] CompiledWrappers { get; } = null!;

		/// <summary>
		/// This constructor is never called and used only as base constructor for constructor signatures
		/// in child class.
		/// </summary>
		protected TypeWrapper()
		{
		}

		/// <summary>
		/// 真正的构造器
		/// </summary>
		/// <param name="instance">Instance of wrapped provider-specific type.</param>
		/// <param name="wrappers">Built delegates for wrapper to call base wrapped type functionality.</param>
		protected TypeWrapper(object instance, Delegate[]? wrappers)
		{
			instance_        = instance;
			CompiledWrappers = wrappers ?? new Delegate[] { };
		}

		/// <summary>
		/// Creates property setter expression from property getter.
		/// Limitation: property should have getter.
		/// </summary>
		protected static Expression<Action<TI, TP>> PropertySetter<TI, TP>(Expression<Func<TI, TP>> getter)
		{
			if (getter.Body is not MemberExpression { Member: PropertyInfo pi })
				throw new LinqToDBException($"Expected property accessor expression");

			var pThis  = Expression.Parameter(typeof(TI));
			var pValue = Expression.Parameter(typeof(TP));

			// use setter call instead of assign, as assign returns value and TypeMapper.BuildWrapper
			// produce Func instead of Action
			return Expression.Lambda<Action<TI, TP>>(
				Expression.Call(
					pThis,
					pi.SetMethod!,
					pValue),
				pThis, pValue);
		}
	}
}
