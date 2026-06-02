using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 多行为、多选项的复制类
    /// </summary>
    public class BulkCopyBase
    {

        /// <summary>
        /// 属性 DB（DBInstance）。
        /// </summary>
        public DBInstance DB {  get; set; }

        /// <summary>
        /// 字段 ContinueOnCapturedContext（bool）。
        /// </summary>
        public bool ContinueOnCapturedContext = false;
        /// <summary>
        /// 最大参数量
        /// </summary>
        protected virtual int MaxParameters => 999;
        /// <summary>
        /// 最大SQL长度
        /// </summary>
        protected virtual int MaxSqlLength => 100000;

        /// <summary>
        /// UNION ALL 首行字面量是否强制类型转换。
        /// </summary>
        protected virtual bool CastFirstRowLiteralOnUnionAll => false;
        /// <summary>
        /// UNION ALL 首行参数是否强制类型转换。
        /// </summary>
        protected virtual bool CastFirstRowParametersOnUnionAll => false;
        /// <summary>
        /// UNION ALL 所有行参数是否强制类型转换。
        /// </summary>
        protected virtual bool CastAllRowsParametersOnUnionAll => false;

        /// <summary>
        /// 属性 CopyType（BulkCopyType）。
        /// </summary>
        public BulkCopyType CopyType { get; set; }

        /// <summary>
        /// 属性 Options（BulkCopyOptions）。
        /// </summary>
        public BulkCopyOptions Options { get; set; }
        //用户侧 曝露API 仅 BulkCopy 一个方法



        /// <summary>
        /// 泛型方法 BulkCopy（返回 BulkCopyRowsCopied）。
        /// </summary>
        public virtual BulkCopyRowsCopied BulkCopy<T>(IEnumerable<T> source)
            where T : notnull
        {
            return CopyType switch
            {
                BulkCopyType.MultipleRows => MultipleRowsCopy( source),
                BulkCopyType.RowByRow => RowByRowCopy(source),
                _ => ProviderSpecificCopy(source),
            };
        }

        /// <summary>
        /// 泛型方法 BulkCopyAsync（返回 Task<BulkCopyRowsCopied>）。
        /// </summary>
        public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(IEnumerable<T> source, CancellationToken cancellationToken)
            where T : notnull
        {
            return CopyType switch
            {
                BulkCopyType.MultipleRows => MultipleRowsCopyAsync( source, cancellationToken),
                BulkCopyType.RowByRow => RowByRowCopyAsync(source, cancellationToken),
                _ => ProviderSpecificCopyAsync(source, cancellationToken),
            };
        }
#if NET5_0_OR_GREATER
        /// <summary>
        /// 泛型方法 BulkCopyAsync（返回 Task<BulkCopyRowsCopied>）。
        /// </summary>
        public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return CopyType switch
			{
				BulkCopyType.MultipleRows => MultipleRowsCopyAsync    ( source, cancellationToken),
				BulkCopyType.RowByRow     => RowByRowCopyAsync        ( source, cancellationToken),
				_                         => ProviderSpecificCopyAsync( source, cancellationToken),
			};
		}

		/// <summary>
		/// 泛型方法 ProviderSpecificCopyAsync（返回 Task<BulkCopyRowsCopied>）。
		/// </summary>
		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return MultipleRowsCopyAsync( source, cancellationToken);
		}

        /// <summary>
        /// 泛型方法 MultipleRowsCopyAsync（返回 Task<BulkCopyRowsCopied>）。
        /// </summary>
        protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    where T : notnull
        {
            return RowByRowCopyAsync(source, cancellationToken);
        }

        /// <summary>
        /// 泛型方法 Task（返回 async）。
        /// </summary>
        protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    where T : notnull
        {


            // This limitation could be lifted later for some providers that supports identity insert if we will get such request
            // It will require support from DataConnection.Insert
            if (Options.KeepIdentity == true)
                throw new Exception($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

            var rowsCopied = new BulkCopyRowsCopied();
            var kit = this.DB.useSQL();
            await foreach (var item in source.ConfigureAwait(ContinueOnCapturedContext).WithCancellation(cancellationToken))
            {
                var cc= kit.insert(item);
                rowsCopied.RowsCopied+=cc;

                if (Options.NotifyAfter != 0 && Options.RowsCopiedCallback != null && rowsCopied.RowsCopied % Options.NotifyAfter == 0)
                {
                    Options.RowsCopiedCallback(rowsCopied);

                    if (rowsCopied.Abort)
                        break;
                }
            }

            return rowsCopied;
        }

#endif


        /// <summary>
        /// 泛型方法 ProviderSpecificCopy（返回 BulkCopyRowsCopied）。
        /// </summary>
        protected virtual BulkCopyRowsCopied ProviderSpecificCopy<T>(IEnumerable<T> source)
            where T : notnull
        {
            return MultipleRowsCopy(source);
        }

        /// <summary>
        /// 泛型方法 ProviderSpecificCopyAsync（返回 Task<BulkCopyRowsCopied>）。
        /// </summary>
        protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(IEnumerable<T> source, CancellationToken cancellationToken)
            where T : notnull
        {
            return MultipleRowsCopyAsync( source, cancellationToken);
        }



        /// <summary>
        /// 泛型方法 MultipleRowsCopy（返回 BulkCopyRowsCopied）。
        /// </summary>
        protected virtual BulkCopyRowsCopied MultipleRowsCopy<T>(IEnumerable<T> source)
            where T : notnull
        {
            return RowByRowCopy(source);
        }

        /// <summary>
        /// 泛型方法 MultipleRowsCopyAsync（返回 Task<BulkCopyRowsCopied>）。
        /// </summary>
        protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(IEnumerable<T> source, CancellationToken cancellationToken)
            where T : notnull
        {
            return RowByRowCopyAsync(source, cancellationToken);
        }



        /// <summary>
        /// 泛型方法 RowByRowCopy（返回 BulkCopyRowsCopied）。
        /// </summary>
        protected virtual BulkCopyRowsCopied RowByRowCopy<T>(IEnumerable<T> source)
            where T : notnull
        {

            // 对于一些支持自增id插入的提供者，如果我们将得到这样的请求，这个限制可能会被取消，它将需要DataConnection的支持。插入
            if (Options.KeepIdentity == true)
                throw new Exception($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

            var rowsCopied = new BulkCopyRowsCopied();
            var kit = DB.useSQL();
            foreach (var item in source)
            {
                kit.insert(item);

                rowsCopied.RowsCopied++;

                if (Options.NotifyAfter != 0 && Options.RowsCopiedCallback != null && rowsCopied.RowsCopied % Options.NotifyAfter == 0)
                {
                    Options.RowsCopiedCallback(rowsCopied);

                    if (rowsCopied.Abort)
                        break;
                }
            }

            return rowsCopied;
        }

        /// <summary>
        /// 泛型方法 Task（返回 async）。
        /// </summary>
        protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(IEnumerable<T> source, CancellationToken cancellationToken)
            where T : notnull
        {


            // This limitation could be lifted later for some providers that supports identity insert if we will get such request
            // It will require support from DataConnection.Insert
            if (Options.KeepIdentity == true)
                throw new Exception($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

            var rowsCopied = new BulkCopyRowsCopied();
            var kit= DB.useSQL();
            foreach (var item in source)
            {
                kit.insert(item);

                rowsCopied.RowsCopied++;

                if (Options.NotifyAfter != 0 && Options.RowsCopiedCallback != null && rowsCopied.RowsCopied % Options.NotifyAfter == 0)
                {
                    Options.RowsCopiedCallback(rowsCopied);

                    if (rowsCopied.Abort)
                        break;
                }
            }

            return rowsCopied;
        }

    }
}