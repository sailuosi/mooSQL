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

    /// <summary>
    /// LINQ 扩展方法调用的抽象基类，承载方法名与参数表达式。
    /// </summary>
    public abstract partial class MethodCall
    {

        /// <summary>
        /// 初始化 MethodCall（构造）。
        /// </summary>
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

        /// <summary>
        /// 属性 MethodInfo（MethodInfo）。
        /// </summary>
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// 属性 Arguments（ReadOnlyCollection<Expression>）。
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments { get; set; }

        /// <summary>
        /// 属性 callExpression（MethodCallExpression）。
        /// </summary>
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


        /// <summary>
        /// 当前调用对应的方法名称。
        /// </summary>
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

        /// <summary>
        /// 当前调用关联的 CLR 类型。
        /// </summary>
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


        /// <summary>
        /// Accept 方法（返回 MethodCall）。
        /// </summary>
        public virtual MethodCall Accept(MethodVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }

        /// <summary>
        /// 是否可化简（表达式树语义，默认不可化简）。
        /// </summary>
        public virtual bool CanReduce => false;

        /// <summary>
        /// Reduce 方法（返回 MethodCall）。
        /// </summary>
        public virtual MethodCall Reduce()
        {
            if (CanReduce) throw new Exception(" Error.ReducibleMustOverrideReduce");
            return this;
        }

        /// <summary>
        /// 访问 Children 调用节点（默认返回原节点）。
        /// </summary>
        protected internal virtual MethodCall VisitChildren(MethodVisitor visitor)
        {
            if (!CanReduce)
            {
                return this;
            }
            return visitor.Visit(ReduceAndCheck());
        }

        /// <summary>
        /// ReduceAndCheck 方法（返回 MethodCall）。
        /// </summary>
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