using System;

namespace mooSQL.linq
{
	public enum ConvertType
	{
		/// <summary>
		/// 将提供的名称转换为SQL参数名，如name =》 @name
		/// For example:
		///     firstName -> @firstName
		/// for the following query:
		///     SELECT * FROM Person WHERE FirstName = @firstName
		///                                            ^ here
		/// </summary>
		NameToQueryParameter,

		/// <summary>
		/// 提供的名称转换为 Command的参数名
		/// For example:
		///     firstName -> @firstName
		/// for the following query:
		///     db.Parameter("@firstName") = "John";
		///                   ^ here
		/// </summary>
		NameToCommandParameter,

		/// <summary>
		/// 提供的名称转换为存储过程参数名
		/// For example:
		///     firstName -> @firstName
		/// for the following query:
		///     db.Parameter("@firstName") = "John";
		///                   ^ here
		/// </summary>
		NameToSprocParameter,

		/// <summary>
		/// 提供的名称转为查询字段名
		/// For example:
		///     FirstName -> [FirstName]
		/// for the following query:
		///     SELECT [FirstName] FROM Person WHERE ID = 1
		///            ^   add   ^
		/// </summary>
		NameToQueryField,

		/// <summary>
		/// 提供的名称转为查询字段别名
		/// For example:
		///     ID -> "ID"
		/// for the following query:
		///     SELECT "ID" as "ID" FROM Person WHERE "ID" = 1
		///                    ^  ^ here
		/// </summary>
		NameToQueryFieldAlias,

		/// <summary>
		/// 转换为关联数据库服务的名称。
		/// For example:
		///     host name\named instance -> [host name\named instance]
		/// for the following query:
		///     SELECT * FROM [host name\named instance]..[Person]
		///                   ^ add      ^
		/// </summary>
		NameToServer,

		/// <summary>
		/// 转换为数据库 database名
		/// For example:
		///     MyDatabase -> [MyDatabase]
		/// for the following query:
		///     SELECT * FROM [MyDatabase]..[Person]
		///                   ^ add      ^
		/// </summary>
		NameToDatabase,

		/// <summary>
		/// 转换查询策略名
		/// For example:
		///     dbo -> [dbo]
		/// for the following query:
		///     SELECT * FROM [ dbo ].[Person]
		///                   ^ add ^
		/// </summary>
		NameToSchema,

		/// <summary>
		/// Provided name should be converted to package/module/library name.
		/// </summary>
		NameToPackage,

		/// <summary>
		/// Provided name should be converted to function/procedure name.
		/// </summary>
		NameToProcedure,

		/// <summary>
		/// Provided name should be converted to query table name.
		/// For example:
		///     Person -> [Person]
		/// for the following query:
		///     SELECT * FROM [Person]
		///                   ^ add  ^
		/// </summary>
		NameToQueryTable,

		/// <summary>
		/// Provided name should be converted to query table alias.
		/// For example:
		///     table1 -> [table1]
		/// for the following query:
		///     SELECT * FROM [Person] [table1]
		///                            ^ add  ^
		/// </summary>
		NameToQueryTableAlias,

		/// <summary>
		/// Provided stored procedure parameter name should be converted to name.
		/// For example:
		///     @firstName -> firstName
		/// for the following query:
		///     db.Parameter("@firstName") = "John";
		///                   ^ '@' has to be removed
		/// </summary>
		SprocParameterToName,

		/// <summary>
		/// Gets error number from a native exception.
		/// For example:
		///     SqlException -> SqlException.Number,
		///   OleDbException -> OleDbException.Errors[0].NativeError
		/// </summary>
		ExceptionToErrorNumber,

		/// <summary>
		/// Gets error message from a native exception.
		/// For example:
		///     SqlException -> SqlException.Message,
		///   OleDbException -> OleDbException.Errors[0].Message
		/// </summary>
		ExceptionToErrorMessage,

		/// <summary>
		/// Provided name should be converted to sequence name.
		/// </summary>
		SequenceName,

		/// <summary>
		/// Provided name should be converted to trigger name.
		/// </summary>
		TriggerName,
	}
}
