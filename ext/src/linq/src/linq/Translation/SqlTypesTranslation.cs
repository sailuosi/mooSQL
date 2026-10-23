using System;
using System.Linq.Expressions;
using System.Reflection;
using mooSQL.linq;

namespace mooSQL.linq.translator
{
	using mooSQL.linq.utils;
	using mooSQL.data;
	using mooSQL.data.model;

	using mooSQL.linq.clause;
	/// <summary>
	/// SQL 类型成员的默认翻译器。
	/// </summary>
	public class SqlTypesTranslationDefault : IMemberTranslator
	{
		TranslationRegistration _registration = new TranslationRegistration();
		/// <summary>
		/// 初始化 <see cref="SqlTypesTranslationDefault"/> 实例。
		/// </summary>
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
		/// <summary>
		/// ConvertBit
		/// </summary>

		protected virtual Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Boolean));
		/// <summary>
		/// ConvertBigInt
		/// </summary>

		protected virtual Expression? ConvertBigInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Int64));
		/// <summary>
		/// ConvertInt
		/// </summary>

		protected virtual Expression? ConvertInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertSmallInt
		/// </summary>

		protected virtual Expression? ConvertSmallInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertTinyInt
		/// </summary>

		protected virtual Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertDefaultDecimal
		/// </summary>

		protected virtual Expression? ConvertDefaultDecimal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertDecimalPrecision
		/// </summary>

		protected virtual Expression? ConvertDecimalPrecision(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var precision))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, t => t.WithPrecision(precision).WithScale(4));
		}
		/// <summary>
		/// ConvertDecimalPrecisionScale
		/// </summary>

		protected virtual Expression? ConvertDecimalPrecisionScale(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var precision))
				return null;

			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[1], out var scale))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, t => t.WithPrecision(precision).WithScale(scale));
		}
		/// <summary>
		/// ConvertMoney
		/// </summary>

		protected virtual Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Money));
		/// <summary>
		/// ConvertSmallMoney
		/// </summary>

		protected virtual Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.SmallMoney));
		/// <summary>
		/// ConvertFloat
		/// </summary>

		protected virtual Expression? ConvertFloat(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertReal
		/// </summary>

		protected virtual Expression? ConvertReal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertDateTime
		/// </summary>

		protected virtual Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime));
		/// <summary>
		/// ConvertDateTime2
		/// </summary>

		protected virtual Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTime2));
		/// <summary>
		/// ConvertSmallDateTime
		/// </summary>

		protected virtual Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
		/// <summary>
		/// ConvertDate
		/// </summary>

		protected virtual Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Date));
		/// <summary>
		/// ConvertCharLength
		/// </summary>

		protected virtual Expression? ConvertCharLength(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(char), t => t.WithDataType(DataFam.Char).WithSystemType(typeof(string)).WithLength(length));
		}

#if NET6_0_OR_GREATER
		/// <summary>
		/// ConvertDateOnly
		/// </summary>
		/// <summary>
		/// ConvertDateOnly
		/// </summary>
		protected virtual Expression? ConvertDateOnly(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
#endif
		/// <summary>
		/// ConvertTime
		/// </summary>

		protected virtual Expression? ConvertTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.Time));
		/// <summary>
		/// ConvertDateTimeOffset
		/// </summary>

		protected virtual Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataFam.DateTimeOffset));
		/// <summary>
		/// ConvertDefaultChar
		/// </summary>

		protected virtual Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(char));
			dbDataType = dbDataType.WithSystemType(typeof(string)).WithLength(null);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}
		/// <summary>
		/// ConvertVarChar
		/// </summary>

		protected virtual Expression? ConvertVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataFam.VarChar));
		}
		/// <summary>
		/// ConvertDefaultVarChar
		/// </summary>

		protected virtual Expression? ConvertDefaultVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(string));

			dbDataType = dbDataType.WithDataType(DataFam.VarChar);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}
		/// <summary>
		/// ConvertNChar
		/// </summary>

		protected virtual Expression? ConvertNChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(char), t => t.WithSystemType(typeof(string)).WithLength(length).WithDataType(DataFam.NChar));
		}
		/// <summary>
		/// ConvertDefaultNChar
		/// </summary>

		protected virtual Expression? ConvertDefaultNChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(char));

			dbDataType = dbDataType.WithDataType(DataFam.NChar).WithSystemType(typeof(string)).WithLength(null);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}
		/// <summary>
		/// ConvertNVarChar
		/// </summary>

		protected virtual Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataFam.NVarChar));
		}
		/// <summary>
		/// ConvertDefaultNVarChar
		/// </summary>

		protected virtual Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(typeof(string));

			dbDataType = dbDataType.WithDataType(DataFam.NVarChar);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new ValueWord(dbDataType, ""), memberExpression);
		}

		#endregion
		/// <summary>
		/// MakeSqlTypeExpression
		/// </summary>

		protected Expression MakeSqlTypeExpression(ITranslationContext translationContext, Expression basedOn, Func<DbDataType, DbDataType>? correctFunc = null) 
			=> MakeSqlTypeExpression(translationContext, basedOn, basedOn.Type, correctFunc);
		/// <summary>
		/// MakeSqlTypeExpression
		/// </summary>

		protected Expression MakeSqlTypeExpression(ITranslationContext translationContext, Expression basedOn, Type type, Func<DbDataType, DbDataType>? correctFunc = null)
		{
			var dbDataType = translationContext.DBLive.dialect.mapping.GetDbDataType(type);

			if (correctFunc != null)
				dbDataType = correctFunc(dbDataType);

			var sqlDataType = new DataTypeWord(dbDataType);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sqlDataType, basedOn);
		}
		/// <summary>
		/// Translates Translate for this dialect.
		/// </summary>

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
