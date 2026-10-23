using System;

using mooSQL.linq;

namespace mooSQL.linq.utils
{
	/// <summary>
	/// Activity Service event ID.
	/// </summary>
	public enum ActivityID
	{
		/// <summary>
		/// QueryProviderExecuteT
		/// </summary>
		/// <summary>
		/// QueryProviderExecuteT
		/// </summary>
		QueryProviderExecuteT,
		/// <summary>
		/// QueryProviderExecute
		/// </summary>
		/// <summary>
		/// QueryProviderExecute
		/// </summary>
		QueryProviderExecute,
		/// <summary>
		/// QueryProviderGetEnumeratorT
		/// </summary>
		/// <summary>
		/// QueryProviderGetEnumeratorT
		/// </summary>
		QueryProviderGetEnumeratorT,
		/// <summary>
		/// QueryProviderGetEnumerator
		/// </summary>
		/// <summary>
		/// QueryProviderGetEnumerator
		/// </summary>
		QueryProviderGetEnumerator,
		/// <summary>
		/// Gets GetQueryTotal.
		/// </summary>
		/// <summary>
		/// Gets GetQueryTotal.
		/// </summary>
		GetQueryTotal,
			/// <summary>
			/// Gets GetQueryFind.
			/// </summary>
			/// <summary>
			/// Gets GetQueryFind.
			/// </summary>
			GetQueryFind,
				/// <summary>
				/// Gets GetQueryFindExpose.
				/// </summary>
				/// <summary>
				/// Gets GetQueryFindExpose.
				/// </summary>
				GetQueryFindExpose,
				/// <summary>
				/// Gets GetQueryFindFind.
				/// </summary>
				/// <summary>
				/// Gets GetQueryFindFind.
				/// </summary>
				GetQueryFindFind,
			/// <summary>
			/// Gets GetQueryCreate.
			/// </summary>
			/// <summary>
			/// Gets GetQueryCreate.
			/// </summary>
			GetQueryCreate,    //在Query<T> 中执行
				/// <summary>
				/// Builds SQL for Build.
				/// </summary>
				/// <summary>
				/// Builds SQL for Build.
				/// </summary>
				Build,			//在 ClauseSqlTranslator / ClauseCompiler 下
					/// <summary>
					/// Builds SQL for BuildSequence.
					/// </summary>
					/// <summary>
					/// Builds SQL for BuildSequence.
					/// </summary>
					BuildSequence,
						/// <summary>
						/// Builds SQL for BuildSequenceCanBuild.
						/// </summary>
						/// <summary>
						/// Builds SQL for BuildSequenceCanBuild.
						/// </summary>
						BuildSequenceCanBuild,
						/// <summary>
						/// Builds SQL for BuildSequenceBuild.
						/// </summary>
						/// <summary>
						/// Builds SQL for BuildSequenceBuild.
						/// </summary>
						BuildSequenceBuild,
					/// <summary>
					/// Builds SQL for BuildQuery.
					/// </summary>
					/// <summary>
					/// Builds SQL for BuildQuery.
					/// </summary>
					BuildQuery,
						/// <summary>
						/// FinalizeQuery
						/// </summary>
						/// <summary>
						/// FinalizeQuery
						/// </summary>
						FinalizeQuery,
			/// <summary>
			/// Gets GetIEnumerable.
			/// </summary>
			/// <summary>
			/// Gets GetIEnumerable.
			/// </summary>
			GetIEnumerable,
		/// <summary>
		/// ExecuteQuery
		/// </summary>
		/// <summary>
		/// ExecuteQuery
		/// </summary>
		ExecuteQuery,
		/// <summary>
		/// ExecuteQueryAsync
		/// </summary>
		/// <summary>
		/// ExecuteQueryAsync
		/// </summary>
		ExecuteQueryAsync,
		/// <summary>
		/// ExecuteElement
		/// </summary>
		/// <summary>
		/// ExecuteElement
		/// </summary>
		ExecuteElement,
		/// <summary>
		/// ExecuteElementAsync
		/// </summary>
		/// <summary>
		/// ExecuteElementAsync
		/// </summary>
		ExecuteElementAsync,
		/// <summary>
		/// ExecuteScalar
		/// </summary>
		/// <summary>
		/// ExecuteScalar
		/// </summary>
		ExecuteScalar,
		/// <summary>
		/// ExecuteScalarAsync
		/// </summary>
		/// <summary>
		/// ExecuteScalarAsync
		/// </summary>
		ExecuteScalarAsync,
		/// <summary>
		/// ExecuteNonQuery
		/// </summary>
		/// <summary>
		/// ExecuteNonQuery
		/// </summary>
		ExecuteNonQuery,
		/// <summary>
		/// ExecuteNonQueryAsync
		/// </summary>
		/// <summary>
		/// ExecuteNonQueryAsync
		/// </summary>
		ExecuteNonQueryAsync,
		/// <summary>
		/// ExecuteNonQuery2
		/// </summary>
		/// <summary>
		/// ExecuteNonQuery2
		/// </summary>
		ExecuteNonQuery2,
		/// <summary>
		/// ExecuteNonQuery2Async
		/// </summary>
		/// <summary>
		/// ExecuteNonQuery2Async
		/// </summary>
		ExecuteNonQuery2Async,
		/// <summary>
		/// Alternative implementation of &lt; cref="linq.DataExtensions.InsertOrReplace{T}(IDataContext,T,InsertOrUpdateColumnFilter{T}?,string?,string?,string?,string?,TableOptions)"/&gt; method.
		/// </summary>
		ExecuteScalarAlternative,
		/// <summary>
		/// ExecuteScalarAlternativeAsync
		/// </summary>
		/// <summary>
		/// ExecuteScalarAlternativeAsync
		/// </summary>
		ExecuteScalarAlternativeAsync,
		/// <summary>
		/// Creates CreateTable.
		/// </summary>
		/// <summary>
		/// Creates CreateTable.
		/// </summary>
		CreateTable,
		/// <summary>
		/// Creates CreateTableAsync.
		/// </summary>
		/// <summary>
		/// Creates CreateTableAsync.
		/// </summary>
		CreateTableAsync,
		/// <summary>
		/// DropTable
		/// </summary>
		/// <summary>
		/// DropTable
		/// </summary>
		DropTable,
		/// <summary>
		/// DropTableAsync
		/// </summary>
		/// <summary>
		/// DropTableAsync
		/// </summary>
		DropTableAsync,
		/// <summary>
		/// DeleteObject
		/// </summary>
		/// <summary>
		/// DeleteObject
		/// </summary>
		DeleteObject,
		/// <summary>
		/// DeleteObjectAsync
		/// </summary>
		/// <summary>
		/// DeleteObjectAsync
		/// </summary>
		DeleteObjectAsync,
		/// <summary>
		/// InsertObject
		/// </summary>
		/// <summary>
		/// InsertObject
		/// </summary>
		InsertObject,
		/// <summary>
		/// InsertObjectAsync
		/// </summary>
		/// <summary>
		/// InsertObjectAsync
		/// </summary>
		InsertObjectAsync,
		/// <summary>
		/// InsertOrReplaceObject
		/// </summary>
		/// <summary>
		/// InsertOrReplaceObject
		/// </summary>
		InsertOrReplaceObject,
		/// <summary>
		/// InsertOrReplaceObjectAsync
		/// </summary>
		/// <summary>
		/// InsertOrReplaceObjectAsync
		/// </summary>
		InsertOrReplaceObjectAsync,
		/// <summary>
		/// InsertWithIdentityObject
		/// </summary>
		/// <summary>
		/// InsertWithIdentityObject
		/// </summary>
		InsertWithIdentityObject,
		/// <summary>
		/// InsertWithIdentityObjectAsync
		/// </summary>
		/// <summary>
		/// InsertWithIdentityObjectAsync
		/// </summary>
		InsertWithIdentityObjectAsync,
		/// <summary>
		/// UpdateObject
		/// </summary>
		/// <summary>
		/// UpdateObject
		/// </summary>
		UpdateObject,
		/// <summary>
		/// UpdateObjectAsync
		/// </summary>
		/// <summary>
		/// UpdateObjectAsync
		/// </summary>
		UpdateObjectAsync,
		/// <summary>
		/// BulkCopy
		/// </summary>
		/// <summary>
		/// BulkCopy
		/// </summary>
		BulkCopy,
		/// <summary>
		/// BulkCopyAsync
		/// </summary>
		/// <summary>
		/// BulkCopyAsync
		/// </summary>
		BulkCopyAsync,
			/// <summary>
			/// Builds SQL for BuildSql.
			/// </summary>

			BuildSql,
		/// <summary>
		/// CommandInfoExecute
		/// </summary>

		CommandInfoExecute,
		/// <summary>
		/// CommandInfoExecuteT
		/// </summary>
		/// <summary>
		/// CommandInfoExecuteT
		/// </summary>
		CommandInfoExecuteT,
		/// <summary>
		/// CommandInfoExecuteCustom
		/// </summary>
		/// <summary>
		/// CommandInfoExecuteCustom
		/// </summary>
		CommandInfoExecuteCustom,
		/// <summary>
		/// CommandInfoExecuteAsync
		/// </summary>
		/// <summary>
		/// CommandInfoExecuteAsync
		/// </summary>
		CommandInfoExecuteAsync,
		/// <summary>
		/// CommandInfoExecuteAsyncT
		/// </summary>
		/// <summary>
		/// CommandInfoExecuteAsyncT
		/// </summary>
		CommandInfoExecuteAsyncT,
			/// <summary>
			/// ConnectionOpen
			/// </summary>

			ConnectionOpen,
			/// <summary>
			/// ConnectionOpenAsync
			/// </summary>
			/// <summary>
			/// ConnectionOpenAsync
			/// </summary>
			ConnectionOpenAsync,
			/// <summary>
			/// ConnectionClose
			/// </summary>
			/// <summary>
			/// ConnectionClose
			/// </summary>
			ConnectionClose,
			/// <summary>
			/// ConnectionCloseAsync
			/// </summary>
			/// <summary>
			/// ConnectionCloseAsync
			/// </summary>
			ConnectionCloseAsync,
			/// <summary>
			/// ConnectionDispose
			/// </summary>
			/// <summary>
			/// ConnectionDispose
			/// </summary>
			ConnectionDispose,
			/// <summary>
			/// ConnectionDisposeAsync
			/// </summary>
			/// <summary>
			/// ConnectionDisposeAsync
			/// </summary>
			ConnectionDisposeAsync,
			/// <summary>
			/// ConnectionBeginTransaction
			/// </summary>
			/// <summary>
			/// ConnectionBeginTransaction
			/// </summary>
			ConnectionBeginTransaction,
			/// <summary>
			/// ConnectionBeginTransactionAsync
			/// </summary>
			/// <summary>
			/// ConnectionBeginTransactionAsync
			/// </summary>
			ConnectionBeginTransactionAsync,
			/// <summary>
			/// TransactionCommit
			/// </summary>
			/// <summary>
			/// TransactionCommit
			/// </summary>
			TransactionCommit,
			/// <summary>
			/// TransactionCommitAsync
			/// </summary>
			/// <summary>
			/// TransactionCommitAsync
			/// </summary>
			TransactionCommitAsync,
			/// <summary>
			/// TransactionRollback
			/// </summary>
			/// <summary>
			/// TransactionRollback
			/// </summary>
			TransactionRollback,
			/// <summary>
			/// TransactionRollbackAsync
			/// </summary>
			/// <summary>
			/// TransactionRollbackAsync
			/// </summary>
			TransactionRollbackAsync,
			/// <summary>
			/// TransactionDispose
			/// </summary>
			/// <summary>
			/// TransactionDispose
			/// </summary>
			TransactionDispose,
			/// <summary>
			/// TransactionDisposeAsync
			/// </summary>
			/// <summary>
			/// TransactionDisposeAsync
			/// </summary>
			TransactionDisposeAsync,
			/// <summary>
			/// CommandExecuteScalar
			/// </summary>
			/// <summary>
			/// CommandExecuteScalar
			/// </summary>
			CommandExecuteScalar,
			/// <summary>
			/// CommandExecuteScalarAsync
			/// </summary>
			/// <summary>
			/// CommandExecuteScalarAsync
			/// </summary>
			CommandExecuteScalarAsync,
			/// <summary>
			/// CommandExecuteReader
			/// </summary>
			/// <summary>
			/// CommandExecuteReader
			/// </summary>
			CommandExecuteReader,
			/// <summary>
			/// CommandExecuteReaderAsync
			/// </summary>
			/// <summary>
			/// CommandExecuteReaderAsync
			/// </summary>
			CommandExecuteReaderAsync,
			/// <summary>
			/// CommandExecuteNonQuery
			/// </summary>
			/// <summary>
			/// CommandExecuteNonQuery
			/// </summary>
			CommandExecuteNonQuery,
			/// <summary>
			/// CommandExecuteNonQueryAsync
			/// </summary>
			/// <summary>
			/// CommandExecuteNonQueryAsync
			/// </summary>
			CommandExecuteNonQueryAsync,

		/// <summary>CommandInitialized 拦截器方法调用。</summary>
		CommandInterceptorCommandInitialized,
		/// <summary>ExecuteScalar 拦截器方法调用。</summary>
		CommandInterceptorExecuteScalar,
		/// <summary>ExecuteScalarAsync 拦截器方法调用。</summary>
		CommandInterceptorExecuteScalarAsync,
		/// <summary>ExecuteNonQuery 拦截器方法调用。</summary>
		CommandInterceptorExecuteNonQuery,
		/// <summary>ExecuteNonQueryAsync 拦截器方法调用。</summary>
		CommandInterceptorExecuteNonQueryAsync,
		/// <summary>ExecuteReader 拦截器方法调用。</summary>
		CommandInterceptorExecuteReader,
		/// <summary>ExecuteReaderAsync 拦截器方法调用。</summary>
		CommandInterceptorExecuteReaderAsync,
		/// <summary>AfterExecuteReader 拦截器方法调用。</summary>
		CommandInterceptorAfterExecuteReader,
		/// <summary>BeforeReaderDispose 拦截器方法调用。</summary>
		CommandInterceptorBeforeReaderDispose,
		/// <summary>BeforeReaderDisposeAsync 拦截器方法调用。</summary>
		CommandInterceptorBeforeReaderDisposeAsync,

		/// <summary>ConnectionOpening 拦截器方法调用。</summary>
		ConnectionInterceptorConnectionOpening,
		/// <summary>ConnectionOpeningAsync 拦截器方法调用。</summary>
		ConnectionInterceptorConnectionOpeningAsync,
		/// <summary>ConnectionOpened 拦截器方法调用。</summary>
		ConnectionInterceptorConnectionOpened,
		/// <summary>ConnectionOpenedAsync 拦截器方法调用。</summary>
		ConnectionInterceptorConnectionOpenedAsync,

		/// <summary>OnClosing 拦截器方法调用。</summary>
		DataContextInterceptorOnClosing,
		/// <summary>OnClosingAsync 拦截器方法调用。</summary>
		DataContextInterceptorOnClosingAsync,
		/// <summary>OnClosed 拦截器方法调用。</summary>
		DataContextInterceptorOnClosed,
		/// <summary>OnClosedAsync 拦截器方法调用。</summary>
		DataContextInterceptorOnClosedAsync,

		/// <summary>EntityCreated 拦截器方法调用。</summary>
		EntityServiceInterceptorEntityCreated,

		/// <summary>UnwrapConnection 拦截器方法调用。</summary>
		UnwrapDataObjectInterceptorUnwrapConnection,
		/// <summary>UnwrapTransaction 拦截器方法调用。</summary>
		UnwrapDataObjectInterceptorUnwrapTransaction,
		/// <summary>UnwrapCommand 拦截器方法调用。</summary>
		UnwrapDataObjectInterceptorUnwrapCommand,
		/// <summary>UnwrapDataReader 拦截器方法调用。</summary>
		UnwrapDataObjectInterceptorUnwrapDataReader,
		/// <summary>
		/// Gets GetSqlText.
		/// </summary>

		GetSqlText,
			/// <summary>
			/// Materialization
			/// </summary>

			Materialization,
			/// <summary>
			/// OnTraceInternal
			/// </summary>
			/// <summary>
			/// OnTraceInternal
			/// </summary>
			OnTraceInternal,
	}
}
