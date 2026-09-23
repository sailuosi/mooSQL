using System.Text;

namespace mooSQL.linq.utils
{
	internal static class Pools
	{
		public static readonly ObjectPool<StringBuilder> StringBuilder = new(() => new StringBuilder(), sb => { sb.Length = 0; }, 100);
	}
}
