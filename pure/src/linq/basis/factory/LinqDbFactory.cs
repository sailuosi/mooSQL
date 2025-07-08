using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 抽象工厂，提供自定义工厂的LINQ编译行为的可能
    /// </summary>
    public abstract class LinqDbFactory
    {

        private  EntityQueryProvider _entityQueryProvider;
        /// <summary>
        /// 实体提供器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public  EntityQueryProvider GetEntityQueryProvider(DBInstance DB) {
            if (_entityQueryProvider == null) { 
                _entityQueryProvider= new EntityQueryProvider(GetQueryCompiler(DB));
            }
            return _entityQueryProvider;
        }
        /// <summary>
        /// 实体查询器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <param name="_entityType"></param>
        /// <returns></returns>
        public EntityQueryable<T> CreateEntityQueryable<T>(IAsyncQueryProvider provider,Type _entityType)
        {
            return new EntityQueryable<T>(provider, _entityType);
        }
        /// <summary>
        /// 查询编译器
        /// </summary>
        /// <param name="DB"></param>
        /// <returns></returns>
        public abstract IQueryCompiler GetQueryCompiler(DBInstance DB);
    }
}
