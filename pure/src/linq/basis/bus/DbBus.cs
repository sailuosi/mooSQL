using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 用于查询和保存数据层实体类
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class DbBus<TEntity> : BaseDbBus<TEntity>
    {
        /// <summary>
        /// 人力资源部
        /// </summary>
        protected LinqDbFactory LinqFactory;
        /// <summary>
        /// 构建特定工厂的实例
        /// </summary>
        /// <param name="factory"></param>
        public DbBus(LinqDbFactory factory) { 
            this.LinqFactory = factory;
        }



        public abstract Type EntityType { get; }

    }
}
