/*
 * 存放构造Clause本身的部分功能，此部分可能因数据库不同，需要个性化
 */
using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    public partial class ClauseTranslateVisitor
    {

        public virtual IExpWord? GetIdentityExpression(FieldWord field)
        {
            return null;
        }
    }
}
