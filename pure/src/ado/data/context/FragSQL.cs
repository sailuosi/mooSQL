


using System.Collections.Generic;

namespace mooSQL.data.builder
{

    /// <summary>
    /// SQL语句要素 表示类
    /// </summary>
    public class FragSQL
    {
        /// <summary>
        /// 是否distinct
        /// </summary>
        public bool distincted = false;
        /// <summary>
        /// 取前多少条
        /// </summary>
        public int toped = -1;
        /// <summary>
        /// select的字段列表
        /// </summary>
        public string selectInner = "";
        /// <summary>
        /// from 语句的子内容
        /// </summary>
        public string fromInner = "";
        /// <summary>
        /// where 条件的子内容
        /// </summary>
        public string whereInner = "";
        /// <summary>
        /// group by 的子内容
        /// </summary>
        public string groupByInner = "";
        /// <summary>
        /// having 的内容
        /// </summary>
        public string havingInner = "";
        /// <summary>
        /// order by的子内容
        /// </summary>
        public string orderbyInner = "";
        /// <summary>
        /// 分页时的页面大小
        /// </summary>
        public int pageSize = -1;
        /// <summary>
        /// 页码
        /// </summary>
        public int pageNum = -1;
        /// <summary>
        /// 是否有行号字段
        /// </summary>
        public bool hasRowNumber;
        /// <summary>
        /// 设置行号字段时的行号名称。
        /// </summary>
        public string rowNumberFieldName;
        /// <summary>
        /// 行号字段的排序依据
        /// </summary>
        public string rowNumberOrderBy;


        // insert 语句
        /// <summary>
        /// insertInto 的内容
        /// </summary>
        public string insertInto = "";
        /// <summary>
        /// 要插入的字段，不包含括号
        /// </summary>
        public string insertCols = "";
        /// <summary>
        /// 一组插入值，不包含括号
        /// </summary>
        public string insertValue = "";
        /// <summary>
        /// 多组插入值，不包含括号
        /// </summary>
        public List<string> insertValues;

        // update 语句

        /// <summary>
        /// update 的内容
        /// </summary>
        public string updateTo="";
        /// <summary>
        /// update 语句的 set 内容
        /// </summary>
        //public string setInner = "";


        // delete 语句

        public string deleteTar = "";

        /// <summary>
        /// 实际表名 非别名
        /// </summary>
        public string tableName;

        public string dataBaseName;


        //merge into 语句碎片
        /// <summary>
        /// 要合并的目标表
        /// </summary>
        public string mergeInto;
        /// <summary>
        /// merge into 语句的on 部分
        /// </summary>
        public string mergeOn;
        /// <summary>
        /// merge using 后包裹的
        /// </summary>
        public string mergeAsName;
        /// <summary>
        /// 是否是从子查询中进行合并
        /// </summary>
        public bool mergeFromCTE=false;

        public bool mergeDeletable=false;
        /// <summary>
        ///     转置的配置
        /// </summary>
        public List<PivotItem> pivots;

        public List<UnpivotItem> unpivots;
        /// <summary>
        /// update语句的set 部分配置项集合
        /// </summary>
        public List<FragSetPart> setPart { get; set; }
    }
    /// <summary>
    /// update语句的set 部分配置项
    /// </summary>
    public class FragSetPart {
        public string field;

        public string value;
    }

    public class FragMergeInto {
        /// <summary>
        /// 目标表
        /// </summary>
        public string intoTable { get; set; }
        /// <summary>
        /// 目标表别名
        /// </summary>
        public string intoAlias { get; set; }
        /// <summary>
        /// 源表
        /// </summary>
        public string usingTable { get; set; }

        /// <summary>
        /// 源表别名
        /// </summary>
        public string usingAlias { get; set; }
        /// <summary>
        /// 桥接条件
        /// </summary>
        public string onPart { get; set; }


        public List<FragMergeWhen> mergeWhens;
    }

    /// <summary>
    /// merge into 语句的when 部分配置项
    /// </summary>
    public class FragMergeWhen {
        public bool matched;
        public string whenWhere;
        public MergeAction action;

        public string fieldInner;
        public string valueInner;
        public List<FragSetPart> setInner;
    }
}