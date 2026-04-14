using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 实体持久化（插入/更新/删除）链式 API 的抽象基类，提供表名解析、参与更新的字段包含/排除以及事务执行器绑定。
    /// </summary>
    /// <typeparam name="C">派生类型，用于方法链返回具体子类实例。</typeparam>
    public abstract class EntitySaveBase<C> where C: EntitySaveBase<C>
    {
        /// <summary>当前使用的数据库实例。</summary>
        protected DBInstance DBLive;
        /// <summary>实体与 SQL 构建之间的翻译器（表名、字段映射及语句准备）。</summary>
        protected EntityTranslator translator;

        /// <summary>非空时表示在指定执行器/事务上下文中执行后续操作。</summary>
        protected DBExecutor executor;


        /// <summary>
        /// 使用指定数据库实例初始化，并从客户端工厂获取实体翻译器。
        /// </summary>
        /// <param name="DB">数据库实例。</param>
        public EntitySaveBase(DBInstance DB)
        {
            this.DBLive = DB;
            this.translator = DB.client.ClientFactory.getEntityTranslator();
        }



        /// <summary>
        /// 指定在已有 <see cref="DBExecutor"/>（通常对应事务）中执行后续的插入、更新或删除。
        /// </summary>
        /// <param name="executor">要绑定的执行器；可为空表示不使用显式事务。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public C useTransaction(DBExecutor executor)
        {
            this.executor = executor;
            return (C)this;
        }

        /// <summary>
        /// 自定义实体对应的数据库表名解析逻辑（例如分表、动态表名）。
        /// </summary>
        /// <param name="onSetTableName">根据实体元信息返回实际表名的委托。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public C setTable(Func<EntityInfo, string> onSetTableName)
        {
            this.translator.setTable(onSetTableName);
            return (C)this;
        }
        /// <summary>
        /// 指定参与更新操作的字段白名单（仅这些列会出现在 SET 中）。
        /// </summary>
        /// <param name="fields">属性名或列逻辑名集合。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public C include(IEnumerable<string> fields)
        {
            this.translator.includeUpdate(fields);
            return (C)this;
        }
        /// <summary>
        /// 指定参与更新操作的字段白名单（仅这些列会出现在 SET 中）。
        /// </summary>
        /// <param name="fields">属性名或列逻辑名。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public C include(params string[] fields)
        {
            this.translator.includeUpdate(fields);
            return (C)this;
        }

        /// <summary>
        /// 指定更新时要忽略的字段（不出现在 SET 中）。
        /// </summary>
        /// <param name="fields">属性名或列逻辑名集合。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public C ignore(IEnumerable<string> fields)
        {
            this.translator.ignoreUpdate(fields);
            return (C)this;
        }

        /// <summary>
        /// 指定更新时要忽略的字段（不出现在 SET 中）。
        /// </summary>
        /// <param name="fields">属性名或列逻辑名。</param>
        /// <returns>当前实例，用于链式调用。</returns>
        public C ignore(params string[] fields)
        {
            this.translator.ignoreUpdate(fields);
            return (C)this;
        }
    }
}
