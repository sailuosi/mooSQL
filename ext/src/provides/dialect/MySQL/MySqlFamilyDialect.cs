using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// MySQL Family 方言基类：MySqlConnector ADO、直接复用 MySQL* SQL 轮子（不另抽抽象）、默认 <see cref="MySqlFamilyBulkCopyee"/>。
    /// 产品方言（MySQL / MariaDB / TiDB / OceanBase…）继承后只覆盖个性。
    /// </summary>
    public abstract class MySqlFamilyDialect : ExtDialect
    {
        /// <summary>
        /// MySqlFamilyDialect type.
        /// </summary>
        /// <summary>
        /// MySqlFamilyDialect type.
        /// </summary>
        protected MySqlFamilyDialect()
        {
            expression = new MySQLExpress(this);
            sentence = new MySQLSentence(this);
            clauseTranslator = new MySQLClauseTranslator(this);
            mapping = new MySQLMappingPanel();
            function = new MySQLFunction();
            Option ??= new SooOption();
            Option.ProviderFlags ??= new SQLProviderFlags();
            Option.ProviderFlags.IsInsertOrUpdateSupported = true;
            Option.ProviderFlags.IsJsonArrowSupported = true;
            Option.ProviderFlags.IsInsertReturningSupported = false;
            Option.ProviderFlags.IsJsonNativeBinary = true;
        }
/// <summary>
/// Creates a new database command for this dialect.
/// </summary>
 

        public override DbCommand getCommand() => new MySqlCommand();
/// <summary>
/// Creates a new database connection for this dialect.
/// </summary>
 

        public override DbConnection getConnection()
            => new MySqlConnection(db.DBConnectStr);
/// <summary>
/// Creates a data adapter for this dialect.
/// </summary>
 

        public override DbDataAdapter getDataAdapter() => new MySqlDataAdapter();
/// <summary>
/// Creates a command builder for this dialect.
/// </summary>
 

        public override DbCommandBuilder getCmdBuilder() => new MySqlCommandBuilder();

        /// <summary>MySQL 族默认 Bulk：具体实现 <see cref="MySqlFamilyBulkCopyee"/>，产品可直接吃默认或 override。</summary>
        public override DbBulkCopy GetBulkCopy()
            => new MySqlFamilyBulkCopyee(this.dbInstance);
/// <summary>
/// Returns whether MERGE statements are supported.
/// </summary>
 

        public override bool SupportsMerge() => false;

        /// <inheritdoc />
        public override bool SupportsUpdateUnion() => true;

        /// <summary>MySQL 值源用 v0.. 别名，避免与保留字/大小写折叠冲突。</summary>
        protected override List<string> BuildUpdateUnionSourceAliases(
            IReadOnlyList<string> setColumns,
            IReadOnlyList<string> keyColumns)
        {
            var n = setColumns.Count + keyColumns.Count;
            var list = new List<string>(n);
            for (var i = 0; i < n; i++)
                list.Add("v" + i);
            return list;
        }
/// <summary>
/// Creates the LINQ member translator for this dialect.
/// </summary>
 

        protected override IMemberTranslator CreateMemberTranslator()
            => new MySqlMemberTranslator();
/// <summary>
/// Adds a parameter to the command.
/// </summary>
 

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is MySqlCommand qcmd)
            {
                var bindVal = ResolveCmdParaValue(para.val) ?? DBNull.Value;
                // Json / Vector：按字符串绑定（Vector 值为 JSON 数组文本）
                if (para.dataType == DataFam.Json || para.dataType == DataFam.Vector)
                    return qcmd.Parameters.AddWithValue(para.key, bindVal);
                return qcmd.Parameters.AddWithValue(para.key, bindVal);
            }
            return null;
        }
/// <summary>
/// Adds a parameter to the command.
/// </summary>
 

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
            if (cmd is MySqlCommand qcmd)
            {
                var parameter = new MySqlParameter
                {
                    ParameterName = parameterName,
                    DbType = GetDBTypeComm(type),
                    Size = size,
                    SourceColumn = sourceColumn
                };
                return qcmd.Parameters.Add(parameter);
            }
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }
        /// <summary>
        /// Maps a CLR type to a common DbType.
        /// </summary>

        protected DbType GetDBTypeComm(Type theType)
        {
            var p1 = new MySqlParameter();
            var tc = TypeDescriptor.GetConverter(p1.DbType);
            if (tc.CanConvertFrom(theType))
            {
                p1.DbType = (DbType)tc.ConvertFrom(theType.Name)!;
            }
            else
            {
                try
                {
                    p1.DbType = (DbType)tc.ConvertFrom(theType.Name)!;
                }
                catch
                {
                    // default
                }
            }
            return p1.DbType;
        }
    }
}
