# if NET5_0_OR_GREATER

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 自定义实现的
    /// </summary>
    internal class SooSQLiteCommandBuilder : DbCommandBuilder
    {
        private bool disposed;


        public new SooSQLiteDataAdapter DataAdapter
        {
            get
            {
                CheckDisposed();
                return (SooSQLiteDataAdapter)base.DataAdapter;
            }
            set
            {
                CheckDisposed();
                base.DataAdapter = value;
            }
        }


        [Browsable(false)]
        public override CatalogLocation CatalogLocation
        {
            get
            {
                CheckDisposed();
                return base.CatalogLocation;
            }
            set
            {
                CheckDisposed();
                base.CatalogLocation = value;
            }
        }


        [Browsable(false)]
        public override string CatalogSeparator
        {
            get
            {
                CheckDisposed();
                return base.CatalogSeparator;
            }
            set
            {
                CheckDisposed();
                base.CatalogSeparator = value;
            }
        }


        [Browsable(false)]
        [DefaultValue("[")]
        public override string QuotePrefix
        {
            get
            {
                CheckDisposed();
                return base.QuotePrefix;
            }
            set
            {
                CheckDisposed();
                base.QuotePrefix = value;
            }
        }


        [Browsable(false)]
        public override string QuoteSuffix
        {
            get
            {
                CheckDisposed();
                return base.QuoteSuffix;
            }
            set
            {
                CheckDisposed();
                base.QuoteSuffix = value;
            }
        }


        [Browsable(false)]
        public override string SchemaSeparator
        {
            get
            {
                CheckDisposed();
                return base.SchemaSeparator;
            }
            set
            {
                CheckDisposed();
                base.SchemaSeparator = value;
            }
        }


        public SooSQLiteCommandBuilder()
            : this(null)
        {
        }


        public SooSQLiteCommandBuilder(SooSQLiteDataAdapter adp)
        {
            QuotePrefix = "[";
            QuoteSuffix = "]";
            DataAdapter = adp;
        }

        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(SooSQLiteCommandBuilder).Name);
            }
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                _ = disposed;
            }
            finally
            {
                base.Dispose(disposing);
                disposed = true;
            }
        }


        protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
        {
            SqliteParameter sQLiteParameter = (SqliteParameter)parameter;
            sQLiteParameter.DbType = (DbType)row[SchemaTableColumn.ProviderType];
        }


        protected override string GetParameterName(string parameterName)
        {
            return string.Format("@{0}", parameterName);
        }


        protected override string GetParameterName(int parameterOrdinal)
        {
            return string.Format( "@param{0}", parameterOrdinal);
        }


        protected override string GetParameterPlaceholder(int parameterOrdinal)
        {
            return GetParameterName(parameterOrdinal);
        }


        protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
        {
            if (adapter == base.DataAdapter)
            {
                ((SooSQLiteDataAdapter)adapter).RowUpdating -= RowUpdatingEventHandler;
            }
            else
            {
                ((SooSQLiteDataAdapter)adapter).RowUpdating += RowUpdatingEventHandler;
            }
        }

        private void RowUpdatingEventHandler(object sender, RowUpdatingEventArgs e)
        {
            RowUpdatingHandler(e);
        }


        public new SqliteCommand GetDeleteCommand()
        {
            CheckDisposed();
            return (SqliteCommand)base.GetDeleteCommand();
        }


        public new SqliteCommand GetDeleteCommand(bool useColumnsForParameterNames)
        {
            CheckDisposed();
            return (SqliteCommand)base.GetDeleteCommand(useColumnsForParameterNames);
        }


        public new SqliteCommand GetUpdateCommand()
        {
            CheckDisposed();
            return (SqliteCommand)base.GetUpdateCommand();
        }


        public new SqliteCommand GetUpdateCommand(bool useColumnsForParameterNames)
        {
            CheckDisposed();
            return (SqliteCommand)base.GetUpdateCommand(useColumnsForParameterNames);
        }


        public new SqliteCommand GetInsertCommand()
        {
            CheckDisposed();
            return (SqliteCommand)base.GetInsertCommand();
        }

        public new SqliteCommand GetInsertCommand(bool useColumnsForParameterNames)
        {
            CheckDisposed();
            return (SqliteCommand)base.GetInsertCommand(useColumnsForParameterNames);
        }


        public override string QuoteIdentifier(string unquotedIdentifier)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(QuotePrefix) || string.IsNullOrEmpty(QuoteSuffix) || string.IsNullOrEmpty(unquotedIdentifier))
            {
                return unquotedIdentifier;
            }

            return QuotePrefix + unquotedIdentifier.Replace(QuoteSuffix, QuoteSuffix + QuoteSuffix) + QuoteSuffix;
        }


        public override string UnquoteIdentifier(string quotedIdentifier)
        {
            CheckDisposed();
            if (string.IsNullOrEmpty(QuotePrefix) || string.IsNullOrEmpty(QuoteSuffix) || string.IsNullOrEmpty(quotedIdentifier))
            {
                return quotedIdentifier;
            }

            if (!quotedIdentifier.StartsWith(QuotePrefix, StringComparison.OrdinalIgnoreCase) || !quotedIdentifier.EndsWith(QuoteSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return quotedIdentifier;
            }

            return quotedIdentifier.Substring(QuotePrefix.Length, quotedIdentifier.Length - (QuotePrefix.Length + QuoteSuffix.Length)).Replace(QuoteSuffix + QuoteSuffix, QuoteSuffix);
        }

        protected override DataTable GetSchemaTable(DbCommand sourceCommand)
        {
            using IDataReader dataReader = sourceCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
            DataTable schemaTable = dataReader.GetSchemaTable();
            if (HasSchemaPrimaryKey(schemaTable))
            {
                ResetIsUniqueSchemaColumn(schemaTable);
            }

            return schemaTable;
        }

        private bool HasSchemaPrimaryKey(DataTable schema)
        {
            DataColumn column = schema.Columns[SchemaTableColumn.IsKey];
            foreach (DataRow row in schema.Rows)
            {
                if ((bool)row[column])
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetIsUniqueSchemaColumn(DataTable schema)
        {
            DataColumn column = schema.Columns[SchemaTableColumn.IsUnique];
            DataColumn column2 = schema.Columns[SchemaTableColumn.IsKey];
            foreach (DataRow row in schema.Rows)
            {
                if (!(bool)row[column2])
                {
                    row[column] = false;
                }
            }

            schema.AcceptChanges();
        }
    }
}

#endif