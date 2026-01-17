
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 行转列时的转置配置
    /// </summary>
    public class PivotItem
    {
        public PivotItem() { 
        
        }

        public PivotItem(string aggregation,string field, List<string> values,string asName )
        {
            this.aggregation = aggregation;
            this.headField = field;
            this.headValues = values;
            this.asName = asName;
        }
        /// <summary>
        /// 聚合部分
        /// </summary>
        public string aggregation;

        /// <summary>
        /// 聚合后作为标题的字段
        /// </summary>
        public string headField;

        /// <summary>
        /// 作为标题的字段的值
        /// </summary>
        public List<string> headValues ;
        /// <summary>
        /// 转置后的别名
        /// </summary>
        public string asName;
    }

    /// <summary>
    /// 多列转行的配置
    /// </summary>
    public class UnpivotItem {

        public UnpivotItem() {
        
        }
        public UnpivotItem(string valueName, string fieldName, List<string> fields, string asName)
        {
            this.valueName = valueName;
            this.fieldName = fieldName;
            this.fields = fields;
            this.asName = asName;
        }
        /// <summary>
        /// 列的值作为的字段名
        /// </summary>
        public string valueName;

        /// <summary>
        /// 列的标题作为的字段名
        /// </summary>
        public string fieldName;

        /// <summary>
        /// 要转置的列的范围
        /// </summary>
        public List<string> fields;

        /// <summary>
        /// 转置后的别名
        /// </summary>
        public string asName;
    }
}
