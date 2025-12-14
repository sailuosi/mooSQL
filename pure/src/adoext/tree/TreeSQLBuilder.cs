// 基础功能说明：

using mooSQL.data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;


namespace mooSQL.data;
/// <summary>
/// 树形结构查询构建器
/// </summary>
public abstract class TreeSQLBuilder<K,RealMe> where RealMe: TreeSQLBuilder<K, RealMe>
{
    /* 机制说明
     * 1.查询时刻
     * 
     * 1.1 通用查询条件，基础树数据构建动作
     * 
     * 1.2 无上级条件，获取根节点集合时刻
     *     如树的第一次加载，获取树非过滤情况下，真正的根节点  
     *     
     * 1.3 已知上级ID，查询子级集合
     *     一般用作展开树时，拉取下层的数据
。
     * 1.4 无上级条件，但需进行过滤，截取一个子树展示
     *     一般为带权限加载，只看有权限的子树
     *
     * 1.5 搜索条件，需要对可查看的子级进行过滤时
     */


    /// <summary>
    /// 数据库实例对象
    /// </summary>
    public DBInstance DBLive { get; set; }
    /// <summary>
    /// 主键字段名
    /// </summary>
    protected string _PKField { get; set; }
    /// <summary>
    /// 外键字段名
    /// </summary>
    protected string _FKField { get; set; }
    /// <summary>
    /// 根节点的主键值，默认为null。
    /// </summary>
    public K RootPKVal { get; set; }
    /// <summary>
    /// 1.1 通用查询条件，基础树数据构建动作
    /// </summary>
    protected event Action<SQLBuilder>? _onQueryBase = null;
    /// <summary>
    /// 1.2 无上级条件，获取根节点集合时刻
    /// </summary>
    protected event Action<SQLBuilder>? _onFilterRootEmpty = null;
    /// <summary>
    /// 1.3 已知上级ID，查询子级集合
    /// </summary>
    protected Action<SQLBuilder, IEnumerable>? _whenLoadChildByFK = null;

    /// <summary>
    /// 1.4 无上级条件，但需进行过滤，截取一个子树展示
    /// </summary>
    protected event Action<SQLBuilder>? _onFilterRootScoped = null;
    /// <summary>
    /// 1.5 搜索条件，需要对可查看的子级进行过滤时
    /// </summary>
    protected event Action<SQLBuilder>? _onFilterChildScoped = null;
    /// <summary>
    /// 1.6 根据主键加载
    /// </summary>
    protected Action<SQLBuilder, K>? _whenLoadRootByPK = null;
    /// <summary>
    /// 查询深度
    /// </summary>
    protected int? _deep = 1;

    /// <summary>
    /// 最大循环次数
    /// </summary>
    protected int maxLoop = 50;

    /// <summary>
    /// 构造函数，传入数据库实例对象。
    /// </summary>
    /// <param name="DB"></param>
    public TreeSQLBuilder(DBInstance DB)
    {
        this.DBLive = DB;
        this.maxLoop = 50;
    }
    /// <summary>
    /// 设置主键和外键字段
    /// </summary>
    /// <param name="PKName"></param>
    /// <param name="FKName"></param>
    /// <returns></returns>
    public RealMe selectPKFK(string PKName, string FKName) { 
        this._PKField = PKName;
        this._FKField = FKName;
        return (RealMe)this;
    }
    /// <summary>
    /// 设置根节点的主键值，默认为null。
    /// </summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public RealMe setPK(K pk)
    {
        this.RootPKVal = pk;
        return (RealMe)this; 
    }
    /// <summary>
    /// 1.1 通用查询条件，基础树数据构建动作
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public RealMe onQueryBase(Action<SQLBuilder> onQuery)
    {
        this._onQueryBase += onQuery;
        return (RealMe)this; 
    }
    /// <summary>
    /// 1.2 无上级条件，获取根节点集合时刻
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public RealMe onFilterRootEmpty(Action<SQLBuilder> onQuery)
    {
        this._onFilterRootEmpty += onQuery;
        return (RealMe)this;
    }
    /// <summary>
    /// 1.3 已知上级ID，查询子级集合
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public RealMe whenLoadChildByFK(Action<SQLBuilder,IEnumerable> onQuery)
    {
        this._whenLoadChildByFK = onQuery;
        return (RealMe)this;
    }

    /// <summary>
    /// 1.4 无上级条件，但需进行过滤，截取一个子树展示
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public RealMe onFilterRootScoped(Action<SQLBuilder> onQuery)
    {
        this._onFilterRootScoped += onQuery;
        return (RealMe)this;
    }

    /// <summary>
    /// 1.5 搜索条件，需要对可查看的子级进行过滤时
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public RealMe onFilterChildScoped(Action<SQLBuilder> onQuery)
    {
        this._onFilterChildScoped += onQuery;
        return (RealMe)this;
    }
    /// <summary>
    /// 设置一次性查询深度，默认为1层。
    /// </summary>
    /// <param name="deep"></param>
    /// <returns></returns>
    public RealMe setDeep(int? deep)
    {
        if (deep == null)
            return (RealMe)this;
        this._deep = deep.Value;
        return (RealMe)this;
    }
    /// <summary>
    /// 加载顶级范围节点数据集
    /// </summary>
    /// <param name="kit"></param>
    /// <returns></returns>
    protected virtual DataTable LoadRootValuesScoped(SQLBuilder kit)
    {
        kit.clear();
        if (_onQueryBase != null)
        {
            _onQueryBase(kit);
        }

        if (_onFilterRootScoped != null)
        {
            _onFilterRootScoped(kit);
        }
        else if(_FKField.HasText())
        {
            kit.whereIsNull(_FKField);
        }
        return kit.query();
    }

    /// <summary>
    /// 获取顶级数据，局部情况下
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="kit"></param>
    /// <param name="rowToTar"></param>
    /// <returns></returns>
    protected virtual List<TreeNodeOutput<T, K>> loadRootListScoped<T>(SQLBuilder kit, Func<DataRow, T> rowToTar)
    {

        var dt = LoadRootValuesScoped(kit);

        //加载其上级节点信息
        var parentMap = new Dictionary<K, DataRow>();
        foreach (DataRow row in dt.Rows)
        {
            var pkVal = getFieldVal(row, _PKField);
            parentMap[pkVal] = row;
        }
        CacadeFindParent(kit, parentMap, dt);

        //把数据组织成树结构形式
        var topNodes = new List<TreeNodeOutput<T>>();
        //循环记录进行处理，递归查找其父记录，直到找不到，则为根节点。
        var tar = geneTreeNodeByMap(parentMap, rowToTar);
        return tar;
    }

    /// <summary>
    /// 转换结果集为树结果数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="map"></param>
    /// <param name="rowToTar"></param>
    /// <returns></returns>
    protected virtual List<TreeNodeOutput<T, K>> geneTreeNodeByMap<T>(Dictionary<K, DataRow> map, Func<DataRow, T> rowToTar)
    {

        var topNodes = new List<TreeNodeOutput<T, K>>();
        var topKeys = new List<K>();
        var resMap = new Dictionary<K, TreeNodeOutput<T, K>>();
        //整理出顶级的外键值，并把当前映射转化为结果对象映射
        foreach (var kv in map)
        {
            if (kv.Value == null)
            {
                topKeys.Add(kv.Key);
                continue;
            }
            var row = kv.Value;

            var pkVal = getFieldVal(row, _PKField);
            var fkR = getFieldVal(row, _FKField);
            var nodeVal = new TreeNodeOutput<T, K>()
            {
                Record = rowToTar(row),
                Children = new List<TreeNodeOutput<T, K>>(),
                Level = -1,//标识未知
                PKValue = pkVal,
                FKValue = fkR
            };
            resMap[kv.Key] = nodeVal;
        }
        //为每个对象挂载其子级集合
        foreach (var kv in resMap)
        {
            //
            var pk = kv.Value.PKValue;
            foreach (var lv2 in resMap)
            {
                if (lv2.Value == null) continue;
                if (lv2.Value.FKValue.Equals(pk))
                {

                    kv.Value.Children.Add(lv2.Value);
                }
            }
            //顶级条件：所属的外键对象不存在
            var fk = kv.Value.FKValue;
            if (!resMap.ContainsKey(fk))
            {
                topNodes.Add(kv.Value);
            }
        }
        return topNodes;
    }
    /// <summary>
    /// 递归查找父级
    /// </summary>
    /// <param name="kit"></param>
    /// <param name="rowMap"></param>
    /// <param name="dt"></param>
    protected virtual void CacadeFindParent(SQLBuilder kit, Dictionary<K, DataRow> rowMap, DataTable dt)
    {

        var notLoadPKs = new List<K>();
        foreach (DataRow row in dt.Rows)
        {
            var pkVal = getFieldVal(row, _FKField);
            //找到过的不再查找
            if (rowMap.ContainsKey(pkVal))
            {
                continue;
            }
            notLoadPKs.Add(pkVal);
        }
        if (notLoadPKs.Count > 0)
        {
            var lvlist = this.LoadByPKValues(kit, notLoadPKs);
            //把查到的结果，挂到字典里，如果没有，视为顶级节点
            foreach (DataRow row in lvlist.Rows)
            {
                var pkVal = getFieldVal(row, _PKField);
                rowMap[pkVal] = row;
            }

            //检查未找到的
            foreach (var pk in notLoadPKs)
            {
                if (rowMap.ContainsKey(pk) == false)
                {
                    rowMap[pk] = null;
                }
            }

            //递归寻找其下一级
            CacadeFindParent(kit, rowMap, lvlist);
        }
    }

    /// <summary>
    /// 检查是否带权限查根节点
    /// </summary>
    /// <returns></returns>
    protected bool checkRootScoped()
    {
        if (this.RootPKVal != null)
        {
            return false;
        }
        if (this._onFilterRootScoped == null)
        {
            return false;
        }
        return true;
    }

    protected virtual List<TreeNodeOutput<T, K>> DtToTreeNode<T>(DataTable dt, Func<DataRow, T> rowToTar,int lv) {
        var topNodes = new List<TreeNodeOutput<T, K>>();
        if (dt != null)
        {
            //没有根节点时，返回值需要包含顶层节点
            foreach (DataRow row in dt.Rows)
            {
                var pkVal = getFieldVal(row, _PKField);
                var fkR = getFieldVal(row, _FKField);
                var nodeVal = new TreeNodeOutput<T, K>()
                {
                    Record = rowToTar(row),
                    Children = new List<TreeNodeOutput<T, K>>(),
                    Level = lv + 1,
                    PKValue = pkVal,
                    FKValue = fkR
                };
                topNodes.Add(nodeVal);
            }

        }
        return topNodes;
    }


    /// <summary>
    /// 加载顶层节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="rowToTar"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public TreeListOutput<T,K> loadTreeList<T>(Func<DataRow, T> rowToTar)
    {
        //先获取关联定义，然后递归查询所有下级


        if (string.IsNullOrWhiteSpace(_PKField) || string.IsNullOrWhiteSpace(_FKField))
        {
            throw new NotSupportedException("定义的主键字段、外键字段为空！");
        }
        var res = new TreeListOutput<T, K>();
        var kit = DBLive.useSQL();
        if (this.checkRootScoped())
        {
            var authTop = this.loadRootListScoped(kit, rowToTar);
            res.Nodes = authTop;
            return res;
        }




        var fk = _FKField;
        var lv = 0;
        var pk = _PKField;

        var rootDt = this.LoadRootValues(kit);
        var topNodes = this.DtToTreeNode(rootDt, rowToTar, lv);

        var preLvNodes = topNodes;
        var total = 0;
        //为防止自循环和无限循环，每次取下级时，如果其主键在之前取过的父级中，则忽略它。
        var paPks = getPKValue(rootDt);
        var fkVals = paPks;
        while (fkVals.Count > 0)
        {
            var lvlist = this.LoadChildValuesByFK(kit, fkVals);
            if (lvlist == null || lvlist.Rows.Count == 0)
                break;

            //获取主键值
            var lvNodes = new List<TreeNodeOutput<T, K>>();
            var lvPks = new List<K>();
            foreach (DataRow lvitem in lvlist.Rows)
            {
                //获取主键值

                var pkVal = getFieldVal(lvitem, _PKField);
                if (pkVal != null && !paPks.Contains(pkVal))
                {
                    lvPks.Add(pkVal);
                    paPks.Add(pkVal);
                }
                //外键值
                var fkR = getFieldVal(lvitem, _FKField);
                var nodeVal = new TreeNodeOutput<T, K>()
                {
                    Record = rowToTar(lvitem),
                    Children = new List<TreeNodeOutput<T, K>>(),
                    Level = lv + 1,
                    PKValue = pkVal,
                    FKValue = fkR
                };
                lvNodes.Add(nodeVal);
                bool isTop = true;
                foreach (var node in preLvNodes)
                {

                    if (node.PKValue!=null && fkR!=null && (node.PKValue.Equals(fkR)|| node.PKValue.ToString() == fkR.ToString()))
                    {
                        node.Children.Add(nodeVal);
                        total++;
                        isTop = false;
                        break;
                    }
                }
                if (isTop && fkR!=null &&this.RootPKVal !=null && fkR.ToString() == this.RootPKVal.ToString())
                {
                    topNodes.Add(nodeVal);
                    total++;
                }
            }
            fkVals = lvPks;
            lv++;
            if (lv >= this._deep)
            {
                break;
            }
            //初始化下一层的节点集合
            preLvNodes = lvNodes;
            if (lv > maxLoop) throw new NotSupportedException("递归层级过多，可能存在无限循环！最大支持50层！");
        }
        res.Nodes = topNodes;
        return res;
    }
    /// <summary>
    /// 获取字段值
    /// </summary>
    /// <param name="row"></param>
    /// <param name="FieldName"></param>
    /// <returns></returns>
    protected virtual K getFieldVal(DataRow row,string FieldName) {
        var v = row[FieldName];
        if (v is K)
        {
            return (K)v;
        }
        var v2 = (K)(object)v;
        if (v2 != null)
        {
            return v2;
        }
        return default;
    }
    /// <summary>
    /// 获取字段值
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    protected virtual List<K> loadFieldValues(DataTable dt,string fieldName)
    {
        var res = new List<K>();
        foreach (DataRow row in dt.Rows)
        {
            var v = row[fieldName];
            if (v is K)
            {
                res.Add((K)v);
                continue;
            }
            var v2 = (K)(object)v;
            if (v2 != null)
            {
                res.Add(v2);
            }

        }
        return res;
    }
    /// <summary>
    /// 获取主键
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    protected virtual List<K> getPKValue(DataTable dt) {
        return this.loadFieldValues(dt, _PKField);
    }
    /// <summary>
    /// 取得外键
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    protected virtual List<K> getFKValue(DataTable dt)
    {
        return this.loadFieldValues(dt, _FKField);
    }
    /// <summary>
    /// 加载子级
    /// </summary>
    /// <param name="kit"></param>
    /// <param name="pkVal"></param>
    /// <returns></returns>
    protected virtual DataTable LoadChildValuesByFK(SQLBuilder kit, IEnumerable pkVal)
    {
        kit.clear();

        if (_onQueryBase != null)
        {
            _onQueryBase(kit);
        }
        //自定义子级查询逻辑唤起
        if (_whenLoadChildByFK != null)
        {
            _whenLoadChildByFK(kit, pkVal);
        }
        else {
            kit.whereIn(_FKField, pkVal);
        }
        //子级加载事件唤起
        if (this._onFilterChildScoped != null) {
            this._onFilterChildScoped(kit);
        }
        return kit.query();
    }
    /// <summary>
    /// 根据主键查询
    /// </summary>
    /// <param name="kit"></param>
    /// <param name="pkVal"></param>
    /// <returns></returns>
    protected virtual DataTable LoadByPKValues(SQLBuilder kit, IEnumerable pkVal)
    {
        kit.clear();

        if (_onQueryBase != null)
        {
            _onQueryBase(kit);
        }

        kit.whereIn(_PKField, pkVal);
        
        return kit.query();
    }
    /// <summary>
    /// 加载根节点
    /// </summary>
    /// <param name="kit"></param>
    /// <returns></returns>
    protected virtual DataTable LoadRootValues(SQLBuilder kit)
    {
        kit.clear();
        if (_onQueryBase != null)
        {
            _onQueryBase(kit);
        }
        //无效的字符串主键
        if (_onFilterRootEmpty!=null &&(  RootPKVal ==null|| (RootPKVal is string str && string.IsNullOrWhiteSpace(str)) )) {
            _onFilterRootEmpty(kit);
        }
        else if (this.RootPKVal != null)
        {
            doWherePKValues(kit, this.RootPKVal);
        }
        else if (_onFilterRootEmpty != null)
        {
            _onFilterRootEmpty(kit);
        }
        else
        {
            kit.whereIsNull(_FKField);
        }
        return kit.query();
    }

    protected virtual void doWherePKValues(SQLBuilder kit, K val) {
        if (this._whenLoadRootByPK != null)
        {
            this._whenLoadRootByPK(kit, val);
        }
        else if (!string.IsNullOrWhiteSpace(_PKField)) {
            kit.where(_FKField, val);
        }
        else
        {
            throw new NotSupportedException("查询失败！未定义主键、主键过滤方式！");
        }
    }
}
