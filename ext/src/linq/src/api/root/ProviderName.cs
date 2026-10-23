using System;



namespace mooSQL.linq
{

	// TODO: v6: obsolete/remove all provider-specific entries as it should be used for dialects only
	/// <summary>
	/// 提供程序默认配置名。
	/// </summary>
	
	public static class ProviderName
	{
		/// <summary>
		/// Microsoft Access OleDb 提供程序（JET 或 ACE）。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Access        = "Access";

		/// <summary>
		/// Microsoft Access ODBC 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string AccessOdbc    = "Access.Odbc";

		/// <summary>
		/// IBM DB2 默认提供程序（DB2 LUW）。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string DB2           = "DB2";
		/// <summary>
		/// IBM DB2 LUW 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string DB2LUW        = "DB2.LUW";
		/// <summary>
		/// IBM DB2 for z/OS 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string DB2zOS        = "DB2.z/OS";
		/// <summary>
		/// Firebird 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Firebird      = "Firebird";
		/// <summary>
		/// Firebird 2.5 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Firebird25      = "Firebird.2.5";
		/// <summary>
		/// Firebird 3 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Firebird3      = "Firebird.3";
		/// <summary>
		/// Firebird 4 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Firebird4      = "Firebird.4";
		/// <summary>
		/// Firebird 5 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Firebird5      = "Firebird.5";
		/// <summary>
		/// Informix IBM.Data.Informix 提供程序（含 IDS）。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Informix      = "Informix";
		/// <summary>
		/// 基于 IBM.Data.DB2 IDS 的 Informix 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string InformixDB2   = "Informix.DB2";
		/// <summary>
		/// Microsoft SQL Server 默认提供程序（SQL Server 2008）。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer     = "SqlServer";
		/// <summary>
		/// Microsoft SQL Server 2005 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2005 = "SqlServer.2005";
		/// <summary>
		/// Microsoft SQL Server 2008 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2008 = "SqlServer.2008";
		/// <summary>
		/// Microsoft SQL Server 2012 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2012 = "SqlServer.2012";
		/// <summary>
		/// Microsoft SQL Server 2012 提供程序。
		/// </summary>
		public const string SqlServer2014 = "SqlServer.2014";
		/// <summary>
		/// Microsoft SQL Server 2016 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2016 = "SqlServer.2016";
		/// <summary>
		/// Microsoft SQL Server 2017 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2017 = "SqlServer.2017";
		/// <summary>
		/// Microsoft SQL Server 2019 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2019 = "SqlServer.2019";
		/// <summary>
		/// Microsoft SQL Server 2022 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlServer2022 = "SqlServer.2022";
		/// <summary>
		/// MySql 提供程序。
		/// </summary>
		public const string MySql         = "MySql";
		/// <summary>
		/// MySql 5.7.x 提供程序。
		/// </summary>
		public const string MySql57 = "MySql.5.7";
		/// <summary>
		/// MySql 8.x 提供程序。
		/// </summary>
		public const string MySql80 = "MySql.8.0";
		/// <summary>
		/// MariaDB 10+ 提供程序。
		/// </summary>
		public const string MariaDB10 = "MariaDB.10";
		/// <summary>
		/// 使用 MySql.Data 的 MySql 5.7.x 提供程序。
		/// </summary>
		public const string MySql57MySqlData = "MySql.5.7.MySql.Data";
		/// <summary>
		/// 使用 MySqlConnector 的 MySql 5.7.x 提供程序。
		/// </summary>
		public const string MySql57MySqlConnector = "MySql.5.7.MySqlConnector";
		/// <summary>
		/// 使用 MySql.Data 的 MySql 8.x 提供程序。
		/// </summary>
		public const string MySql80MySqlData = "MySql.8.0.MySql.Data";
		/// <summary>
		/// 使用 MySqlConnector 的 MySql 8.x 提供程序。
		/// </summary>
		public const string MySql80MySqlConnector = "MySql.8.0.MySqlConnector";
		/// <summary>
		/// 使用 MySql.Data 的 MariaDB 10+ 提供程序。
		/// </summary>
		public const string MariaDB10MySqlData = "MariaDB.10.MySql.Data";
		/// <summary>
		/// 使用 MySqlConnector 的 MariaDB 10+ 提供程序。
		/// </summary>
		public const string MariaDB10MySqlConnector = "MariaDB.10.MySqlConnector";

		/// <summary>
		/// Oracle ODP.NET 自动检测提供程序（原生或托管）。
		/// </summary>
		public const string Oracle        = "Oracle";
		/// <summary>
		/// Oracle（11g 方言）ODP.NET 原生提供程序。
		/// </summary>
		public const string Oracle11Native = "Oracle.11.Native";
		/// <summary>
		/// Oracle（11g 方言）Devart 提供程序。
		/// </summary>
		public const string Oracle11Devart = "Oracle.11.Devart";
		/// <summary>
		/// Oracle（11g 方言）ODP.NET 托管提供程序。
		/// </summary>
		public const string Oracle11Managed = "Oracle.11.Managed";
		/// <summary>
		/// Oracle ODP.NET 原生提供程序。
		/// </summary>
		public const string OracleNative  = "Oracle.Native";
		/// <summary>
		/// Oracle ODP.NET 托管提供程序。
		/// </summary>
		public const string OracleManaged = "Oracle.Managed";
		/// <summary>
		/// Oracle Devart 提供程序。
		/// </summary>
		public const string OracleDevart = "Oracle.Devart";
		/// <summary>
		/// PostgreSQL 9.2- 数据提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string PostgreSQL    = "PostgreSQL";
		/// <summary>
		/// PostgreSQL 9.2- 数据提供程序。
		/// </summary>
		public const string PostgreSQL92  = "PostgreSQL.9.2";
		/// <summary>
		/// PostgreSQL 9.3+ 数据提供程序。
		/// </summary>
		public const string PostgreSQL93  = "PostgreSQL.9.3";
		/// <summary>
		/// PostgreSQL 9.5+ 数据提供程序。
		/// </summary>
		public const string PostgreSQL95  = "PostgreSQL.9.5";
		/// <summary>
		/// PostgreSQL 15+ 数据提供程序。
		/// </summary>
		public const string PostgreSQL15 = "PostgreSQL.15";
		/// <summary>
		/// Microsoft SQL Server Compact Edition 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SqlCe         = "SqlCe";
		/// <summary>
		/// SQLite 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SQLite        = "SQLite";
		/// <summary>
		/// System.Data.Sqlite 提供程序。
		/// </summary>
		public const string SQLiteClassic = "SQLite.Classic";
		/// <summary>
		/// Microsoft.Data.Sqlite 提供程序。
		/// </summary>
		public const string SQLiteMS      = "SQLite.MS";
		/// <summary>
		/// 原生 SAP/Sybase ASE 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string Sybase        = "Sybase";
		/// <summary>
		/// 来自 DataAction 的托管 Sybase/SAP ASE 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SybaseManaged = "Sybase.Managed";
		/// <summary>
		/// SAP HANA 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SapHana       = "SapHana";
		/// <summary>
		/// SAP HANA 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SapHanaNative = "SapHana.Native";
		/// <summary>
		/// SAP HANA ODBC 提供程序。
		/// 用作对应映射架构的配置名。
		/// </summary>
		public const string SapHanaOdbc   = "SapHana.Odbc";
		/// <summary>
		/// ClickHouse 提供程序基础名。
		/// </summary>
		public const string ClickHouse        = "ClickHouse";
		/// <summary>
		/// 使用 Octonica.ClickHouseClient 的 ClickHouse 提供程序。
		/// </summary>
		public const string ClickHouseOctonica = "ClickHouse.Octonica";
		/// <summary>
		/// 使用 ClickHouse.Client 的 ClickHouse 提供程序。
		/// </summary>
		public const string ClickHouseClient   = "ClickHouse.Client";
		/// <summary>
		/// 使用 MySqlConnector 的 ClickHouse 提供程序。
		/// </summary>
		public const string ClickHouseMySql   = "ClickHouse.MySql";
	}
}
