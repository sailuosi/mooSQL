using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.data.translation
{
    /// <summary>
    /// LINQ 成员翻译注册表（Pure 层）。Ext 以 <see cref="mooSQL.linq.Linq.Translation.TranslationRegistration"/> 特化。
    /// </summary>
    public class TranslationRegistration<TContext> where TContext : class
    {
        public delegate Expression? TranslateFunc(TContext translationContext, Expression member, TranslationFlags translationFlags);
        public delegate Expression? TranslateMethodFunc(TContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags);
        public delegate Expression? TranslateMemberAccessFunc(TContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags);

        public sealed class MemberReplacement
        {
            public MemberReplacement(LambdaExpression pattern, LambdaExpression replacement)
            {
                Pattern = pattern;
                Replacement = replacement;
            }

            public LambdaExpression Pattern;
            public LambdaExpression Replacement;
        }

        readonly Dictionary<MemberInfo, TranslateFunc> _translations = new();
        Dictionary<MemberInfo, MemberReplacement>? _replacements;

        public void RegisterMethodInternal(LambdaExpression methodCallPattern, TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch)
        {
            var methodInfo = ExpressionMemberHelper.GetMemberInfo(methodCallPattern) as MethodInfo
                ?? throw new ArgumentException("MethodCallPattern must be a method call.");

            if (!isGenericTypeMatch && methodInfo.IsGenericMethod)
                methodInfo = methodInfo.GetGenericMethodDefinitionCached();

            _translations[methodInfo] = (ctx, member, flags) => translateMethodFunc(ctx, (MethodCallExpression)member, flags);
        }

        public void RegisterMemberInternal(LambdaExpression memberAccessPattern, TranslateMemberAccessFunc translateMemberAccessFunc)
        {
            var memberInfo = ExpressionMemberHelper.GetMemberInfo(memberAccessPattern);

            if (memberInfo.MemberType is not (MemberTypes.Field or MemberTypes.Property))
                throw new ArgumentException("MemberAccessPattern must be a field or property access.");

            _translations[memberInfo] = (ctx, member, flags) => translateMemberAccessFunc(ctx, (MemberExpression)member, flags);
        }

        public void RegisterConstructorInternal(LambdaExpression memberAccessPattern, TranslateFunc translateConstructorFunc)
        {
            var memberInfo = ExpressionMemberHelper.GetMemberInfo(memberAccessPattern);

            if (memberInfo.MemberType is not MemberTypes.Constructor)
                throw new ArgumentException("MemberAccessPattern must be a constructor access.");

            _translations[memberInfo] = translateConstructorFunc;
        }

        public void RegisterMemberReplacement(LambdaExpression pattern, LambdaExpression replacement)
        {
            var memberInfo = ExpressionMemberHelper.GetMemberInfo(pattern);

            if (memberInfo.MemberType is not (MemberTypes.Field or MemberTypes.Property or MemberTypes.Method or MemberTypes.Constructor))
                throw new ArgumentException("MemberAccessPattern must be a method, field or property access.");

            if (pattern.Parameters.Count != replacement.Parameters.Count)
                throw new ArgumentException("Pattern and replacement must have the same number of parameters.");

            for (var i = 0; i < pattern.Parameters.Count; i++)
            {
                if (pattern.Parameters[i].Type != replacement.Parameters[i].Type)
                    throw new ArgumentException("Pattern and replacement must have the same parameter types.");
            }

            _replacements ??= new Dictionary<MemberInfo, MemberReplacement>();

            if (_replacements.ContainsKey(memberInfo))
                throw new InvalidOperationException($"Member replacement for {memberInfo.Name} is already registered.");

            _replacements.Add(memberInfo, new MemberReplacement(pattern, replacement));
        }

        public TranslateFunc? GetTranslation(MemberInfo member)
        {
            if (member is MethodInfo mi)
            {
                if (_translations.TryGetValue(member, out var concreteFunc))
                    return concreteFunc;

                if (mi.IsGenericMethod)
                    member = mi.GetGenericMethodDefinitionCached();
            }

            _translations.TryGetValue(member, out var func);
            return func;
        }

        public MemberReplacement? GetMemberReplacementInfo(MemberInfo member)
        {
            if (_replacements != null && _replacements.TryGetValue(member, out var replacement))
                return replacement;
            return null;
        }

        public Expression? ProvideReplacement(Expression expression)
        {
            MemberInfo memberInfo;
            if (expression is MemberExpression memberExpression)
                memberInfo = memberExpression.Member;
            else if (expression is MethodCallExpression methodCallExpression)
                memberInfo = methodCallExpression.Method;
            else if (expression is NewExpression { Constructor: { } } newExpression)
                memberInfo = newExpression.Constructor;
            else
                return null;

            var replacementInfo = GetMemberReplacementInfo(memberInfo);
            if (replacementInfo == null)
                return null;

            return LambdaReplacementHelper.Apply(replacementInfo.Pattern, replacementInfo.Replacement);
        }
    }

    static class LambdaReplacementHelper
    {
        public static Expression Apply(LambdaExpression pattern, LambdaExpression replacement)
        {
            if (pattern.Parameters.Count != replacement.Parameters.Count)
                throw new InvalidOperationException("Replacement parameter count mismatch.");

            var map = new Dictionary<ParameterExpression, Expression>(pattern.Parameters.Count);
            for (var i = 0; i < pattern.Parameters.Count; i++)
                map[pattern.Parameters[i]] = replacement.Parameters[i];

            return new ParameterReplaceVisitor(map).Visit(pattern.Body);
        }

        sealed class ParameterReplaceVisitor : ExpressionVisitor
        {
            readonly Dictionary<ParameterExpression, Expression> _map;
            public ParameterReplaceVisitor(Dictionary<ParameterExpression, Expression> map) => _map = map;
            protected override Expression VisitParameter(ParameterExpression node)
                => _map.TryGetValue(node, out var repl) ? repl : node;
        }
    }
}
