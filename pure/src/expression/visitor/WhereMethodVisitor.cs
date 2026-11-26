using mooSQL.data;
using mooSQL.data.call;
using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;



namespace mooSQL.linq
{
    internal class WhereMethodVisitor : MethodVisitor
    {

        private FastCompileContext _context;

        /// <summary>
        /// 关联的条件访问器（SQLClip 子类可复用字段解析）。
        /// </summary>
        protected ConditionVisitor Buddy { get; }
        /// <summary>
        /// 当前编译上下文。
        /// </summary>
        protected FastCompileContext CompileContext => _context;
        public WhereMethodVisitor(FastCompileContext cont,ConditionVisitor link) {
            this.Buddy = link;
            this._context = cont;
        }


        public override MethodCall VisitContains(ContainsCall method)
        {
            var prop= method.callExpression.Object;

            var fie= Buddy.VisitToGotColumn(prop, out var col);
            var argu = method.Arguments[0];
            var kit = _context.CurrentLayer.Current;
            var val= kit.Dialect.expression.ConvertColumnValue(col, Buddy.VisitToGotValue(argu));

            if (!string.IsNullOrWhiteSpace(fie) && val != null) { 
                kit.whereLike(fie, val);
            }

            return method;
        }
        public override MethodCall VisitEquals(EqualsCall method)
        {
            var prop = method.callExpression.Object;

            var fie = Buddy.VisitToGotColumn(prop, out var col);
            var argu = method.Arguments[0];
            var val = Buddy.VisitToGotValue(argu);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                Buddy.ApplyTypedWhereConstant(fie, col, val);
            }

            return method;
        }
        public override MethodCall VisitLike(LikeCall method)
        {
            var prop =method.Arguments[0];
            
            var fie = Buddy.VisitToGotColumn(prop, out var col);
            var argu = method.Arguments[1];
            var kit = _context.CurrentLayer.Current;
            var val = kit.Dialect.expression.ConvertColumnValue(col, Buddy.VisitToGotValue(argu));

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                kit.whereLike(fie, val);
            }

            return method;
        }
        public override MethodCall VisitLikeLeft(LikeLeftCall method)
        {
            var prop = method.Arguments[0];

            var fie = Buddy.VisitToGotColumn(prop, out var col);
            var argu = method.Arguments[1];
            var kit = _context.CurrentLayer.Current;
            var val = kit.Dialect.expression.ConvertColumnValue(col, Buddy.VisitToGotValue(argu));

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                kit.whereLikeLeft(fie, val);
            }

            return method;
        }
        /// <summary>
        /// 解析whereIn
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public override MethodCall VisitInList(InListCall method)
        {
            string fie = null;
            EntityColumn col = null;
            object val = null;
            var prop = method.callExpression.Object;
            if (prop != null) { 
                fie = Buddy.VisitToGotColumn(prop, out col);
                var argu = method.Arguments[0];
                val = Buddy.VisitToGotValue(argu);            
            }
            else if(method.Arguments.Count ==2) {
                fie = Buddy.VisitToGotColumn(method.Arguments[0], out col);
                val = Buddy.VisitToGotValue(method.Arguments[1]);
            }

            if (string.IsNullOrWhiteSpace(fie) || val == null)
                return method;

            try
            {
                var kit = _context.CurrentLayer.Current;
                var exp = kit.Dialect.expression;
                if (val is IEnumerable enumerable && val is not string)
                {
                    var converted = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        if (!exp.TryConvertColumnValue(col, item, out var v, out _))
                        {
                            kit.where("1=2");
                            return method;
                        }
                        converted.Add(v);
                    }
                    if (converted.Count > 0)
                        kit.whereIn(fie, converted);
                    return method;
                }

                var vtype = val.GetType();
                if (IsIEnumerableT(vtype))
                {
                    var whereInType = vtype.GetGenericArguments()[0];
                    var mothod = this.GetType().GetMethod("callWhereInSQL", BindingFlags.NonPublic | BindingFlags.Instance);
                    var mot = mothod.MakeGenericMethod(whereInType);
                    mot.Invoke(this, new object[] { kit, fie, val });
                }
                else if (typeof(IEnumerable).IsAssignableFrom(vtype))
                {
                    var mothod = this.GetType().GetMethod("callWhereInNotT", BindingFlags.NonPublic | BindingFlags.Instance);
                    mothod.Invoke(this, new object[] { kit, fie, val });
                }
            }
            catch { }

            return method;
        }


        public override MethodCall VisitIsNull(IsNullCall method)
        {
            var prop = method.callExpression.Object;
            var fie = Buddy.VisitToGotField(prop);

            if (!string.IsNullOrWhiteSpace(fie))
            {
                _context.CurrentLayer.Current.where(fie +" IS NULL");
            }

            return method;
        }

        public override MethodCall VisitIsNotNull(IsNotNullCall method)
        {
            var prop = method.callExpression.Object;
            var fie = Buddy.VisitToGotField(prop);

            if (!string.IsNullOrWhiteSpace(fie))
            {
                _context.CurrentLayer.Current.where(fie + " IS NOT NULL");
            }

            return method;
        }




        bool IsIEnumerableT(Type type)
        {
            return type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
        private void callWhereInSQL<T>(SQLBuilder kit,string key, IEnumerable<T> list) {
            kit.whereIn(key, list);
        }

        private void callWhereInNotT(SQLBuilder kit, string key, IEnumerable list)
        {
            kit.whereIn(key, list);
        }
    }
}
