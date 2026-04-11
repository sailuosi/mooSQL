using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 内置核心
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnDbBus<T> : DbBus<T> 
    {

        private EntityQueryable<T> _entityQueryable;
        private DbContext _context;
        private string _entityTypeName;
        private Type _entityType;

        /// <summary>
        /// 按实体类型名称（字符串）构造查询总线。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entityTypeName">实体类型全名。</param>
        /// <param name="factory">LINQ 工厂。</param>
        public EnDbBus(DbContext context,string entityTypeName,LinqDbFactory factory)
            :base(factory)
        { 
            _context = context;
            _entityTypeName = entityTypeName;
        }
        /// <summary>
        /// 按实体 <see cref="Type"/> 构造查询总线。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="entityType">实体类型。</param>
        /// <param name="factory">LINQ 工厂。</param>
        public EnDbBus(DbContext context, Type entityType, LinqDbFactory factory)
    : base(factory)
        {
            _context = context;
            _entityType = entityType;
        }
        private EntityQueryable<T> CreateEntityQueryable() {
            return this.LinqFactory.CreateEntityQueryable<T>(_context.EntityProvider, _entityType);
        }

        private EntityQueryable<T> EntityQueryable
        {
            get {
                if (_entityQueryable == null) { 
                    _entityQueryable= CreateEntityQueryable();
                }
                return _entityQueryable;
            }
        }

        /// <inheritdoc />
        public override Expression Expression => EntityQueryable.Expression;

        /// <inheritdoc />
        public override Type ElementType => EntityQueryable.ElementType;

        /// <inheritdoc />
        public override IQueryProvider Provider => EntityQueryable.Provider;

        /// <inheritdoc />
        public override IDbBusProvider BusProvider => EntityQueryable.BusProvider;

        /// <inheritdoc />
        public override Type EntityType
        {
            get {
                if (_entityType != null) {
                    return _entityType;
                }
                return _entityType;
            }
        }

        /// <inheritdoc />
        public override IEnumerator<T> GetEnumerator()
        {
            return EntityQueryable.GetEnumerator();
        }
    }
}
