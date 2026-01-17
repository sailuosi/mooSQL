using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data.call
{
    /// <summary>
    /// 导航查询包含方法
    /// </summary>
    public class IncludesCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitIncludes(this);
        }
        public IncludesCall() : base("Includes", null)
        {

        }
    }
}