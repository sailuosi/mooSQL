using mooSQL.data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// 外键定义注解
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface,
        AllowMultiple = true, Inherited = true)]
    public class SooFKAttribute : MappingAttribute
    {

        public override string GetObjectID()
        {
            return GetHashCode().ToString();
        }
        /// <summary>
        /// 外键的表
        /// </summary>
        /// <param name="thatTable"></param>
        /// <param name="thatField"></param>
        public SooFKAttribute(string thatTable, string thatField)
        {
            this.thatTable = thatTable;
            this.thatField = thatField;
        }

        /// <summary>
        /// 外键对象的表名称
        /// </summary>
        public string thatTable { get; set; }
        /// <summary>
        /// 外键字段
        /// </summary>
        public string thatField { get; set; }
    }
}
