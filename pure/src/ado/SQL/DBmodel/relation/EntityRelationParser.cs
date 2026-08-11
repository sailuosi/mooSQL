using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.data
{
    /// <summary>
    /// 解析 Relation 等值 Lambda：仅支持单一 <c>Member == Member</c>（允许 Convert 解包）。
    /// </summary>
    public static class EntityRelationParser
    {
        /// <summary>
        /// 解析 <c>(a,b) =&gt; a.X == b.Y</c> 为正向 <see cref="EntityRelationInfo"/>（左→Type1，右→Type2）。
        /// </summary>
        public static EntityRelationInfo ParseEquality<T, TJoin>(Expression<Func<T, TJoin, bool>> expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var body = Unwrap(expression.Body);
            if (!(body is BinaryExpression binary) || binary.NodeType != ExpressionType.Equal)
                throw new ArgumentException(
                    "Relation 仅支持单一等值表达式，形如 (a, b) => a.Prop == b.Prop。当前：" + expression,
                    nameof(expression));

            var left = UnwrapMember(binary.Left, nameof(expression));
            var right = UnwrapMember(binary.Right, nameof(expression));

            var leftType = ResolveOwnerType(left, typeof(T), typeof(TJoin));
            var rightType = ResolveOwnerType(right, typeof(T), typeof(TJoin));

            return new EntityRelationInfo
            {
                Type1 = leftType,
                Type2 = rightType,
                Field1Name = left.Member.Name,
                Field2Name = right.Member.Name,
                Expression = expression.Body,
                Parameters = expression.Parameters
            };
        }

        /// <summary>从导航属性表达式取得成员名，如 <c>x =&gt; x.Posts</c>。</summary>
        public static string ParseNavMemberName(Expression navExpression)
        {
            if (navExpression == null) throw new ArgumentNullException(nameof(navExpression));
            Expression body = navExpression;
            if (navExpression is LambdaExpression lambda)
                body = lambda.Body;
            body = Unwrap(body);
            if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
                body = Unwrap(unary.Operand);
            if (!(body is MemberExpression member) || !(member.Member is PropertyInfo))
                throw new ArgumentException(
                    "导航属性表达式须为成员访问，形如 x => x.Posts。当前：" + navExpression,
                    nameof(navExpression));
            return member.Member.Name;
        }

        static Expression Unwrap(Expression expr)
        {
            while (expr is UnaryExpression u &&
                   (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked ||
                    u.NodeType == ExpressionType.Quote))
            {
                expr = u.Operand;
            }
            return expr;
        }

        static MemberExpression UnwrapMember(Expression expr, string paramName)
        {
            expr = Unwrap(expr);
            if (expr is MemberExpression m && m.Member is PropertyInfo)
                return m;
            throw new ArgumentException(
                "Relation 等值两侧须为属性成员访问。当前节点：" + (expr?.NodeType.ToString() ?? "null"),
                paramName);
        }

        /// <summary>
        /// 优先用泛型参数对齐 DeclaringType（继承属性时 DeclaringType 可能是基类）。
        /// </summary>
        static Type ResolveOwnerType(MemberExpression member, Type t, Type tJoin)
        {
            var declaring = member.Member.DeclaringType;
            if (declaring == null) return t;

            if (declaring == t || declaring.IsAssignableFrom(t) || t.IsAssignableFrom(declaring))
            {
                // 若左右都能对上，用参数 Expression 类型更准：看 Expression 是否 Parameter
                if (member.Expression is ParameterExpression pe)
                {
                    if (pe.Type == t || t.IsAssignableFrom(pe.Type)) return t;
                    if (pe.Type == tJoin || tJoin.IsAssignableFrom(pe.Type)) return tJoin;
                }
                if (declaring == t || t.IsAssignableFrom(declaring)) return t;
            }
            if (declaring == tJoin || declaring.IsAssignableFrom(tJoin) || tJoin.IsAssignableFrom(declaring))
            {
                if (member.Expression is ParameterExpression pe)
                {
                    if (pe.Type == tJoin || tJoin.IsAssignableFrom(pe.Type)) return tJoin;
                    if (pe.Type == t || t.IsAssignableFrom(pe.Type)) return t;
                }
                return tJoin;
            }

            if (member.Expression is ParameterExpression p2)
                return p2.Type;

            return declaring;
        }
    }
}
