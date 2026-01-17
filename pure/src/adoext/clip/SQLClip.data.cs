using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.clip;

namespace mooSQL.data
{
    public partial class ClipContext
    {

        public ClipContext(SQLBuilder builder) { 
            _builder = builder;
            Joins = new List<ClipJoinData>();
            _bindTables = new Dictionary<object, ClipTable>();
            FieldCount = 0;
            _fromTarget = null;
            _fromBinded = false;
        }

        internal BuildSQLType BType { get; set; }

        internal SQLBuilder _builder;
        public SQLBuilder Builder { 
            get { return _builder; }
            set { _builder = value; }
        }

        public int FieldCount { get; internal set; }
        /// <summary>
        /// from绑定的实例对象
        /// </summary>
        internal object _fromTarget;
        /// <summary>
        /// 标识是否已解析完成from部分
        /// </summary>
        private bool _fromBinded = false;

        /// <summary>
        /// from绑定的实例对象
        /// </summary>
        internal object _updateTarget;
        /// <summary>
        /// 标识是否已解析完成from部分
        /// </summary>
        private bool _updateBinded = false;

        /// <summary>
        /// 绑定的目标表，lmda语句中，如果成员的值为绑定的表，则转换为对应的表引用，否则应直接求值。
        /// </summary>
        private Dictionary<object, ClipTable> _bindTables;

        internal Dictionary<object, ClipTable> BindTables
        {
            get { return _bindTables; }
        }

        internal bool FromBinded { 
                get { return _fromBinded; }
                set { _fromBinded = value; }
        }
        internal bool UpdateBinded
        {
            get { return _updateBinded; }
            set { _updateBinded = value; }
        }

        internal List<ClipJoinData> Joins { get; set; }


        internal ClipTable getFromTable() { 
            return _bindTables[_fromTarget];
        }
        internal ClipTable getSetTable()
        {
            return _bindTables[_updateTarget];
        }

        internal void BindFrom(object target, ClipTable table) {
            this._fromBinded = false;
            this._fromTarget = target;
            this._bindTables[target] = table;
            this.BType = BuildSQLType.Select;
        }

        internal void BindJoin(object target, ClipTable table) { 
            this._bindTables[target] = table;
        
        }
        internal void BindUpdate(object target, ClipTable table)
        {
            this._updateBinded = false;
            this._updateTarget = target;
            this._bindTables[target] = table;
            this.BType = BuildSQLType.Edit;
        }
        internal void clear() { 
            Joins.Clear();
            _bindTables.Clear();
            FieldCount = 0;
            _fromTarget = null;
            _fromBinded = false;
            _updateTarget = null;
            _updateBinded = false;
            _builder.clear();
        }
    }

    internal enum BuildSQLType { 
        Select=1,
        Edit=2,

    
    }
}
