#if NET8_0_OR_GREATER
using HuaweiCloud.GaussDB;
using HuaweiCloud.GaussDBTypes;
#else
using Npgsql;
using NpgsqlTypes;
#endif
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace mooSQL.data
{
    /// <summary>
    /// openGauss / GaussDB：PostgreSQL Family；net8+ HuaweiCloud.GaussDB，低 TFM Npgsql；Bulk 吃族默认 Fallback。
    /// </summary>
    public class OpenGaussDialect : PgFamilyDialect
    {
        public OpenGaussDialect()
        {
            expression = new OpenGaussExpress(this);
            sentence = new OpenGaussSentence(this);
            clauseTranslator = new OpenGaussClauseTranslator(this);
            mapping = new OpenGaussMappingPanel();
            function = new OpenGaussSQLFunction();
        }

        public override DbCommandBuilder getCmdBuilder()
#if NET8_0_OR_GREATER
            => new GaussDBCommandBuilder();
#else
            => new NpgsqlCommandBuilder();
#endif

        public override DbCommand getCommand()
#if NET8_0_OR_GREATER
            => new GaussDBCommand();
#else
            => new NpgsqlCommand();
#endif

        public override DbConnection getConnection()
#if NET8_0_OR_GREATER
            => new GaussDBConnection(db.DBConnectStr);
#else
            => new NpgsqlConnection(db.DBConnectStr);
#endif

        public override DbDataAdapter getDataAdapter()
#if NET8_0_OR_GREATER
            => new GaussDBDataAdapter();
#else
            => new NpgsqlDataAdapter();
#endif

        public override DbParameter AddCmdPara(DbCommand cmd, Parameter para)
        {
#if NET8_0_OR_GREATER
            if (cmd is GaussDBCommand qcmd)
            {
                var p = new GaussDBParameter(para.key, para.val);
                return qcmd.Parameters.Add(p);
            }
#else
            if (cmd is NpgsqlCommand qcmd)
            {
                var p = new NpgsqlParameter
                {
                    ParameterName = para.key,
                    Value = para.val
                };
                return qcmd.Parameters.Add(p);
            }
#endif
            return null;
        }

        public override DbParameter AddCmdPara(DbCommand cmd, string parameterName, Type type, int size, string sourceColumn)
        {
#if NET8_0_OR_GREATER
            if (cmd is GaussDBCommand qcmd)
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
#else
            if (cmd is NpgsqlCommand qcmd)
                qcmd.Parameters.Add(parameterName, GetDBType(type), size, sourceColumn);
#endif
            int i = cmd.Parameters.Add(parameterName);
            return cmd.Parameters[i];
        }

#if NET8_0_OR_GREATER
        private GaussDBDbType GetDBType(Type theType)
        {
            var p1 = new GaussDBParameter();
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
            return p1.GaussDBDbType;
        }
#else
        private NpgsqlDbType GetDBType(Type theType)
        {
            var p1 = new NpgsqlParameter();
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
            return p1.NpgsqlDbType;
        }
#endif
    }
}
