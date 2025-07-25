// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 权限下的部门或单位对象
    /// </summary>
    public class AuthOrg : Childable
    {
        /// <summary>
        /// 业务主键
        /// </summary>
        public string HROID;
        /// <summary>
        /// 编号
        /// </summary>
        public string ID;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 识别ID
        /// </summary>
        public string OrgNo;
        /// <summary>
        /// 人力层次码
        /// </summary>
        public string HRCode;
        /// <summary>
        /// 纯层次码
        /// </summary>
        public string ClassCode;
        /// <summary>
        /// 是否是某个的子级
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public bool isChildOf(Childable a)
        {
            var t = a as AuthOrg;
            if (HRCode.StartsWith(t.HRCode))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 是否相同
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public bool isSame(Childable a)
        {
            var t = a as AuthOrg;
            if (t.HRCode == HRCode || OrgNo == t.OrgNo || HROID == t.HROID)
            {
                return true;
            }
            return false;
        }
    }
}