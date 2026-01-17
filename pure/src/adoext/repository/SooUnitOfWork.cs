using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 带有事务功能，可累积一些仓储动作并最后执行和释放。工作单元。
    /// </summary>
    public class SooUnitOfWork : IDisposable
    {
        /// <summary>
        /// 数据库信息
        /// </summary>
        public DBInstance DB {  get; set; }
        /// <summary>
        /// 以某个数据库实例，创建工作单元
        /// </summary>
        /// <param name="db"></param>
        public SooUnitOfWork(DBInstance db) { 
            DB = db; 
            this.SQLCommands = new List<SQLCmd>();
            this.IsTran = true;
        }
        /// <summary>
        /// 是否开启事务执行SQL，默认是true。
        /// </summary>
        public bool IsTran { get; internal set; }
        /// <summary>
        /// 是否已经提交，默认是false。
        /// </summary>
        public bool IsCommit { get; internal set; }
        /// <summary>
        /// 是否已经关闭，默认是false。
        /// </summary>
        public bool IsClose { get; internal set; }

        private Action<Exception> _onException = null;
        /// <summary>
        /// 是否使用事务执行SQL，默认是true。
        /// </summary>
        /// <param name="useTran"></param>
        /// <returns></returns>
        public SooUnitOfWork UseTransaction(bool useTran = true) {
            this.IsTran = useTran;
            return this;
        }

        public SooUnitOfWork WhenError(Action<Exception> onError) {
            this._onException = onError;
            return this;
        }

        private List<SQLCmd> SQLCommands;
        /// <summary>
        /// 释放资源，暂未实现。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 提交
        /// </summary>
        /// <returns></returns>
        public int Commit() {

            try
            {
                var runner = DB.useBatchSQL();
                if (this.IsTran) { 
                    runner.useTransaction = true;
                }
                runner.addSQLs(this.SQLCommands);
                var cc = runner.exeNonQuery();
                return cc;
            }
            catch (Exception ex) {
                if (this._onException != null) { 
                    this._onException(ex);
                }
                return -1;
            }

        }
        /// <summary>
        /// 添加一个SQL
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public SooUnitOfWork AddSQL(SQLCmd cmd) { 
            if (cmd == null || string.IsNullOrEmpty(cmd.sql)) return this;
            this.SQLCommands.Add(cmd);
            return this;
        }
        /// <summary>
        /// 添加一组SQL
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public SooUnitOfWork AddSQLs(IEnumerable<SQLCmd> cmd)
        {
            if (cmd == null) return this;
            foreach (var item in cmd) {
                AddSQL(item);
            }
            return this;
        }
        /// <summary>
        /// 通过SQL构建器添加一个插入语句，并执行。
        /// </summary>
        /// <param name="builderInsert"></param>
        /// <returns></returns>
        public SooUnitOfWork InsertBySQL(Action<SQLBuilder> builderInsert)
        {
            var kit = DB.useSQL();
            builderInsert(kit);
            var cmd = kit.toInsert();
            AddSQL(cmd);

            return this;
        }

        /// <summary>
        /// 添加一个
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork Insert<T>(T entity) {
            if (entity == null) return this;
            var kit=DB.useSQL();
            var cmd=kit.toInsert(entity);
            return AddSQL(cmd);
        }
        /// <summary>
        /// 批量新增
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork InsertRange<T>(IEnumerable<T> entity)
        {
            if (entity == null) return this;
            var kit = DB.useSQL();
            foreach (var item in entity) {
                var cmd = kit.toInsert(item);
                AddSQL(cmd);
            }
            return this;
        }
        /// <summary>
        /// 单个更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork Update<T>(T entity)
        {
            if (entity == null) return this;
            var kit = DB.useSQL();
            var cmd = kit.toUpdate(entity);
            return AddSQL(cmd);
        }

        /// <summary>
        /// 批量更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork UpdateRange<T>(IEnumerable<T> entity)
        {
            if (entity == null) return this;
            var kit = DB.useSQL();
            foreach (var item in entity)
            {
                var cmd = kit.toUpdate(item);
                AddSQL(cmd);
            }
            return this;
        }
        /// <summary>
        /// 批量保存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork SaveRange<T>(IEnumerable<T> entity)
        {
            if (entity == null) return this;
            var kit = DB.useSQL();
            var cmds= kit.toSave(entity);
            this.AddSQLs(cmds);
            return this;
        }
        /// <summary>
        /// 通过SQL构建器添加一个更新语句，等待执行。
        /// </summary>
        /// <param name="builderUpdate"></param>
        /// <returns></returns>
        public SooUnitOfWork UpdateBySQL(Action<SQLBuilder> builderUpdate)
        {
            var kit = DB.useSQL();
            builderUpdate(kit);
            var cmd = kit.toUpdate();
            AddSQL(cmd);

            return this;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork Delete<T>(T entity)
        {
            if (entity == null) return this;
            var kit = DB.useSQL();
            var cmd = kit.toDelete(entity);
            return AddSQL(cmd);
        }
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public SooUnitOfWork DeleteRange<T>(IEnumerable<T> entity)
        {
            if(entity ==null) return this;
            var kit = DB.useSQL();

            var cmd = kit.toDelete(entity);
            AddSQLs(cmd);
            
            return this;
        }
        /// <summary>
        /// 按ID删除，自动忽略null值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public SooUnitOfWork DeleteById<T,K>(K id)
        {
            if(id==null) return this;
            var kit = DB.useSQL();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];

            var cc = kit.setTable(en.DbTableName)
                .where(pk.DbColumnName, id)
                .toDelete();
            this.AddSQL(cc);
            return this;
        }
        /// <summary>
        /// 按ID，批量删除
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public SooUnitOfWork DeleteByIds<T,K>(K[] ids)
        {
            var kit = DB.useSQL();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            var cc = kit.setTable(en.DbTableName)
                .whereIn(pk.DbColumnName, ids)
                .toDelete();
            this.AddSQL(cc);
            return this;
        }
        /// <summary>
        /// 按ID进行批量删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="updateKey"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public SooUnitOfWork DeleteByIds<T, K>(Expression<Func<T, K>> updateKey, K[] ids)
        {
            var kit = DB.useSQL();
            var en = kit.DBLive.client.EntityCash.getEntityInfo(typeof(T));
            var pks = en.GetPK();
            if (pks.Count != 1)
            {
                throw new NotSupportedException("当前实体的主键信息不匹配！");
            }
            var pk = pks[0];
            var cc = kit.setTable(en.DbTableName)
                .whereIn(pk.DbColumnName, ids)
                .toDelete();
            this.AddSQL(cc);
            return this;
        }
        /// <summary>
        /// 通过SQL构建器添加一个删除语句，等待执行。
        /// </summary>
        /// <param name="builderDelete"></param>
        /// <returns></returns>
        public SooUnitOfWork DeleteBySQL(Action<SQLBuilder> builderDelete)
        {
            var kit = DB.useSQL();
            builderDelete(kit);
            var cmd = kit.toDelete();
            AddSQL(cmd);

            return this;
        }
    }
}
