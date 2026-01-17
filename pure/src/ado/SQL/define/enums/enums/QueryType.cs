using System;

namespace mooSQL.data.model
{
	public enum QueryType
	{
		Select,
		Delete,
		Update,
		Insert,
		InsertOrUpdate,
		CreateTable,
		DropTable,
		TruncateTable,
		Merge,
		MultiInsert,
	}
}
