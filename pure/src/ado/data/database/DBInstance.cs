/*
 * * 命名规范： 
 * 所有查询类操作以 ExeQuery开头。 非查询类以 ExeNonQuery开头
 * 需要用户后续手动方式执行的以  Executing 开头
 * 
 * 事务功能待进一步完善
 */

using mooSQL.data.context;
using mooSQL.data.slave;
using System;

using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;


namespace mooSQL.data
{
    /// <summary>
    /// 一个可用的数据库实例
    /// </summary>
    public partial class DBInstance
    {
        /// <summary>
        /// 数据库连接配置信息
        /// </summary>
        public DataBase config;
        /// <summary>
        /// 数据库方言
        /// </summary>
        public Dialect dialect;
        /// <summary>
        /// mooSQL的实例，持有切面等信息
        /// </summary>
        public MooClient client;
        /// <summary>
        /// 对外暴露的 <see cref="MooClient"/>，与 <see cref="client"/> 字段指向同一实例。
        /// </summary>
        public MooClient Client
        {
            get { 
                return client;
            }
        }
        /// <summary>
        /// SQL执行器
        /// </summary>
        public ICmdExecutor cmd;
        /// <summary>
        /// 创建一个空的数据库实例，请后续补充信息。
        /// </summary>
        public DBInstance() {

        }
        /// <summary>
        /// 获取数据库方言的表达式代理。
        /// </summary>
        public SQLExpression expression {
            get {

                return dialect.expression;
            }
        }

        /// <summary>
        /// 执行一个查询类的SQL
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public DataTable ExeQuery(SQLCmd cmd, DBExecutor executor=null)
        {
            if (executor == null) {
                executor = new DBExecutor(this);
            }
            return executor.ExecuteCmd(cmd, (cmd, cont) => {
                return cmd.ExecuteQuery(cont);
            });
        }
        /// <summary>
        /// 异步执行查询类 SQL，结果填充为 <see cref="DataTable"/>。
        /// </summary>
        /// <param name="SQL">命令与参数。</param>
        /// <param name="executor">可选的执行器；为 null 时使用默认 <see cref="DBExecutor"/>。</param>
        /// <returns>查询结果表。</returns>
        public Task<DataTable> ExeQueryAsync(SQLCmd SQL, DBExecutor executor = null)
        {
            if (executor == null)
            {
                executor = new DBExecutor(this);
            }
            return executor.ExecuteCmd(SQL, (cmd, cont) => {
                return cmd.ExecuteQueryAsync(cont);
            });
        }

        /// <summary>
        /// 执行数据查询
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public DataTable ExeQuery(string sql, Paras para) {
            return ExeQuery(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 执行一个Select 查询
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable ExeQuery(string sql)
        {
            return ExeQuery(sql, new Paras());
        }
        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns></returns>
        public LiveTransaction beginTransaction() {
            //准备环境
            var context = new ExeContext();
            context.DBLive=this;
            context.dialect = this.dialect;
            context.session = new ExeSession();
            context.session.linkClient(client);

            var transaction= new LiveTransaction();
            transaction.DB = this;
            transaction.context = context;
            transaction.session = context.session;

            //打开连接、创建事务
            context.session.Open(context);
            context.session.BeginTransaction(context);

            return transaction;
        } 

        /// <summary>
        /// 开放准备好的cmd，由用户自定义执行
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExecuteCmd<R>(string sql, Paras para,Func<ICmdExecutor,ExeContext,R> executor)
        {
            return ExecuteCmd<R>(new SQLCmd(sql, para), executor);
        }
        /// <summary>
        /// 开放准备好的cmd，由用户自定义执行
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R ExecuteCmd<R>(SQLCmd sql, Func<ICmdExecutor, ExeContext, R> executor, DBExecutor runner = null)
        {
            if (runner == null) {
                runner = new DBExecutor(this);
            }
            return runner.ExecuteCmd<R>(sql, executor);
        }

        /// <summary>
        /// 在临时会话中执行回调：创建 <see cref="DBExecutor"/>，打开连接后调用 <paramref name="onrun"/>，用完释放资源。
        /// </summary>
        private R PrepareSession<R>(Func<ExeContext, R> onrun)
        {
            using (var runner = new DBExecutor(this)) { 
                return runner.PrepareSession<R>(onrun);
            }
        }
        /// <summary>
        /// 获取数据库架构信息，例如表结构等。 若不指定collectionName,则返回所有集合的架构信息。 若指定了collectionName，则只返回该集合的架构信息。 若要获取特定类型的架构信息，请使用GetSchema(string collectionName)方法。
        /// </summary>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public DataTable GetSchema(string collectionName="")
        {
            return PrepareSession((context) =>
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                {
                    return context.session.connection.GetSchema();
                }
                var dt = context.session.connection.GetSchema(collectionName);
                return dt;
            });
        }
        /// <summary>
        /// 获取数据库数据源信息。 
        /// </summary>
        /// <returns></returns>
        public string GetDataSource()
        {
            return PrepareSession((context) =>
            {
                return context.session.connection.DataSource;
            });
        }
        /// <summary>
        /// 获取数据库名称。 
        /// </summary>
        /// <returns></returns>
        public string GetDatabase()
        {
            return PrepareSession((context) =>
            {
                return context.session.connection.Database;
            });
        }
        /// <summary>
        /// 获取数据库服务器版本信息。
        /// </summary>
        /// <returns></returns>
        public string GetServerVersion()
        {
            return PrepareSession((context) =>
            {
                return context.session.connection.ServerVersion;
            });
        }
#if NET5_0_OR_GREATER
        /// <summary>
        /// 异步获取数据库架构信息（等价于无集合名参数的 <c>GetSchema</c>，需目标提供方支持）。
        /// </summary>
        /// <returns>架构信息表。</returns>
        public Task<DataTable> GetSchemaAsync()
        {
            return PrepareSession((context) =>
            {
                return context.session.connection.GetSchemaAsync();
            });
        }
#endif

        /// <summary>
        /// 自定义Dbcommand的执行动作，并返回结果
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="executor"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public R Execute<R>(SQLCmd SQL, Func<DbCommand, ExeContext, R> executor, DBExecutor runner = null)
        {
            if (runner == null) {
                runner = new DBExecutor(this);
            }
            return runner.Execute<R>(SQL, executor);
        }
        /// <summary>
        /// 自定义的2参执行器
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="executor"></param>
        /// <returns></returns>
        public R Execute<R>(string sql, Paras para, Func<DbCommand, ExeContext, R> executor) {
            return Execute<R>(new SQLCmd(sql, para), executor);
        }
        /// <summary>
        /// 自定义Dbcommand的执行动作，并返回结果
        /// </summary>
        /// <returns></returns>
        public R Execute<R>(SQLCmd SQL, Func<DbCommand, R> executor, DBExecutor runner = null)
        {
            if (runner == null) {
                runner = new DBExecutor(this);
            }
            return runner.Execute<R>(SQL, executor);
        }

        /// <summary>
        /// 使用 SQL 与参数构建命令，通过仅含 <see cref="DbCommand"/> 的委托执行并返回结果（无 <see cref="ExeContext"/>）。
        /// </summary>
        public R Execute<R>(string sql, Paras para, Func<DbCommand, R> executor) {
            return Execute<R>(new SQLCmd(sql, para), executor);
        }
        /// <summary>
        /// 执行自定义读取。自动释放连接。
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        public R ExeQueryReader<R>(SQLCmd SQL, Func<DbDataReader, R> executor, DBExecutor runner = null)
        {
            if(runner == null)
            { runner = new DBExecutor(this); }
            return runner.ExeQueryReader<R>(SQL, executor);
        }

        /// <summary>
        /// 执行查询SQL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(SQLCmd SQL, DBExecutor runner = null) {
            if(runner == null)
            { runner = new DBExecutor(this); }
            return runner.ExeQuery<T>(SQL);
        }
        /// <summary>
        /// 基础泛型查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(string sql, Paras para=null)
        {
            return ExeQuery<T>(new SQLCmd(sql, para));
        }

        /// <summary>
        /// 异步查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsync<T>(string sql, Paras para=null)
        {
            return ExeQueryAsync<T>(new SQLCmd(sql,para));
        }
        /// <summary>
        /// 异步查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsync<T>(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryAsync<T>(SQL);
        }

        /// <summary>
        /// 异步查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="reader"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsyc<T>(SQLCmd SQL, Func<DbDataReader, T> reader, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this); 
            }
            return runner.ExeQueryAsync<T>(SQL,reader);
        }
        /// <summary>
        /// 使用SQL/参数的重载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExeQueryAsyc<T>(string sql, Paras para, Func<DbDataReader, T> reader)
        {
            return ExeQueryAsyc(new SQLCmd(sql,para),reader);
        }
        /// <summary>
        /// 自定义读取器，执行后不关闭连接
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public DataReaderWrapper ExecutingReader(string sql, Paras para)
        {
            return ExecutingReader(new SQLCmd(sql,para));
        }
        /// <summary>
        /// 返回待释放的执行器。使用方必须手动释放。
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public DataReaderWrapper ExecutingReader(SQLCmd cmd, DBExecutor runner = null) {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExecutingNotClose(cmd, (cmd, context) =>
            {
                return cmd.ExecuteReader(context);
            });
        }


        /// <summary>
        /// 自定义读取器
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public Task<DataReaderWrapper> ExeQueryReaderAsync(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryReaderAsync(SQL);
        }
        /// <summary>
        /// 异步执行查询并返回可包装的 <see cref="DataReaderWrapper"/>（由 SQL 与参数构建命令）。
        /// </summary>
        public Task<DataReaderWrapper> ExeQueryReaderAsync(string sql, Paras para)
        {
            return ExeQueryReaderAsync(new SQLCmd(sql,para));
        }
        /// <summary>
        /// 自定义读取器
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<DataReaderWrapper> ExeQueryReaderAsync(SQLCmd cmd,CancellationToken token, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryReaderAsync(cmd,token);
        }
        /// <summary>
        /// 允许用户自定义行读取逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="onReadRow"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(SQLCmd SQL, Func<DbDataReader, T> onReadRow, DBExecutor runner = null) {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQuery<T>(SQL, onReadRow);
        }
        /// <summary>
        /// 允许用户自定义行读取逻辑
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <param name="onReadRow"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQuery<T>(string sql, Paras para, Func<DbDataReader, T> onReadRow)
        {
            return ExeQuery(new SQLCmd(sql, para), onReadRow);
        }
        /// <summary>
        /// 查询首字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQueryFirstField<T>(SQLCmd SQL, DBExecutor runner = null) {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryFirstField<T>(SQL);
        }
        /// <summary>
        /// 查询首字段
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public IEnumerable<T> ExeQueryFirstField<T>(string sql, Paras para)
        {
            return ExeQueryFirstField<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 查询单行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public T ExeQueryRow<T>(SQLCmd SQL, DBExecutor runner = null) {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryRow<T>(SQL);
        }
        /// <summary>
        /// 查询单行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public T ExeQueryRow<T>(string sql, Paras para=null)
        {
            return ExeQueryRow<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 查询唯一的一行，不唯一不行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SQL"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public T ExeQueryUniqueRow<T>(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryUniqueRow<T>(SQL);
        }
        /// <summary>
        /// 异步查询并期望唯一一行；若零行或多行，行为由底层 <see cref="DBExecutor"/> 实现决定（通常抛错或返回默认值）。
        /// </summary>
        /// <typeparam name="T">行映射类型。</typeparam>
        /// <param name="SQL">命令与参数。</param>
        /// <param name="runner">可选执行器。</param>
        /// <returns>映射后的单行结果。</returns>
        public Task<T> ExeQueryUniqueRowAsync<T>(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryUniqueRowAsync<T>(SQL);
        }
        /// <summary>
        /// 查询唯一的一行，不唯一null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public T ExeQueryUniqueRow<T>(string sql, Paras para=null) {
            return ExeQueryUniqueRow<T>(new SQLCmd(sql, para));
        }

        /// <summary>
        /// 执行返回多结果集的查询；在 <paramref name="read"/> 中通过 <see cref="IMultiReader"/> 顺序消费每个结果集。
        /// </summary>
        public TResult ExeQueryMultiple<TResult>(SQLCmd SQL, Func<IMultiReader, TResult> read, DBExecutor runner = null)
        {
            if (runner == null)
                runner = new DBExecutor(this);
            return runner.ExeQueryMultiple(SQL, read);
        }


        /// <summary>
        /// 异步执行多结果集查询；在 <paramref name="read"/> 中通过 <see cref="IMultiReader"/> 顺序异步消费每个结果集。
        /// </summary>
        /// <inheritdoc cref="ExeQueryMultiple{TResult}(SQLCmd, Func{IMultiReader, TResult}, DBExecutor)" path="/param"/>
        public Task<TResult> ExeQueryMultipleAsync<TResult>(SQLCmd SQL, Func<IMultiReader, Task<TResult>> read, CancellationToken cancellationToken = default, DBExecutor runner = null)
        {
            if (runner == null)
                runner = new DBExecutor(this);
            return runner.ExeQueryMultipleAsync(SQL, read, cancellationToken);
        }

        /// <summary>
        /// 异步打开多结果集查询；在 <paramref name="read"/> 中同步委托内顺序消费 <see cref="IMultiReader"/>（在异步管线中执行）。
        /// </summary>
        /// <inheritdoc cref="ExeQueryMultiple{TResult}(SQLCmd, Func{IMultiReader, TResult}, DBExecutor)" path="/param"/>
        public Task<TResult> ExeQueryMultipleAsync<TResult>(SQLCmd SQL, Func<IMultiReader, TResult> read, CancellationToken cancellationToken = default, DBExecutor runner = null)
        {
            if (runner == null)
                runner = new DBExecutor(this);
            return runner.ExeQueryMultipleAsync(SQL, read, cancellationToken);
        }


        /// <summary>
        /// 执行查询，取首行首列并转换为 <typeparamref name="T"/>（标量）。
        /// </summary>
        public T ExeQueryScalar<T>(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryScalar<T>(SQL);
        }
        /// <summary>
        /// 异步执行标量查询，取首行首列并转换为 <typeparamref name="T"/>。
        /// </summary>
        public Task<T> ExeQueryScalarAsync<T>(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryScalarAsync<T>(SQL);
        }
        /// <summary>
        /// 使用 SQL 与参数执行标量查询，取首行首列并转换为 <typeparamref name="T"/>。
        /// </summary>
        public T ExeQueryScalar<T>(string sql, Paras para = null) {
            return ExeQueryScalar<T>(new SQLCmd(sql, para));
        }
        /// <summary>
        /// 执行查询，取首行首列为 <see cref="object"/>（标量）。
        /// </summary>
        public object ExeQueryScalar(SQLCmd SQL, DBExecutor runner = null) {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryScalar(SQL);
        }

        /// <summary>
        /// object类型的执行器
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public object ExeQueryScalar(string sql, Paras para=null)
        {
            return ExeQueryScalar(new SQLCmd(sql, para));
        }

        /// <summary>
        /// 异步执行标量查询，支持取消；取首行首列（可空引用类型）。
        /// </summary>
        /// <param name="cmd">命令与参数。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <param name="runner">可选执行器。</param>
        public Task<object?> ExeQueryScalarAsync(SQLCmd cmd,CancellationToken cancellationToken, DBExecutor runner = null) {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeQueryScalarAsync(cmd,cancellationToken);
        }


        /// <summary>
        /// 异步执行
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public Task<int> ExeNonQueryAsync(SQLCmd cmd, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeNonQueryAsync(cmd);
        }
        /// <summary>
        /// 异步执行带token
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<int> ExeNonQueryAsync(SQLCmd cmd,CancellationToken token, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeNonQueryAsync(cmd,token);
        }
        /// <summary>
        /// 执行数据修改
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="para"></param>
        /// <returns></returns>
        public int ExeNonQuery(string sql, Paras para)
        {
            return ExeNonQuery(new SQLCmd(sql,para));
        }
        /// <summary>
        /// 执行非查询的操作
        /// </summary>
        /// <param name="SQL"></param>
        /// <returns></returns>
        public int ExeNonQuery(SQLCmd SQL, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeNonQuery(SQL);
        }
        /// <summary>
        /// 批量执行
        /// </summary>
        /// <param name="SQLs"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public int ExeNonQuery(IEnumerable<SQLCmd> SQLs, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeNonQuery(SQLs);
        }
        /// <summary>
        /// 异步执行一批命令
        /// </summary>
        /// <param name="SQLs"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public Task<int> ExeNonQueryAsync(IEnumerable<SQLCmd> SQLs, DBExecutor runner = null)
        {
            if (runner == null)
            {
                runner = new DBExecutor(this);
            }
            return runner.ExeNonQueryAsync(SQLs);
        }
    }
}
