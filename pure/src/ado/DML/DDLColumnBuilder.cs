using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.builder;

namespace mooSQL.data
{
    /// <summary>
    /// 业务侧使用的快捷设置表字段的构造器
    /// </summary>
    public class DDLColumnBuilder
    {
        /// <summary>
        /// 正在配置的字段
        /// </summary>
        public DDLField field;
        /// <summary>
        /// 来源编制器
        /// </summary>
        public DDLBuilder fromBuilder;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        public DDLColumnBuilder(DDLField field, DDLBuilder from)
        {
            this.field = field;
            this.fromBuilder = from;
        }
        /// <summary>
        /// 设置为时间字段
        /// </summary>
        /// <returns></returns>
        public DDLColumnBuilder useDateTime() {
            var t = fromBuilder.DBLive.dialect.expression.getDateTimeColumnType(-1);
            if (!string.IsNullOrWhiteSpace(t)) { 
                field.TextType = t;
            }
            return this;
        }
        /// <summary>
        /// 设置为字符串字段
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public DDLColumnBuilder useString(int length) {
            var t = fromBuilder.DBLive.dialect.expression.getStringColumnType(length);
            if (!string.IsNullOrWhiteSpace(t))
            {
                field.TextType = t;
            }
            return this;         
        }
        /// <summary>
        /// 设置为整型字段
        /// </summary>
        /// <returns></returns>
        public DDLColumnBuilder useInt(int length=-1) {
            var t = fromBuilder.DBLive.dialect.expression.getIntColumnType(length);
            if (!string.IsNullOrWhiteSpace(t))
            {
                field.TextType = t;
            }
            return this;
        }
        /// <summary>
        /// 设置为小数数值字段
        /// </summary>
        /// <param name="precision"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public DDLColumnBuilder useNumber(int precision,int scale) {
            var t = fromBuilder.DBLive.dialect.expression.getNumberColumnType(precision,scale);
            if (!string.IsNullOrWhiteSpace(t))
            {
                field.TextType = t;
            }
            return this;
        }
        /// <summary>
        /// 设置为布尔字段
        /// </summary>
        /// <returns></returns>
        public DDLColumnBuilder useBool() {
            var t = fromBuilder.DBLive.dialect.expression.getBoolColumnType();
            if (!string.IsNullOrWhiteSpace(t))
            {
                field.TextType = t;
            }
            return this;
        }
        /// <summary>
        /// 设置为Guid字段
        /// </summary>
        /// <returns></returns>
        public DDLColumnBuilder useGUID() {
            var t = fromBuilder.DBLive.dialect.expression.getGuidColumnType();
            if (!string.IsNullOrWhiteSpace(t))
            {
                field.TextType = t;
            }
            return this;
        }
        /// <summary>
        /// 设置不为空
        /// </summary>
        /// <returns></returns>
        public DDLColumnBuilder notNull() { 
            field.Nullable = false;
            return this;
        }
        /// <summary>
        /// 配置默认值SQL
        /// </summary>
        /// <param name="valueSQL"></param>
        /// <returns></returns>
        public DDLColumnBuilder defaultVal(string valueSQL) { 
            field.DefaultValue = valueSQL;
            return this;
        }
        /// <summary>
        /// 设置中文名
        /// </summary>
        /// <param name="caption"></param>
        /// <returns></returns>
        public DDLColumnBuilder caption(string caption) { 
            field.Caption = caption;
            return this;
        }
        /// <summary>
        /// 直接结束
        /// </summary>
        /// <returns></returns>
        public DDLBuilder ok() {
            fromBuilder.set(field);
            return fromBuilder;
        }
        /// <summary>
        /// 设置为只添加字段用，并结束
        /// </summary>
        /// <returns></returns>
        public DDLBuilder add()
        {
            field.Mode = "add";
            fromBuilder.set(field);
            return fromBuilder;
        }
        /// <summary>
        /// 设置为只修改字段用，并结束
        /// </summary>
        /// <returns></returns>
        public DDLBuilder set()
        {
            field.Mode = "change";
            fromBuilder.set(field);
            return fromBuilder;
        }
    }
}
