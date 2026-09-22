using MySqlConnector;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using mooSQL.linq;

namespace mooSQL.data
{
    /// <summary>
    /// TiDB 方言：MySQL 协议兼容；ADO 为 MySqlConnector，SQL 模板复用 MySQL。
    /// </summary>
    public class TiDBDialect : ExtDialect
    {
        public TiDBDialect()
        {
            expression = new TiDBExpress(this);
            sentence = new MySQLSentence(this);
            clauseTranslator = new MySQLClauseTranslator(this);
            mapping = new MySQLMappingPanel();
            function = new MySQLFunction();
            Option ??= new SooOption();
            Option.ProviderFlags ??= new SQLProviderFlags();
            Option.ProviderFlags.IsInsertOrUpdateSupported = true;
        }

        public override DbCommand getCommand() => new MySqlCommand();

        public override DbConnection getConnection()
            => new MySqlConnection(db.DBConnectStr);

        public override DbDataAdapter getDataAdapter() => new MySqlDataAdapter();

        public override DbCommandBuilder getCmdBuilder() => new MySqlCommandBuilder();

        public override DbBulkCopy GetBulkCopy()
            => new MySQLBulkCopyee(this.dbInstance);

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

        private static DbType GetDBTypeComm(Type theType)
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
