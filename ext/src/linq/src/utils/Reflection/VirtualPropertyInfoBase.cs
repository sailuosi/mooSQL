using System.Reflection;

namespace mooSQL.linq.utils
{
	/// <summary>
	/// VirtualPropertyInfoBase type.
	/// </summary>
	/// <summary>
	/// VirtualPropertyInfoBase type.
	/// </summary>
	public abstract class VirtualPropertyInfoBase : PropertyInfo
	{
	/// <summary>
	/// MetadataToken
	/// </summary>
	
		public override int MetadataToken => -1;
/// <summary>
/// Module
/// </summary>
	

		public override Module Module => typeof(object).Module;
	}
}
