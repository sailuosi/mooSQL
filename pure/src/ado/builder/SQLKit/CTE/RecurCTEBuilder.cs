using System;
using System.Collections.Generic;
using System.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// 递归 CTE 编排器。绑在门面 <see cref="SQLBuilder"/> 上；
    /// <see cref="apply"/> 通过门面 <c>withSelect</c> 入队子步骤并返回门面。
    /// </summary>
    public class RecurCTEBuilder
    {
        private string withAsName;

        private string srcTable;
        private string srcAsName = "src";
        /// <summary>
        /// 如果定义了这里，将忽略 destTable selfAsName joinOnStr等参数定义
        /// </summary>
        private string nextFromString;
        private string destTable;
        private string destAsName = "tar";

        private string selfAsName = "np";

        private string joinOnStr;

        private string rootJoinAs = "tmpro";
        /// <summary>
        /// 如果设置，则有深度字段
        /// </summary>
        private string deepFieldName = "";

        private HashSet<string> fields = new HashSet<string>();
        private List<RecurFieldItem> xFeilds = new List<RecurFieldItem>();

        private SQLBuilder facade;

        private Action<SQLBuilder, RecurCTEBuilder> onBuildSrcWhere;

        private Action<SQLBuilder, RecurCTEBuilder> onBuildDstWhere;

        /// <summary>
        /// 属性 RootAs（string）。
        /// </summary>
        public string RootAs
        {
            get { return srcAsName; }
        }
        /// <summary>
        /// 属性 NextAs（string）。
        /// </summary>
        public string NextAs
        {
            get { return destAsName; }
        }
        /// <summary>
        /// 属性 CTEJoinAs（string）。
        /// </summary>
        public string CTEJoinAs
        {
            get { return rootJoinAs; }
        }

        /// <summary>
        /// setWithAsName 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder setWithAsName(string withAsName)
        {
            this.withAsName = withAsName;
            return this;
        }
        /// <summary>
        /// 默认递归别名 tar  CTE别名:np
        /// </summary>
        public RecurCTEBuilder fromRoot(string tableName, string srcAsName = "")
        {
            this.srcTable = tableName;
            if (!string.IsNullOrWhiteSpace(srcAsName))
            {
                this.srcAsName = srcAsName;
            }

            if (string.IsNullOrWhiteSpace(destTable))
            {
                destTable = tableName;
            }
            return this;
        }

        /// <summary>
        /// fromNext 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder fromNext(string tableName, string asName = "", string selfAsName = "")
        {
            this.destTable = tableName;
            if (!string.IsNullOrWhiteSpace(asName))
            {
                destAsName = asName;
            }
            if (!string.IsNullOrWhiteSpace(selfAsName))
            {
                this.selfAsName = selfAsName;
            }
            return this;
        }

        /// <summary>
        /// joinOn 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder joinOn(string joinOnPart)
        {
            this.joinOnStr = joinOnPart;
            return this;
        }

        /// <summary>
        /// joinOn 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder joinOn(string rootField, string nextField)
        {
            this.joinOnStr = rootJoinAs + "." + rootField + "=" + destAsName + "." + nextField;
            this.fields.Add(rootField);
            this.fields.Add(nextField);
            return this;
        }
        /// <summary>
        /// 公用字段，不需要带别名
        /// </summary>
        public RecurCTEBuilder select(string field)
        {
            this.fields.Add(field);
            return this;
        }
        /// <summary>
        /// select 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder select(string rootField, string nextField, string asName)
        {
            var fie = new RecurFieldItem();
            fie.rootField = rootField;
            fie.nextField = nextField;
            fie.asName = asName;
            this.xFeilds.Add(fie);
            return this;
        }
        /// <summary>
        /// selectDeep 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder selectDeep(string field)
        {
            this.deepFieldName = field;
            return this;
        }

        /// <summary>
        /// 绑定编排门面。
        /// </summary>
        public RecurCTEBuilder useBuilder(SQLBuilder builder)
        {
            this.facade = builder;
            return this;
        }

        /// <summary>
        /// 内核路径：包装为物化门面后绑定（供 StepBuilder.withRecur* 使用）。
        /// </summary>
        public RecurCTEBuilder useBuilder(StepBuilder builder)
        {
            this.facade = SQLBuilder.Attach(builder, materializing: true);
            return this;
        }

        /// <summary>
        /// fromNext 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder fromNext(string fromNextPart, string selfAsName = "np")
        {
            this.nextFromString = fromNextPart;
            this.selfAsName = selfAsName;
            return this;
        }

        /// <summary>
        /// whereRoot 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder whereRoot(Action<SQLBuilder, RecurCTEBuilder> whereBuilder)
        {
            this.onBuildSrcWhere = whereBuilder;
            return this;
        }

        /// <summary>
        /// whereNext 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder whereNext(Action<SQLBuilder, RecurCTEBuilder> whereBuilder)
        {
            this.onBuildDstWhere = whereBuilder;
            return this;
        }

        private List<string> loadFeilds()
        {
            var cols = new HashSet<string>();
            foreach (var field in this.fields)
            {
                if (field.Contains("(") || !field.Contains(','))
                {
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        cols.Add(field);
                        continue;
                    }

                }
                var colArr = field.Split(',');
                foreach (var col in colArr)
                {
                    if (!string.IsNullOrWhiteSpace(col))
                    {
                        cols.Add(col);
                    }

                }
            }
            return cols.ToList();
        }

        /// <summary>
        /// 将递归 CTE 写入门面队列（via withSelect），并返回门面以便继续链式编排。
        /// </summary>
        public SQLBuilder apply()
        {
            if (facade == null)
                throw new InvalidOperationException("RecurCTEBuilder 未绑定 SQLBuilder，请先 withRecurTo / useBuilder。");

            facade.withSelect(withAsName, (w) =>
            {
                var fies = this.loadFeilds();
                //先构建根查询
                foreach (var f in fies)
                {
                    w.select(srcAsName + "." + f);
                }
                foreach (var fi in xFeilds)
                {
                    w.select(fi.rootField + " as " + fi.asName);
                }
                if (!string.IsNullOrWhiteSpace(deepFieldName))
                {
                    w.select("0 as " + deepFieldName);
                }
                w.from(srcTable + " as " + srcAsName);
                if (onBuildSrcWhere != null)
                {
                    onBuildSrcWhere(w, this);
                }

                w.unionAll(false);
                foreach (var f in fies)
                {
                    w.select(destAsName + "." + f);
                }
                foreach (var fi in xFeilds)
                {
                    w.select(fi.nextField + " as " + fi.asName);
                }
                if (!string.IsNullOrWhiteSpace(deepFieldName))
                {
                    w.select(rootJoinAs + "." + deepFieldName + "+ 1 as " + deepFieldName);
                }
                //from部分
                w.from(destTable + " as " + destAsName + " join " + withAsName + " as " + rootJoinAs + " on " + joinOnStr);

                if (onBuildDstWhere != null)
                {
                    onBuildDstWhere(w, this);
                }
            });
            return facade;
        }
    }

    /// <summary>
    /// 类型 RecurFieldItem。
    /// </summary>
    public class RecurFieldItem
    {
        /// <summary>
        /// 字段 rootField（string）。
        /// </summary>
        public string rootField;
        /// <summary>
        /// 字段 nextField（string）。
        /// </summary>
        public string nextField;
        /// <summary>
        /// 字段 asName（string）。
        /// </summary>
        public string asName;
    }
}
