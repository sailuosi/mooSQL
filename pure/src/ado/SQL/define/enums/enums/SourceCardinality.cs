using System;

namespace mooSQL.data.model
{

	/// <summary>表或关联在优化器中可能的行数特征。</summary>
	[Flags]
	public enum SourceCardinality
	{
		/// <summary>未知。</summary>
		Unknown = 0,

		/// <summary>可能为零行。</summary>
		Zero = 0x1,
		/// <summary>可能恰好一行。</summary>
		One  = 0x2,
		/// <summary>可能多行。</summary>
		Many = 0x4,

		/// <summary>零或一行。</summary>
		ZeroOrOne  = Zero | One,
		/// <summary>零或多行。</summary>
		ZeroOrMany = Zero | Many,
		/// <summary>一行或多行。</summary>
		OneOrMany  = One | Many,

	}

}
