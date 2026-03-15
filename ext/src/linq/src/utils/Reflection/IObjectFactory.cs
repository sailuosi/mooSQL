using System;

namespace mooSQL.linq.Reflection
{
	public interface IObjectFactory
	{
		object CreateInstance(TypeAccessor typeAccessor);
	}
}
