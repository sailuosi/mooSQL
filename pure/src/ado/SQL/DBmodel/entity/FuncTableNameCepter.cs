using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class FuncTableNameCepter : ITableNameInterceptor
    {


        public FuncTableNameCepter(object act) { 
            this.action = act;
        }

        private object action;

        public string Parse<T>(T value)
        {
            var act=this.action as Func<T, string>;
            if (act != null) {
                return act(value);
            }
            return null;
        }
    }
}
