using mooSQL.data.call;
using mooSQL.linq;
using mooSQL.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.data.clip
{
    /// <summary>
    /// SQLClip 专用方法型条件：在调用 SQLBuilder 前按列信息变换枚举值。
    /// </summary>
    internal class ClipWhereMethodVisitor : WhereMethodVisitor
    {
        private readonly ClipConditionVisitor _clipBuddy;

        public ClipWhereMethodVisitor(FastCompileContext cont, ClipConditionVisitor link)
            : base(cont, link)
        {
            _clipBuddy = link;
        }

        public override MethodCall VisitEquals(EqualsCall method)
        {
            var prop = method.callExpression.Object;
            var fie = _clipBuddy.VisitToGotField(prop, out var col);
            var argu = method.Arguments[0];
            var val = EnumColumnMapping.ResolveClipValue(_clipBuddy.VisitToGotValue(argu), col);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                CompileContext.CurrentLayer.Current.where(fie, val);
            }

            return method;
        }

        public override MethodCall VisitContains(ContainsCall method)
        {
            var prop = method.callExpression.Object;
            var fie = _clipBuddy.VisitToGotField(prop, out var col);
            var argu = method.Arguments[0];
            var val = EnumColumnMapping.ResolveClipValue(_clipBuddy.VisitToGotValue(argu), col);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                CompileContext.CurrentLayer.Current.whereLike(fie, val);
            }

            return method;
        }

        public override MethodCall VisitLike(LikeCall method)
        {
            var prop = method.Arguments[0];
            var fie = _clipBuddy.VisitToGotField(prop, out var col);
            var argu = method.Arguments[1];
            var val = EnumColumnMapping.ResolveClipValue(_clipBuddy.VisitToGotValue(argu), col);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                CompileContext.CurrentLayer.Current.whereLike(fie, val);
            }

            return method;
        }

        public override MethodCall VisitLikeLeft(LikeLeftCall method)
        {
            var prop = method.Arguments[0];
            var fie = _clipBuddy.VisitToGotField(prop, out var col);
            var argu = method.Arguments[1];
            var val = EnumColumnMapping.ResolveClipValue(_clipBuddy.VisitToGotValue(argu), col);

            if (!string.IsNullOrWhiteSpace(fie) && val != null)
            {
                CompileContext.CurrentLayer.Current.whereLikeLeft(fie, val);
            }

            return method;
        }

        public override MethodCall VisitInList(InListCall method)
        {
            string fie = null;
            EntityColumn col = null;
            object val = null;
            var prop = method.callExpression.Object;
            if (prop != null)
            {
                fie = _clipBuddy.VisitToGotField(prop, out col);
                val = _clipBuddy.VisitToGotValue(method.Arguments[0]);
            }
            else if (method.Arguments.Count == 2)
            {
                fie = _clipBuddy.VisitToGotField(method.Arguments[0], out col);
                val = _clipBuddy.VisitToGotValue(method.Arguments[1]);
            }

            if (string.IsNullOrWhiteSpace(fie) || val == null)
                return method;

            try
            {
                var kit = CompileContext.CurrentLayer.Current;
                if (val is IEnumerable enumerable && val is not string)
                {
                    var converted = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        converted.Add(EnumColumnMapping.ResolveClipValue(item, col));
                    }
                    if (converted.Count > 0)
                        kit.whereIn(fie, converted);
                }
            }
            catch
            {
                // 与基类一致：解析失败时静默跳过
            }

            return method;
        }
    }
}
