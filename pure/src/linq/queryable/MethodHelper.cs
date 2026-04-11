using System;
using System.Reflection;

namespace mooSQL.linq
{
	// TODO: replace remaining calls in API with
	// Methods.*.MakeGenericMethod calls
	/// <summary>
	/// 通过委托实例安全获取 <see cref="MethodInfo"/>，供表达式树构造使用。
	/// </summary>
	public static class MethodHelper
	{
		/// <summary>返回委托对应的方法元数据。</summary>
		public static MethodInfo GetMethodInfo(this Delegate del)
		{
			if ((object)del == null)
				throw new ArgumentNullException(nameof(del));
			return del.Method;
		}

		#region Helper methods to obtain MethodInfo in a safe way

		/// <summary>利用泛型委托推断获取方法信息（unused 参数仅用于推断类型形参）。</summary>
		public static MethodInfo GetMethodInfo<T1,T2>(Func<T1,T2> f, T1 unused1)
		{
			return f.GetMethodInfo();
		}

		/// <summary>三参数委托重载。</summary>
		public static MethodInfo GetMethodInfo<T1,T2,T3>(Func<T1,T2,T3> f, T1 unused1, T2 unused2)
		{
			return f.GetMethodInfo();
		}

		/// <summary>四参数委托重载。</summary>
		public static MethodInfo GetMethodInfo<T1,T2,T3,T4>(Func<T1,T2,T3,T4> f, T1 unused1, T2 unused2, T3 unused3)
		{
			return f.GetMethodInfo();
		}

		/// <summary>五参数委托重载。</summary>
		public static MethodInfo GetMethodInfo<T1,T2,T3,T4,T5>(Func<T1,T2,T3,T4,T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
		{
			return f.GetMethodInfo();
		}

		/// <summary>六参数委托重载。</summary>
		public static MethodInfo GetMethodInfo<T1,T2,T3,T4,T5,T6>(Func<T1,T2,T3,T4,T5,T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
		{
			return f.GetMethodInfo();
		}

		/// <summary>七参数委托重载。</summary>
		public static MethodInfo GetMethodInfo<T1,T2,T3,T4,T5,T6, T7>(Func<T1,T2,T3,T4,T5,T6,T7> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
		{
			return f.GetMethodInfo();
		}

		#endregion
	}
}
