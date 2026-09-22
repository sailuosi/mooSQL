using MySqlConnector;
using System;
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

        public override DbCommand getCommand() => new MySqlCommand();

        public override DbConnection getConnection()
            => new MySqlConnection(db.DBConnectStr);

        public override DbDataAdapter getDataAdapter() => new MySqlDataAdapter();

        public override DbCommandBuilder getCmdBuilder() => new MySqlCommandBuilder();

        /// <summary>MySQL 族默认 Bulk：具体实现 <see cref="MySqlFamilyBulkCopyee"/>，产品可直接吃默认或 override。</summary>
        public override DbBulkCopy GetBulkCopy()
            => new MySqlFamilyBulkCopyee(this.dbInstance);

        public override bool SupportsMerge() => false;

        protected override IMemberTranslator CreateMemberTranslator()
            => new MySqlMemberTranslator();

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
            if (cmd is MySqlCommand qcmd)
                return qcmd.Parameters.AddWithValue(para.key, para.val);
            return null;
        }

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
