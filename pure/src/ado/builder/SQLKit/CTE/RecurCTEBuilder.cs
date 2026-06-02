using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 类型 RecurCTEBuilder。
    /// </summary>
    public class RecurCTEBuilder
    {
        private string withAsName;

        private string srcTable;
        private string srcAsName= "src";
        /// <summary>
        /// 如果定义了这里，将忽略 destTable selfAsName joinOnStr等参数定义
        /// </summary>
        private string nextFromString;
        private string destTable;
        private string destAsName="tar";

        private string selfAsName="np";

        private string joinOnStr;

        private string union = " UNION ALL ";

        private string rootJoinAs = "tmpro";
        /// <summary>
        /// 如果设置，则有深度字段
        /// </summary>
        private string deepFieldName = "";


        private HashSet<string> fields = new HashSet<string>();
        private List<RecurFieldItem> xFeilds = new List<RecurFieldItem>();

        private SQLBuilder builder;

        private Action<SQLBuilder, RecurCTEBuilder> onBuildSrcWhere;

        private Action<SQLBuilder, RecurCTEBuilder> onBuildDstWhere;

        /// <summary>
        /// 属性 RootAs（string）。
        /// </summary>
        public string RootAs {
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
        public RecurCTEBuilder setWithAsName(string withAsName) { 
            this.withAsName = withAsName;
            return this;
        }
        /// <summary>
        /// 默认递归别名 tar  CTE别名:np
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="srcAsName"></param>
        /// <returns></returns>
        public RecurCTEBuilder fromRoot(string tableName, string srcAsName = "") { 
            this.srcTable = tableName;
            if (!string.IsNullOrWhiteSpace(srcAsName)) {
                this.srcAsName = srcAsName;
            }
            
            if (string.IsNullOrWhiteSpace(destTable)) { 
                destTable = tableName;
            }
            return this;
        }

        /// <summary>
        /// fromNext 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder fromNext(string tableName, string asName = "", string selfAsName = "") { 
            this.destTable = tableName;
            if (!string.IsNullOrWhiteSpace(asName)) { 
                destAsName = asName;
            }
            if (!string.IsNullOrWhiteSpace(selfAsName)) { 
                this.selfAsName = selfAsName;
            }
            return this;
        }


        /// <summary>
        /// joinOn 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder joinOn(string joinOnPart) { 
            this.joinOnStr=joinOnPart;
            return this;
        }

        /// <summary>
        /// joinOn 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder joinOn(string rootField,string nextField)
        {
            this.joinOnStr = rootJoinAs+"."+rootField+"="+destAsName+"."+nextField;
            this.fields.Add(rootField);
            this.fields.Add(nextField);
            return this;
        }
        /// <summary>
        /// 公用字段，不需要带别名
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public RecurCTEBuilder select(string field) { 
            this.fields.Add(field);
            return this;
        }
        /// <summary>
        /// select 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder select(string rootField,string nextField,string asName)
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
            this.deepFieldName=field;
            return this;
        }

        /// <summary>
        /// useBuilder 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder useBuilder(SQLBuilder builder) { 
            this.builder = builder;

            return this;
        }


        /// <summary>
        /// fromNext 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder fromNext(string fromNextPart, string selfAsName = "np") { 
            this.nextFromString = fromNextPart;
            this.selfAsName = selfAsName;
            return this;
        }

        /// <summary>
        /// whereRoot 方法（返回 RecurCTEBuilder）。
        /// </summary>
        public RecurCTEBuilder whereRoot(Action<SQLBuilder,RecurCTEBuilder> whereBuilder) { 
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

        private List<string> loadFeilds() {
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
        /// apply 方法（返回 SQLBuilder）。
        /// </summary>
        public SQLBuilder apply() {
            builder.withSelect(withAsName, (w) =>
            {
                var fies= this.loadFeilds();
                //先构建根查询
                //select部分
                foreach (var f in fies) {
                    w.select(srcAsName + "." + f);
                }
                //不同列部分
                foreach (var fi in xFeilds) {
                    w.select( fi.rootField + " as " + fi.asName);
                }
                //层深部分
                if (!string.IsNullOrWhiteSpace(deepFieldName)) {
                    w.select("0 as " + deepFieldName);
                }
                //from部分
                w.from(srcTable + " as " + srcAsName);
                if (onBuildSrcWhere != null) {
                    onBuildSrcWhere(w,this);
                }

                //开始union

                w.unionAll(false);
                //select部分
                foreach (var f in fies)
                {
                    w.select(destAsName + "." + f);
                }
                //不同列部分
                foreach (var fi in xFeilds)
                {
                    w.select(fi.nextField + " as " + fi.asName);
                }
                //层深部分
                if (!string.IsNullOrWhiteSpace(deepFieldName))
                {
                    w.select(rootJoinAs+"."+ deepFieldName+"+ 1 as "+deepFieldName);
                }
                //from部分
                w.from(destTable + " as " + destAsName+" join " + withAsName +" as "+rootJoinAs +" on "+joinOnStr);

                if (onBuildDstWhere != null)
                {
                    onBuildDstWhere(w,this);
                }


            });
            return builder;
        }

    }

    /// <summary>
    /// 类型 RecurFieldItem。
    /// </summary>
    public class RecurFieldItem {
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