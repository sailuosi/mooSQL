// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    internal class UnionCollection
    {
        private bool _unionAll = false;
        private bool _unionWrap = true;
        /// <summary>
        /// 包裹 union的表别名
        /// </summary>
        private string _unionName = "tmpunioned";

        internal List<SqlGoup> united;

        private List<string> orderbyPart = new List<string>();

        internal int Count
        {
            get { return united.Count; }
        }
        /**
         * 分组模式下的最终执行器。
         */
        public SqlGoup unitedWraper;

        public UnionCollection()
        {
            united = new List<SqlGoup>();
        }

        /// <summary>
        /// 开始一个新的 SQL 分组。
        /// </summary>
        /// <returns></returns>
        private UnionCollection doUnion(SQLBuilder root)
        {
            if (this.unitedWraper == null)
            {
                unitedWraper = new SqlGoup("", "unitedWraper", root);
                unitedWraper.position = root.position;
                unitedWraper.ps = root.ps;
            }
            if (this.united.Count == 0)
            {
                //当第一次开始时，把当前的放入到union列表。并创建一个新的
                united.Add(root.current);
            }

            root.newGroup();
            united.Add(root.current);
            return this;
        }
        /// <summary>
        /// 设置是否使用 union all,以及union 外层是否需要自动用一层select包裹
        /// </summary>
        /// <param name="isUnionAll"></param>
        /// <param name="wrapSelect"></param>
        /// <returns></returns>
        internal UnionCollection union(SQLBuilder root, bool isUnionAll = false, bool wrapSelect = true, string wrapAsName = "tmpunioned")
        {
            this._unionAll = isUnionAll;
            this._unionWrap = wrapSelect;
            this._unionName = wrapAsName;
            this.doUnion(root);
            return this;
        }



        internal void orderby(string orderByPart)
        {
            if (string.IsNullOrWhiteSpace(orderByPart))
            {
                return;
            }
            orderbyPart.Add(orderByPart);

        }

        public void Clear()
        {
            this.united.Clear();
            _unionAll = false;
            _unionWrap = true;
            _unionName = "tmpunioned";
        }

        private string gengeUnionPartContent()
        {
            //union模式下的查询，需要最后执行分页和order
            StringBuilder readSQL = new StringBuilder();
            bool isFirst = true;
            foreach (SqlGoup g in united)
            {
                if (!isFirst)
                {
                    readSQL.Append(_unionAll ? " UNION ALL " : " UNION ");
                }
                readSQL.Append(g.preConnector);
                readSQL.Append(" ");
                readSQL.Append(g.buildSelectNoPage(false));
                readSQL.Append(" ");
                isFirst = false;
            }
            return readSQL.ToString();
            //当前的逻辑，每次union时，会把当前的也纳入union队列中。不需要再补偿当前。
            //readSQL.Append(_unionAll ? " UNION ALL " : " UNION ");
            //readSQL.Append(current.preConnector);
            //readSQL.Append(" ");
            //readSQL.Append(current.buildSelectNoPage(false));
            //readSQL.Append(" ");
            //unitedExecutor.clearFrom();
            //unitedExecutor.from("( " + readSQL.ToString() + " ) as " + _unionName);
        }

        public string build()
        {
            var sql = gengeUnionPartContent();
            if (_unionWrap)
            {
                unitedWraper.from("( " + sql + " ) as " + _unionName);
                return unitedWraper.buildSelect();
            }
            return sql;
        }
        public string buildCount()
        {
            var sql = gengeUnionPartContent();

            unitedWraper.from("( " + sql + " ) as " + _unionName);
            return unitedWraper.buildCountSQL();

        }

    }
}