
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace mooSQL.data.context
{
    /// <summary>
    /// SQL 命令执行器：使用提供好的各类SQL执行环境，进行SQL的执行
    /// </summary>
    public interface ICmdExecutor
    {
        /// <summary>
        /// 执行非查询类的SQL命令
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        int ExecuteNonQuery(ExeContext executionContext);
        /// <summary>
        /// 执行数据读取
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        DataReaderWrapper ExecuteReader(ExeContext executionContext);

        DbDataReader ExecuteReaderBase(ExeContext executionContext);
        
        /// <summary>
        /// 执行单行查询
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        object ExecuteScalar(ExeContext executionContext);
        /// <summary>
        /// 执行查询并返回一个DataTable对象
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        DataTable ExecuteQuery(ExeContext executionContext);

        /// <summary>
        /// 自定义Dbcommand的执行动作，并返回结果
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="context"></param>
        /// <param name="onRunCommand"></param>
        /// <returns></returns>
        R ExecuteCmd<R>(ExeContext context, Func<DbCommand, R> onRunCommand);
        /// <summary>
        /// 执行自定义的查询器的读取动作
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="context"></param>
        /// <param name="onRunCommand"></param>
        /// <returns></returns>
        R ExecuteReader<R>(ExeContext context, Func<DbDataReader, R> onRunCommand);
        /// <summary>
        /// 自定义Dbcommand的执行动作，并返回结果
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="context"></param>
        /// <param name="onRunCommand"></param>
        /// <returns></returns>
        R ExecuteCmd<R>(ExeContext context, Func<DbCommand, ExeContext, R> onRunCommand);
        /// <summary>
        /// 执行查询并返回一个类型化的集合对象
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        IEnumerable<T> ExecuteQuery<T>(ExeContext executionContext);
        /// <summary>
        /// 自定义读取行的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executionContext"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        IEnumerable<T> ExecuteQuery<T>(ExeContext executionContext,Func<DbDataReader,T> reader);
        /// <summary>
        /// 执行查询并返回一个类型化的集合对象
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        IEnumerable<T> ExecuteQueryFirstField<T>(ExeContext executionContext);
        /// <summary>
        /// 执行查询一行并返回一个对象实例
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        T ExecuteQueryRow<T>(ExeContext executionContext);

        T ExecuteQueryUniqueRow<T>(ExeContext context);

        /// <summary>
        /// 执行查询一行一列并返回一个对象实例
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        T ExecuteQueryScalar<T>(ExeContext executionContext);

        /// <summary>
        /// 执行多表查询
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        DataSet ExecuteQueryLot(ExeContext executionContext);
        #region Async
        /// <summary>
        /// 异步执行非查询类的SQL命令
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        Task<int> ExecuteNonQueryAsync(ExeContext executionContext);
        /// <summary>
        /// 异步执行非查询类的SQL命令 并传入取消异步参数
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> ExecuteNonQueryAsync(ExeContext executionContext, CancellationToken cancellationToken);
        /// <summary>
        /// 异步执行数据读取
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        Task<DataReaderWrapper> ExecuteReaderAsync(ExeContext executionContext);
        /// <summary>
        /// 异步执行数据读取，并传入取消异步参数
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<DataReaderWrapper> ExecuteReaderAsync(ExeContext executionContext, CancellationToken cancellationToken);
        /// <summary>
        /// 异步单值查询
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(ExeContext executionContext);
        /// <summary>
        /// 异步单值查询
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(ExeContext executionContext, CancellationToken cancellationToken);
        /// <summary>
        /// 异步执行查询并返回一个DataTable对象
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        Task<DataTable> ExecuteQueryAsync(ExeContext executionContext);

        /// <summary>
        /// 执行查询并返回一个类型化的集合对象
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ExecuteQueryAsync<T>(ExeContext executionContext);
        /// <summary>
        /// 自定义读取行的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executionContext"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> ExecuteQueryAsync<T>(ExeContext executionContext, Func<DbDataReader, T> reader);
        /// <summary>
        /// 执行查询并返回一个DataSet对象
        /// </summary>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        Task<DataSet> ExecuteQueryLotAsync(ExeContext executionContext);
        #endregion
    }
}