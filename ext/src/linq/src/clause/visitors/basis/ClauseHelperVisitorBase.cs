using mooSQL.data.model;

namespace mooSQL.linq.SqlQuery.Visitors
{
	public class ClauseHelperVisitorBase : ClauseVisitor
	{
        public VisitMode VisitingMode;
        public ClauseHelperVisitorBase()
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
