using System;

namespace mooSQL.linq.utils
{
	/// <summary>
	/// ObjectFactoryAttribute type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		/// <summary>
		/// ObjectFactoryAttribute type.
		/// </summary>
		/// <summary>
		/// ObjectFactoryAttribute type.
		/// </summary>
		public ObjectFactoryAttribute(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			ObjectFactory = (Activator.CreateInstance(type) as IObjectFactory)!;

			if (ObjectFactory == null)
				throw new ArgumentException($"Type '{type}' does not implement IObjectFactory interface.");
		}
		/// <summary>
		/// Gets or sets ObjectFactory.
		/// </summary>

		public IObjectFactory ObjectFactory { get; }
	}
}
