using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public class SinkCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSink(this);
        }
        public SinkCall() : base("Sink", null)
        {

        }
    }

    public class RiseCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitRise(this);
        }
        public RiseCall() : base("Rise", null)
        {

        }
    }

    public class SinkORCall : MethodCall
    {
        public override MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitSinkOR(this);
        }
        public SinkORCall() : base("SinkOR", null)
        {

        }
    }
}
