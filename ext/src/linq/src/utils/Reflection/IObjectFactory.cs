using System;

namespace mooSQL.linq.utils
{
	public interface IObjectFactory
	{
		object CreateInstance(TypeAccessor typeAccessor);
	}
}
