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

        public EnDbBus(DbContext context,string entityTypeName,LinqDbFactory factory)
            :base(factory)
        { 
            _context = context;
            _entityTypeName = entityTypeName;
        }
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

        public override Expression Expression => EntityQueryable.Expression;

        public override Type ElementType => EntityQueryable.ElementType;

        public override IQueryProvider Provider => EntityQueryable.Provider;

        public override IDbBusProvider BusProvider => EntityQueryable.BusProvider;

        public override Type EntityType
        {
            get {
                if (_entityType != null) {
                    return _entityType;
                }
                return _entityType;
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return EntityQueryable.GetEnumerator();
        }
    }
}
