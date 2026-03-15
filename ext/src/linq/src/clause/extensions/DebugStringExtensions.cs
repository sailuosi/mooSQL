using mooSQL.data.model;
using System.Collections.Generic;
using System.Xml;

namespace mooSQL.linq.SqlQuery
{
	public static class DebugStringExtensions
	{
		public static QueryElementTextWriter AppendElement<T>(this QueryElementTextWriter writer, T? element)
			where T : ISQLNode
		{
			if (element == null)
				return writer;

			element.ToString();
			return writer;
		}




		internal static string ToDebugString<T>(this T element, SelectQueryClause? selectQuery = null)
			where T : ISQLNode
		{
			try
			{
				var writer = new QueryElementTextWriter(NullabilityContext.GetContext(selectQuery));
				writer.AppendElement(element);
				return writer.ToString();
			}
			catch
			{
				return $"FAIL ToDebugString('{element.GetType().Name}').";
			}
		}
	}
}
