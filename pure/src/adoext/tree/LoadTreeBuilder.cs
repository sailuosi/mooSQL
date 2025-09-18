// 基础功能说明：

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data;


namespace mooSQL.data;
/// <summary>
/// 树形结构查询构建器
/// </summary>
public class LoadTreeBuilder<K>
{
    /// <summary>
    /// 数据库实例对象
    /// </summary>
    public DBInstance DBLive { get; set; }

    private string _PKField { get; set; }
    private string _FKField { get; set; }
    /// <summary>
    /// 根节点的主键值，默认为null。
    /// </summary>
    public K RootPKVal { get;private set; }
    /// <summary>
    /// 基础的查询
    /// </summary>
    private Action<SQLBuilder> _onQueryBase = null;
    /// <summary>
    /// 过滤根节点
    /// </summary>
    private Action<SQLBuilder> _onFilterRoot = null;
    /// <summary>
    /// 过滤子节点
    /// </summary>
    private Action<SQLBuilder, IEnumerable> _onFilterChild = null;


    private int _deep = 1;

    /// <summary>
    /// 构造函数，传入数据库实例对象。
    /// </summary>
    /// <param name="DB"></param>
    public LoadTreeBuilder(DBInstance DB)
    {
        this.DBLive = DB;
    }
    /// <summary>
    /// 设置主键和外键字段
    /// </summary>
    /// <param name="PKName"></param>
    /// <param name="FKName"></param>
    /// <returns></returns>
    public LoadTreeBuilder<K> selectPKFK(string PKName, string FKName) { 
        this._PKField = PKName;
        this._FKField = FKName;
        return this;
    }
    /// <summary>
    /// 设置根节点的主键值，默认为null。
    /// </summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public LoadTreeBuilder<K> setPK(K pk)
    {
        this.RootPKVal = pk;
        return this;
    }
    /// <summary>
    /// 设置基础查询
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public LoadTreeBuilder<K> OnQueryBase(Action<SQLBuilder> onQuery)
    {
        this._onQueryBase = onQuery;
        return this;
    }
    /// <summary>
    /// 设置过滤根节点查询条件
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public LoadTreeBuilder<K> OnFilterRoot(Action<SQLBuilder> onQuery)
    {
        this._onFilterRoot = onQuery;
        return this;
    }
    /// <summary>
    /// 设置过滤子节点查询条件
    /// </summary>
    /// <param name="onQuery"></param>
    /// <returns></returns>
    public LoadTreeBuilder<K> OnFilterChild(Action<SQLBuilder, IEnumerable> onQuery)
    {
        this._onFilterChild = onQuery;
        return this;
    }
    /// <summary>
    /// 设置一次性查询深度，默认为1层。
    /// </summary>
    /// <param name="deep"></param>
    /// <returns></returns>
    public LoadTreeBuilder<K> setDeep(int? deep)
    {
        if (deep == null)
            return this;
        this._deep = deep.Value;
        return this;
    }
    /// <summary>
    /// 加载顶层节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="rowToTar"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public TreeListOutput<T> GetTreeList<T>(Func<DataRow,T> rowToTar)
    {
        //先获取关联定义，然后递归查询所有下级


        if (string.IsNullOrWhiteSpace(_PKField) || string.IsNullOrWhiteSpace(_FKField))
        {
            throw new NotSupportedException("定义的主键字段、外键字段为空！");
        }
        var fk = _FKField;


        var lv = 0;
        
        var res = new TreeListOutput<T>();
        var pk = _PKField;
        var topNodes = new List<TreeNodeOutput<T>>();

        var kit = DBLive.useSQL();

        var rootDt = this.LoadTopValues(kit);
        if (rootDt != null) {
            //没有根节点时，返回值需要包含顶层节点
            foreach (DataRow row in rootDt.Rows) {
                var pkVal = getPKVal(row, _PKField);
                var fkR = getPKVal(row, _FKField);
                var nodeVal = new TreeNodeOutput<T>()
                {
                    Record = rowToTar(row),
                    Children = new List<TreeNodeOutput<T>>(),
                    Level = lv + 1,
                    PKValue = pkVal,
                    FKValue = fkR
                };
                topNodes.Add(nodeVal);
            }
        }

        var preLvNodes = topNodes;
        var total = 0;
        //为防止自循环和无限循环，每次取下级时，如果其主键在之前取过的父级中，则忽略它。
        var paPks = loadPKValue(rootDt);
        var fkVals = paPks;
        while (fkVals.Count > 0)
        {
            var lvlist = this.LoadChildValues(kit, fkVals);
            if (lvlist == null || lvlist.Rows.Count == 0)
                break;

            //获取主键值
            var lvNodes = new List<TreeNodeOutput<T>>();
            var lvPks = new List<K>();
            foreach (DataRow lvitem in lvlist.Rows)
            {
                //获取主键值

                var pkVal = getPKVal(lvitem,_PKField);
                if (pkVal != null && !paPks.Contains(pkVal))
                {
                    lvPks.Add(pkVal);
                    paPks.Add(pkVal);
                }
                //外键值
                var fkR = getPKVal(lvitem, _FKField);
                var nodeVal = new TreeNodeOutput<T>()
                {
                    Record = rowToTar(lvitem),
                    Children = new List<TreeNodeOutput<T>>(),
                    Level = lv + 1,
                    PKValue = pkVal,
                    FKValue = fkR
                };
                lvNodes.Add(nodeVal);
                bool isTop = true;
                foreach (var node in preLvNodes)
                {

                    if (node.PKValue.ToString() == fkR.ToString())
                    {
                        node.Children.Add(nodeVal);
                        total++;
                        isTop = false;
                        break;
                    }
                }
                if (isTop && fkR.ToString() == this.RootPKVal.ToString())
                {
                    topNodes.Add(nodeVal);
                    total++;
                }
            }
            fkVals = lvPks;
            lv++;
            if (lv >= this._deep) {
                break;
            }
            //初始化下一层的节点集合
            preLvNodes = lvNodes;
            if (lv > 50) throw new NotSupportedException("递归层级过多，可能存在无限循环！最大支持50层！");
        }
        res.Nodes = topNodes;
        return res;
    }

    private K getPKVal(DataRow row,string FieldName) {
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

    private List<K> loadPKValue(DataTable dt) { 
        var res=new List<K>();
        foreach (DataRow row in dt.Rows) {
            var v = row[_PKField] ;
            if (v is K) { 
                res.Add((K)v);
                continue;
            }
            var v2= (K)(object)v;
            if (v2 != null ) {
                res.Add(v2);
            }
            
        }
        return res;
    }

    private List<K> loadFKValue(DataTable dt)
    {
        var res = new List<K>();
        foreach (DataRow row in dt.Rows)
        {
            var v = row[_FKField];
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

    private DataTable LoadChildValues(SQLBuilder kit, IEnumerable pkVal)
    {
        kit.clear();

        if (_onQueryBase != null)
        {
            _onQueryBase(kit);
        }
        if (_onFilterChild != null)
        {
            _onFilterChild(kit, pkVal);
        }
        else {
            kit.whereIn(_FKField, pkVal);
        }
        return kit.query();
    }
    private DataTable LoadTopValues(SQLBuilder kit)
    {
        kit.clear();
        if (_onQueryBase != null)
        {
            _onQueryBase(kit);
        }
        if (this.RootPKVal != null)
        {
            kit.where(_FKField, this.RootPKVal);
        }
        else if (_onFilterRoot != null)
        {
            _onFilterRoot(kit);
        }
        else
        {
            kit.whereIsNull(_FKField);
        }
        return kit.query();
    }
}
