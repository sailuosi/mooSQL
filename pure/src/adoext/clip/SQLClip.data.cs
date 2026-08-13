using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.clip;
using mooSQL.data.clip.project;

namespace mooSQL.data
{
    /// <summary>
    /// SQLClip的上下文
    /// </summary>
    public partial class ClipContext
    {

        /// <summary>
        /// 构造函数。
        /// </summary>
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
        /// <summary>
        /// 构建器
        /// </summary>
        public SQLBuilder Builder { 
            get { return _builder; }
            set { _builder = value; }
        }
        /// <summary>
        /// 字段数量
        /// </summary>
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

        /// <summary>
        /// 非 null 时 query* 走客户端尾投影（阶段 A 槽位列 + 阶段 B 投影器）。
        /// </summary>
        internal ProjectionPlan ClientProjection { get; set; }

        /// <summary>
        /// 尾调用在列值为 null 时不抛 NRE，改为返回 null（值类型提升为 Nullable）。
        /// </summary>
        public bool NullPropagateTailCalls { get; set; }

        /// <summary>
        /// 尾投影委托用表达式解释执行（AOT/裁剪友好折中；非源生成）。
        /// </summary>
        public bool PreferInterpretedTailProjector { get; set; }

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
            ClientProjection = null;
            _builder.clear();
        }
    }

    internal enum BuildSQLType { 
        Select=1,
        Edit=2,

    
    }
}