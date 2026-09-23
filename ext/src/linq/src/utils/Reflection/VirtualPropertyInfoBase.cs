using System.Reflection;

namespace mooSQL.linq.utils
{
	public abstract class VirtualPropertyInfoBase : PropertyInfo
	{
		public override int MetadataToken => -1;

		public override Module Module => typeof(object).Module;
	}
}
