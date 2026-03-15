using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.Linq.Builder
{
	[Flags]
	public enum BuildFlags
	{
		None = 0,
		ForceAssignments = 0x1,
		ForceDefaultIfEmpty = 0x2,
		IgnoreRoot = 0x4,
	}
}
