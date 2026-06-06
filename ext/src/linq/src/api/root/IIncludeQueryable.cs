using System.Collections.Generic;
using System.Linq;

namespace mooSQL.linq
{
	/// <summary>
	/// Provides support for queryable Includes/ThenInclude chaining operators.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <typeparam name="TProperty">The property type.</typeparam>
	// ReSharper disable once UnusedTypeParameter
	public interface IIncludeQueryable<out TEntity, out TProperty> : IQueryable<TEntity>
#if NET5_0_OR_GREATER
        , IAsyncEnumerable<TEntity>
#endif
    {
    }
}
