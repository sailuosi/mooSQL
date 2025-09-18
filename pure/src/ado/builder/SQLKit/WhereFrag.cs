using mooSQL.auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 盒子，一个盒子可以只是一个盒子，也可以在其中放小盒子，或者任意其他东西。
    /// </summary>
    public abstract class Boxable {
        /// <summary>
        /// 是否为盒子
        /// </summary>
        public bool isBox;
        /// <summary>
        /// 是否顶级
        /// </summary>
        public bool isTop;
        /// <summary>
        /// 否定盒子。会在最终的执行前增加not
        /// </summary>
        public bool isNot;
        /// <summary>
        /// 根
        /// </summary>
        public SQLBuilder root;
        /// <summary>
        /// 父盒子
        /// </summary>
        public Boxable parent;
        /// <summary>
        /// 内容元素
        /// </summary>
        public List<Boxable> children;
        /// <summary>
        /// 连接器
        /// </summary>
        public string Connector = "AND";

        public virtual void Add(Boxable frag)
        {
            frag.Connector = Connector;
            children.Add(frag);
        }

        public abstract string ToSQL(string fmt="");
    }
    /// <summary>
    /// where条件的括号分组
    /// </summary>
    public class WhereBracket : Boxable
    {
        public WhereBracket() { 
            this.isBox = true;
            this.children = new List<Boxable>();
        }

        public override string ToSQL(string fmt = "")
        {
            //当盒子为空盒子时，返回空字符；
            if (children.Count == 0) return "";

            var sqls = new List<string>();
            foreach (var child in children)
            {
                var sql=child.ToSQL();
                if (!string.IsNullOrEmpty(sql)) { 
                    sqls.Add(sql);
                }
            }

            if (sqls.Count == 0) return "";
            //目前实际上会忽略掉 whereFrag的 connector设置。

            string prefixNot = "";
            if (isNot) {
                prefixNot = " NOT ";
            }

            if(sqls.Count==1) return prefixNot+sqls[0];

            return prefixNot+" ( " +string.Join(" "+Connector+" ", sqls) +" ) ";

        }
    }

    /// <summary>
    /// where 条件项碎片
    /// 增加子条件。
    /// </summary>
    public class WhereFrag: Boxable
    {

        /// <summary>
        /// 字段
        /// </summary>
        public string key;
        /// <summary>
        /// 单个映射值，用于update 或者单个 insert
        /// </summary>
        public object value;
        /// <summary>
        /// 是否为手动拼接项，手动拼接时，where条件前方连接不再拼接连接符。false--需要拼接，true--不需要拼接
        /// </summary>
        public bool pined = false;
        /// <summary>
        /// 标识 下一个条件拼接时，是否需要使用拼接符。 false--需要拼接，true--不需要拼接
        /// </summary>
        public bool nextPined = false;
        /// <summary>
        /// 按照行号映射的多个值，用于批量插入。
        /// </summary>
        public Dictionary<int, object> values = new Dictionary<int, object>();
        /// <summary>
        /// 是否参数化
        /// </summary>
        public bool paramed = true;
        /// <summary>
        /// 是否左侧也参数化
        /// </summary>
        public bool leftParamed = false;
        /// <summary>
        /// 条件操作符
        /// </summary>
        public string op = "=";//
        /// <summary>
        /// 前置连接符
        /// </summary>
        public string connector = "AND";//
        /**
         * 参数化的键值
         */
        public string paramKey = "";

        public string leftParamKey = "";

        public object leftValue;
        /**
         * 可用于构建更新语句
         */
        public bool updatable = true;
        /**
         * 可用于构建插入语句
         */
        public bool insetable = true;

        public WhereFrag()
        {
            this.isBox = false;
        }
        public WhereFrag(string key)
        {
            this.key = key;
        }
        public WhereFrag(string key, object val)
        {
            this.key = key;
            this.value = val;
        }
        public WhereFrag(string key, object val, bool paramed)
        {
            this.key = key;
            this.value = val;
            this.paramed = paramed;
        }
        public WhereFrag(string key, object val, bool paramed, bool updatable, bool insetable)
        {
            this.key = key;
            this.value = val;
            this.paramed = paramed;
            this.updatable = updatable;
            this.insetable = insetable;
        }

        public void setValue(object value, int index)
        {
            //if
        }
        /// <summary>
        /// 设置where 条件直接的连接符  AND OR
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public WhereFrag setConnect(string val)
        {
            this.connector = val;
            return this;
        }
        /// <summary>
        /// 转义配置为SQL
        /// </summary>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public override string ToSQL(string fmt = "")
        {
            var conditon = "";
            if (value == null && op == (""))
            {
                conditon += key;

                return conditon;
            }

            //增加左侧也参数化的支持
            if (leftParamed)
            {
                string prefix = leftParamKey;
                conditon += root.expression.paraPrefix + prefix;
                if (fmt.Contains(";noPara;") == false)
                {
                    root.ps.AddByPrefix(prefix, leftValue, root.expression.paraPrefix);
                }
            }
            else {
                conditon += key;
            }

                
            if (!string.IsNullOrEmpty(op))
            {
                conditon += " " + op + " ";
            }

            if (paramed)
            {
                string prefix = paramKey;
                conditon += root.expression.paraPrefix + prefix;
                if (fmt.Contains(";noPara;") == false) {
                    root.ps.AddByPrefix(prefix, value, root.expression.paraPrefix);
                }
                
            }
            else
            {
                conditon += value.ToString();
            }
            return conditon;
        }
    }
}
