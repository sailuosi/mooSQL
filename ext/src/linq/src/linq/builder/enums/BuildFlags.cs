using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.builder
{
	/// <summary>
	/// BuildFlags enum.
	/// </summary>
	[Flags]
	public enum BuildFlags
	{
		/// <summary>
		/// None
		/// </summary>
		/// <summary>
		/// None
		/// </summary>
		None = 0,
		/// <summary>
		/// ForceAssignments
		/// </summary>
		/// <summary>
		/// ForceAssignments
		/// </summary>
		ForceAssignments = 0x1,
		/// <summary>
		/// ForceDefaultIfEmpty
		/// </summary>
		/// <summary>
		/// ForceDefaultIfEmpty
		/// </summary>
		ForceDefaultIfEmpty = 0x2,
		/// <summary>
		/// IgnoreRoot
		/// </summary>
		/// <summary>
		/// IgnoreRoot
		/// </summary>
		IgnoreRoot = 0x4,
	}
}
