using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.data.richRepo.tracking
{
    /// <summary>
    /// 手动脏标记扩展（对标 CRL Change / Cumulation）。
    /// </summary>
    public static class EntityTrackingExtensions
    {
        /// <summary>将当前属性值记入脏袋。</summary>
        public static T MarkDirty<T, TKey>(this T entity, Expression<Func<T, TKey>> expression)
            where T : class
        {
            var member = GetMember(expression);
            var bag = EntityTracking.GetOrCreateBag(entity);
            var val = GetValue(entity, member);
            bag.Set(member.Name, val);
            return entity;
        }

        /// <summary>赋值并记入脏袋。</summary>
        public static T MarkDirty<T, TKey>(this T entity, Expression<Func<T, TKey>> expression, TKey value)
            where T : class
        {
            var member = GetMember(expression);
            SetValue(entity, member, value);
            EntityTracking.GetOrCreateBag(entity).Set(member.Name, value);
            return entity;
        }

        /// <summary>数值累加：更新 CLR 属性，并标记为 SQL 侧 col = col + delta。</summary>
        public static T Cumulate<T>(this T entity, Expression<Func<T, object>> expression, object delta)
            where T : class
        {
            var member = GetMember(expression);
            var bag = EntityTracking.GetOrCreateBag(entity);
            var current = GetValue(entity, member);
            var next = AddDelta(current, delta);
            SetValue(entity, member, ConvertTo(member.PropertyType, next));
            bag.SetCumulation(member.Name, delta);
            return entity;
        }

        static PropertyInfo GetMember<T, TKey>(Expression<Func<T, TKey>> expression)
        {
            Expression body = expression.Body;
            if (body is UnaryExpression u && u.NodeType == ExpressionType.Convert)
                body = u.Operand;
            if (body is MemberExpression m && m.Member is PropertyInfo p)
                return p;
            throw new ArgumentException("表达式必须是属性访问，如 x => x.Email", nameof(expression));
        }

        static object GetValue(object entity, PropertyInfo prop) => prop.GetValue(entity);

        static void SetValue(object entity, PropertyInfo prop, object value)
        {
            if (value == null && prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null)
                return;
            prop.SetValue(entity, value == null ? null : ConvertTo(prop.PropertyType, value));
        }

        static object ConvertTo(Type target, object value)
        {
            if (value == null) return null;
            var t = Nullable.GetUnderlyingType(target) ?? target;
            if (t.IsInstanceOfType(value)) return value;
            return Convert.ChangeType(value, t);
        }

        static object AddDelta(object current, object delta)
        {
            if (delta == null) return current;
            if (current == null) return delta;
            if (current is string || delta is string)
                return Convert.ToString(current) + Convert.ToString(delta);
            return Convert.ToDecimal(current) + Convert.ToDecimal(delta);
        }
    }
}
