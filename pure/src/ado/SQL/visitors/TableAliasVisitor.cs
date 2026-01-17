using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public class TableAliasVisitor:ClauseVisitor
    {

        public override Clause VisitTableWord(TableWord clause)
        {
            var name= clause.Alias;
            if (string.IsNullOrWhiteSpace(name)) {
                name = clause.Name;
            }
            return new SQLFragClause(name);
        }

        public override Clause VisitDerivatedTable(DerivatedTableWord clause)
        {
            if (!string.IsNullOrWhiteSpace(clause.Name)) { 
                return new SQLFragClause(clause.Name);
            }
            var tar= VisitTableNode(clause.src);
            return tar;
        }
    }
}
