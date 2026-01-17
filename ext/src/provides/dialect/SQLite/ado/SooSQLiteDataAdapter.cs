#if NET5_0_OR_GREATER


using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{

    internal class SooSQLiteDataAdapter:DbDataAdapter
    {
        private bool disposeSelect = true;

        private static object _updatingEventPH = new object();

        private static object _updatedEventPH = new object();

        private bool disposed;


        //[Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [DefaultValue(null)]
        public new SqliteCommand SelectCommand
        {
            get
            {
                CheckDisposed();
                return (SqliteCommand)base.SelectCommand;
            }
            set
            {
                CheckDisposed();
                base.SelectCommand = value;
            }
        }


        [DefaultValue(null)]
        //[Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public new SqliteCommand InsertCommand
        {
            get
            {
                CheckDisposed();
                return (SqliteCommand)base.InsertCommand;
            }
            set
            {
                CheckDisposed();
                base.InsertCommand = value;
            }
        }

        [DefaultValue(null)]
        //[Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public new SqliteCommand UpdateCommand
        {
            get
            {
                CheckDisposed();
                return (SqliteCommand)base.UpdateCommand;
            }
            set
            {
                CheckDisposed();
                base.UpdateCommand = value;
            }
        }


        //[Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [DefaultValue(null)]
        public new SqliteCommand DeleteCommand
        {
            get
            {
                CheckDisposed();
                return (SqliteCommand)base.DeleteCommand;
            }
            set
            {
                CheckDisposed();
                base.DeleteCommand = value;
            }
        }

        public event EventHandler<RowUpdatingEventArgs> RowUpdating
        {
            add
            {
                CheckDisposed();
                EventHandler<RowUpdatingEventArgs> eventHandler = (EventHandler<RowUpdatingEventArgs>)base.Events[_updatingEventPH];
                if (eventHandler != null && value.Target is DbCommandBuilder)
                {
                    EventHandler<RowUpdatingEventArgs> eventHandler2 = (EventHandler<RowUpdatingEventArgs>)FindBuilder(eventHandler);
                    if (eventHandler2 != null)
                    {
                        base.Events.RemoveHandler(_updatingEventPH, eventHandler2);
                    }
                }

                base.Events.AddHandler(_updatingEventPH, value);
            }
            remove
            {
                CheckDisposed();
                base.Events.RemoveHandler(_updatingEventPH, value);
            }
        }

        public event EventHandler<RowUpdatedEventArgs> RowUpdated
        {
            add
            {
                CheckDisposed();
                base.Events.AddHandler(_updatedEventPH, value);
            }
            remove
            {
                CheckDisposed();
                base.Events.RemoveHandler(_updatedEventPH, value);
            }
        }

        public SooSQLiteDataAdapter()
        {
        }

        public SooSQLiteDataAdapter(SqliteCommand cmd)
        {
            SelectCommand = cmd;
            disposeSelect = false;
        }

        public SooSQLiteDataAdapter(string commandText, SqliteConnection connection)
        {
            SelectCommand = new SqliteCommand(commandText, connection);
        }


        public SooSQLiteDataAdapter(string commandText, string connectionString)
            : this(commandText, connectionString, parseViaFramework: false)
        {
        }


        public SooSQLiteDataAdapter(string commandText, string connectionString, bool parseViaFramework)
        {
            SqliteConnection connection = new SqliteConnection(connectionString);
            SelectCommand = new SqliteCommand(commandText, connection);
        }

        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(SooSQLiteDataAdapter).Name);
            }
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed && disposing)
                {
                    if (disposeSelect && SelectCommand != null)
                    {
                        SelectCommand.Dispose();
                        SelectCommand = null;
                    }

                    if (InsertCommand != null)
                    {
                        InsertCommand.Dispose();
                        InsertCommand = null;
                    }

                    if (UpdateCommand != null)
                    {
                        UpdateCommand.Dispose();
                        UpdateCommand = null;
                    }

                    if (DeleteCommand != null)
                    {
                        DeleteCommand.Dispose();
                        DeleteCommand = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
                disposed = true;
            }
        }

        internal static Delegate FindBuilder(MulticastDelegate mcd)
        {
            if ((object)mcd != null)
            {
                Delegate[] invocationList = mcd.GetInvocationList();
                for (int i = 0; i < invocationList.Length; i++)
                {
                    if (invocationList[i].Target is DbCommandBuilder)
                    {
                        return invocationList[i];
                    }
                }
            }

            return null;
        }

        protected override void OnRowUpdating(RowUpdatingEventArgs value)
        {
            if (base.Events[_updatingEventPH] is EventHandler<RowUpdatingEventArgs> eventHandler)
            {
                eventHandler(this, value);
            }
        }

        protected override void OnRowUpdated(RowUpdatedEventArgs value)
        {
            if (base.Events[_updatedEventPH] is EventHandler<RowUpdatedEventArgs> eventHandler)
            {
                eventHandler(this, value);
            }
        }
    }
}
#endif