using mooSQL.data.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.call
{

    public abstract partial class MethodCall
    {

        protected MethodCall(string methodName, Type type)
        {
            // 
            if (_supportInfo == null)
            {
                _supportInfo = new ConcurrentDictionary<MethodCall, ExtensionInfo>();
            }

            _supportInfo.TryAdd(this, new ExtensionInfo(methodName, type));
        }
        ///// <summary>
        ///// 此处是为了兼容老代码
        ///// </summary>
        //protected Clause() { 

        //}

        public MethodInfo MethodInfo { get; set; }

        public ReadOnlyCollection<Expression> Arguments { get; set; }

        public MethodCallExpression callExpression { get; set; }

        private sealed class ExtensionInfo
        {
            public ExtensionInfo(string name, Type type)
            {
                MethodName = name;
                Type = type;
            }

            internal readonly string MethodName;
            internal readonly Type Type;
        }

        private static ConcurrentDictionary<MethodCall, ExtensionInfo> _supportInfo = new ConcurrentDictionary<MethodCall, ExtensionInfo>();


        public virtual string Name
        {
            get
            {
                if (_supportInfo != null && _supportInfo.TryGetValue(this, out ExtensionInfo? extInfo))
                {
                    return extInfo.MethodName;
                }

                // 
                throw new Exception("子类必须重写属性 MethodCall.MethodName");
            }
        }

        public virtual Type Type
        {
            get
            {
                if (_supportInfo != null && _supportInfo.TryGetValue(this, out ExtensionInfo? extInfo))
                {
                    return extInfo.Type;
                }

                // 
                throw new Exception("子类必须重写属性，Clause.Type");
            }
        }


        public virtual MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }

        public virtual bool CanReduce => false;

        public virtual MethodCall Reduce()
        {
            if (CanReduce) throw new Exception(" Error.ReducibleMustOverrideReduce");
            return this;
        }

        protected internal virtual MethodCall VisitChildren(MethodVisitor visitor)
        {
            if (!CanReduce)
            {
                return this;
            }
            return visitor.Visit(ReduceAndCheck());
        }

        public MethodCall ReduceAndCheck()
        {
            if (!CanReduce) throw new Exception("Error.MustBeReducible");

            MethodCall newNode = Reduce();

            // 1. Reduction must return a new, non-null node
            // 2. Reduction must return a new node whose result type can be assigned to the type of the original node
            if (newNode == null || newNode == this) throw new Exception("Error.MustReduceToDifferent");
            if (!TypeUtils.AreReferenceAssignable(Type, newNode.Type)) throw new Exception("Error.ReducedNotCompatible");
            return newNode;
        }
    }
    
}
