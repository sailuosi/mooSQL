using System;
using System.Linq.Expressions;
using System.Reflection;

namespace mooSQL.linq.Linq.Translation
{
	using Common;

	using mooSQL.data;
	using mooSQL.data.model;

	using SqlQuery;

	public class SqlTypesTranslationDefault : IMemberTranslator
	{
		TranslationRegistration _registration = new TranslationRegistration();

		public SqlTypesTranslationDefault()
		{
			_registration.RegisterMember(() => DbFunc.Types.Bit,      ConvertBit);
			_registration.RegisterMember(() => DbFunc.Types.BigInt,   ConvertBigInt);
			_registration.RegisterMember(() => DbFunc.Types.Int,      ConvertInt);
			_registration.RegisterMember(() => DbFunc.Types.SmallInt, ConvertSmallInt);
			_registration.RegisterMember(() => DbFunc.Types.TinyInt,  ConvertTinyInt);
			_registration.RegisterMember(() => DbFunc.Types.DefaultDecimal, ConvertDefaultDecimal);

			_registration.RegisterMethod(() => DbFunc.Types.Decimal(0),  ConvertDecimalPrecision);
			_registration.RegisterMethod(() => DbFunc.Types.Decimal(0, 0), ConvertDecimalPrecisionScale);

			_registration.RegisterMember(() => DbFunc.Types.Money, ConvertMoney);
			_registration.RegisterMember(() => DbFunc.Types.SmallMoney, ConvertSmallMoney);
			
			//TODO: What is it?
			_registration.RegisterMember(() => DbFunc.Types.Float, ConvertFloat);

			_registration.RegisterMember(() => DbFunc.Types.Real, ConvertReal);
			_registration.RegisterMember(() => DbFunc.Types.DateTime, ConvertDateTime);
			_registration.RegisterMember(() => DbFunc.Types.DateTime2, ConvertDateTime2);
			_registration.RegisterMember(() => DbFunc.Types.SmallDateTime, ConvertSmallDateTime);
			_registration.RegisterMember(() => DbFunc.Types.Date, ConvertDate);

#if NET6_0_OR_GREATER
			_registration.RegisterMember(() => DbFunc.Types.DateOnly, ConvertDateOnly);
#endif
			_registration.RegisterMember(() => DbFunc.Types.Time, ConvertTime);
			_registration.RegisterMember(() => DbFunc.Types.DateTimeOffset, ConvertDateTimeOffset);
			_registration.RegisterMethod(() => DbFunc.Types.Char(0), ConvertCharLength);
			_registration.RegisterMember(() => DbFunc.Types.DefaultChar, ConvertDefaultChar);
			_registration.RegisterMethod(() => DbFunc.Types.VarChar(0), ConvertVarChar);

			_registration.RegisterMember(() => DbFunc.Types.DefaultVarChar, ConvertDefaultVarChar);
			_registration.RegisterMethod(() => DbFunc.Types.NChar(0), ConvertNChar);
			_registration.RegisterMember(() => DbFunc.Types.DefaultNChar, ConvertDefaultNChar);
			_registration.RegisterMethod(() => DbFunc.Types.NVarChar(0), ConvertNVarChar);
			_registration.RegisterMember(() => DbFunc.Types.DefaultNVarChar, ConvertDefaultNVarChar);
		}

		#region Convert functions

		protected virtual Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Boolean));

		protected virtual Expression? ConvertBigInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Int64));

		protected virtual Expression? ConvertInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertSmallInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDefaultDecimal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDecimalPrecision(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var precision))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, t => t.WithPrecision(precision).WithScale(4));
		}

		protected virtual Expression? ConvertDecimalPrecisionScale(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var precision))
				return null;

			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[1], out var scale))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, t => t.WithPrecision(precision).WithScale(scale));
		}

		protected virtual Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Money));

		protected virtual Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.SmallMoney));

		protected virtual Expression? ConvertFloat(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertReal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime));

		protected virtual Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime2));

		protected virtual Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Date));

		protected virtual Expression? ConvertCharLength(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(char), t => t.WithDataType(DataFam.Char).WithSystemType(typeof(string)).WithLength(length));
		}

#if NET6_0_OR_GREATER
		protected virtual Expression? ConvertDateOnly(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
#endif

		protected virtual Expression? ConvertTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Time));

		protected virtual Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTimeOffset));

		protected virtual Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(char));
			dbDataType = dbDataType.WithSystemType(typeof(string)).WithLength(null);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}

		protected virtual Expression? ConvertVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataFam.VarChar));
		}

		protected virtual Expression? ConvertDefaultVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(string));

			dbDataType = dbDataType.WithDataType(DataFam.VarChar);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}

		protected virtual Expression? ConvertNChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(char), t => t.WithSystemType(typeof(string)).WithLength(length).WithDataType(DataFam.NChar));
		}

		protected virtual Expression? ConvertDefaultNChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(char));

			dbDataType = dbDataType.WithDataType(DataFam.NChar).WithSystemType(typeof(string)).WithLength(null);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}

		protected virtual Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataFam.NVarChar));
		}

		protected virtual Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(string));

			dbDataType = dbDataType.WithDataType(DataFam.NVarChar);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}

		#endregion

		protected Expression MakeSqlTypeExpression(ITranslationContext translationContext, Expression basedOn, Func<DbDataType, DbDataType>? correctFunc = null) 
			=> MakeSqlTypeExpression(translationContext, basedOn, basedOn.Type, correctFunc);

		protected Expression MakeSqlTypeExpression(ITranslationContext translationContext, Expression basedOn, Type type, Func<DbDataType, DbDataType>? correctFunc = null)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(type);

			if (correctFunc != null)
				dbDataType = correctFunc(dbDataType);

			var sqlDataType = new DataTypeWord(dbDataType);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sqlDataType, basedOn);
		}

		public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			MemberInfo memberInfo;

			if (memberExpression is MethodCallExpression methodCall)
				memberInfo = methodCall.Method;
			else if (memberExpression is MemberExpression member)
				memberInfo = member.Member;
			else
				return null;

			var translationFunc = _registration.GetTranslation(memberInfo);
			if (translationFunc == null)
				return null;

			var result = translationFunc(translationContext, memberExpression, translationFlags);
			return result;
		}
	}
}
