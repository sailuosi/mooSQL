using mooSQL.data;
using mooSQL.data.call;
using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    internal class WhereMethodVisitor : MethodVisitor
    {

        private FastCompileContext _context;

        private ConditionVisitor Buddy;
        public WhereMethodVisitor(FastCompileContext cont,ConditionVisitor link) {
            this.Buddy = link;
            this._context = cont;
        }


        public override MethodCall VisitContains(ContainsCall method)
        {
            var prop= method.callExpression.Object;

            var fie= Buddy.VisitToGotField(prop);
            var argu = method.Arguments[0];
            var val= Buddy.VisitToGotValue(argu);

            if (!string.IsNullOrWhiteSpace(fie) && val != null) { 
                _context.CurrentLayer.Current.whereLike(fie, val);
            }

            return method;
        }
        public override MethodCall VisitLike(LikeCall method)
        {
            var prop =method.Arguments[0];
            
            var fie = Buddy.VisitToGotField(prop);
            var argu = method.Arguments[1];
            var val = Buddy.VisitToGotValue(argu);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                _context.CurrentLayer.Current.whereLike(fie, val);
            }

            return method;
        }
        public override MethodCall VisitLikeLeft(LikeLeftCall method)
        {
            var prop = method.Arguments[0];

            var fie = Buddy.VisitToGotField(prop);
            var argu = method.Arguments[1];
            var val = Buddy.VisitToGotValue(argu);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                _context.CurrentLayer.Current.whereLikeLeft(fie, val);
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
            MemberInfo field = null;
            object val = null;
            var prop = method.callExpression.Object;
            if (prop != null) { 
                fie = Buddy.VisitToGotField(prop,out field);
                var argu = method.Arguments[0];
                val = Buddy.VisitToGotValue(argu);            
            }
            else if(method.Arguments.Count ==2) {
                fie = Buddy.VisitToGotField(method.Arguments[0],out field);
                val = Buddy.VisitToGotValue(method.Arguments[1]);
            }

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                try
                {
                    var vtype = val.GetType();
                    var gtype = GetMemberType( field);
                    //因为不进行类型转换，所以这里是按照值的类型进行调用，否则需要进行类型转换，这里不做处理
                    if (IsIEnumerableT(vtype))
                    {
                        var whereInType = vtype.GetGenericArguments()[0];
                        var baseTypeMem = gtype.UnwrapNullable();
                        var baseTypeVal= whereInType.UnwrapNullable();
                        if (baseTypeVal == baseTypeMem)
                        {
                            var mothod = this.GetType().GetMethod("callWhereInSQL", BindingFlags.NonPublic | BindingFlags.Instance);
                            var mot = mothod.MakeGenericMethod(whereInType);
                            mot.Invoke(this, new object[] { _context.CurrentLayer.Current, fie, val });
                        }
                        else { 
                            //参数类型不一致，尝试进行转换，未实现。
                            var mothod = this.GetType().GetMethod("callWhereInSQL", BindingFlags.NonPublic | BindingFlags.Instance);
                            var mot = mothod.MakeGenericMethod(whereInType);
                            mot.Invoke(this, new object[] { _context.CurrentLayer.Current, fie, val });                        
                        }


                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(vtype))
                    {
                        var mothod = this.GetType().GetMethod("callWhereInNotT", BindingFlags.NonPublic | BindingFlags.Instance);
                        mothod.Invoke(this, new object[] { _context.CurrentLayer.Current, fie, val });
                    }
                    else { 
                        // 
                    }


                }
                catch { }
            }



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




        private Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo p) return p.PropertyType;
            else if (member is FieldInfo f) return f.FieldType;
            return null;
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
