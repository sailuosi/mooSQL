// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 承载用户信息，只需要权限判断需要的数据。允许业务侧扩展或继承本类进行增强
    /// </summary>
    public partial class AuthUser:Samable
    {
        /// <summary>
        /// 承载从数据库获取权限对象的功能，一般每个系统都不一样，所以是方言；
        /// </summary>
        public AuthDialect dialect;
        //基础ID部分
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name;
        /// <summary>
        /// 账号
        /// </summary>
        public string Acount;
        public string Id;
        public string UserOID;

        public string divisionOID;

        public string orgOID;

        public string postNo;
        /// <summary>
        /// 代表当前正在作为权限过滤所使用的角色主键
        /// </summary>
        public List<string> dutyOIDs= new List<string>();
        //扩展属性

        public Dictionary<string, string> attrs;

        //所属身份部分

        private AuthOrg _manorg;
        private AuthOrg _mandiv;
        private AuthPost _manpost;

        public AuthOrg HisDivision
        {
            get {
                if (_mandiv == null) {
                    _mandiv = dialect.loadManDiv(this);
                }
                return _mandiv;
            }
        }

        public AuthOrg HisOrg
        {
            get {
                if (_manorg == null)
                {
                    _manorg = dialect.loadManOrg(this);
                }
                return _manorg;
            }
        }

        public AuthPost HisPost
        {
            get {
                if (_manpost == null)
                {
                    _manpost = dialect.loadManPost(this);
                }
                return _manpost;
            }
        }


        public bool isSame(Samable a)
        {
            var t = a as AuthUser;
            if (t.Acount == Acount || Id == t.Id || UserOID == t.UserOID)
            {
                return true;
            }
            return false;
        }
    }
}

