using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
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

        public string RootAs {
            get { return srcAsName; }
        }
        public string NextAs
        {
            get { return destAsName; }
        }
        public string CTEJoinAs
        {
            get { return rootJoinAs; }
        }



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


        public RecurCTEBuilder joinOn(string joinOnPart) { 
            this.joinOnStr=joinOnPart;
            return this;
        }

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
        public RecurCTEBuilder select(string rootField,string nextField,string asName)
        {
            var fie = new RecurFieldItem();
            fie.rootField = rootField;
            fie.nextField = nextField;
            fie.asName = asName;
            this.xFeilds.Add(fie);
            return this;
        }
        public RecurCTEBuilder selectDeep(string field)
        {
            this.deepFieldName=field;
            return this;
        }

        public RecurCTEBuilder useBuilder(SQLBuilder builder) { 
            this.builder = builder;

            return this;
        }


        public RecurCTEBuilder fromNext(string fromNextPart, string selfAsName = "np") { 
            this.nextFromString = fromNextPart;
            this.selfAsName = selfAsName;
            return this;
        }

        public RecurCTEBuilder whereRoot(Action<SQLBuilder,RecurCTEBuilder> whereBuilder) { 
            this.onBuildSrcWhere = whereBuilder;
            return this;
        }

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

    public class RecurFieldItem {
        public string rootField;
        public string nextField;
        public string asName;
    }
}
