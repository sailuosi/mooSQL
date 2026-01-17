using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.model
{
    public class JoinOnWord
    {

        public SearchConditionWord condition;

        public JoinOnWord() { }

        public JoinOnWord(SearchConditionWord condition) { this.condition = condition; }    
    }
}
