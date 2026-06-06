using System;
using System.Linq.Expressions;

namespace mooSQL.data.translation
{
    public static class TranslationRegistrationExtensions
    {
        public static void RegisterMethod<TContext>(this TranslationRegistration<TContext> registration, Expression<Action> methodCallPattern, TranslationRegistration<TContext>.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
            where TContext : class
            => registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

        public static void RegisterMethod<TContext, T, TResult>(this TranslationRegistration<TContext> registration, Expression<Func<T, TResult>> methodCallPattern, TranslationRegistration<TContext>.TranslateMethodFunc translateMethodFunc, bool isGenericTypeMatch = false)
            where TContext : class
            => registration.RegisterMethodInternal(methodCallPattern, translateMethodFunc, isGenericTypeMatch);

        public static void RegisterMember<TContext, TResult>(this TranslationRegistration<TContext> registration, Expression<Func<TResult>> memberAccessPattern, TranslationRegistration<TContext>.TranslateMemberAccessFunc translateMemberAccessFunc)
            where TContext : class
            => registration.RegisterMemberInternal(memberAccessPattern, translateMemberAccessFunc);

        public static void RegisterMember<TContext, T, TResult>(this TranslationRegistration<TContext> registration, Expression<Func<T, TResult>> memberAccessPattern, TranslationRegistration<TContext>.TranslateMemberAccessFunc translateMemberAccessFunc)
            where TContext : class
            => registration.RegisterMemberInternal(memberAccessPattern, translateMemberAccessFunc);
    }
}
