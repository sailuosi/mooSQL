using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class SqlQueryHelperVisitorBase : ClauseVisitor
	{
        public VisitMode VisitingMode;
        public SqlQueryHelperVisitorBase()
		{
            VisitingMode = VisitMode.ReadOnly;
        }

		public override Clause VisitColumnWord(ColumnWord column)
		{
			Visit(column);
			return base.VisitColumnWord(column);
		}

        public override Clause VisitTableWord(TableWord element)
		{
			return base.VisitTableWord(element);
		}
	}
}
