// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 用于过滤权限的资源
    /// </summary>
    public class AuthGoods
    {
        /*
                 /// <summary>
        /// 需过滤角色的权限码，使用时，只依据符合该权限码的进行角色过滤。
        /// </summary>
        public string pemissionCode;
        /// <summary>
        /// 使用页面ID进行过滤
        /// </summary>
        public long menuId = 0;
        /// <summary>
        /// 页面编码
        /// </summary>
        public string menuCode = "";
         */
        /// <summary>
        /// 数值型主键
        /// </summary>
        public long id;
        /// <summary>
        /// 字符型主键OID，例如：用户ID、角色ID等
        /// </summary>
        public string oid;
        /// <summary>
        /// 字符型编码
        /// </summary>
        public string code;
        /// <summary>
        /// 名称
        /// </summary>
        public string name;
        /// <summary>
        /// 类型
        /// </summary>
        public string type;

        /// <summary>
        /// 分组
        /// </summary>
        public string group;

        public string Key {
            get {
                var k1 = "t" + type;
                if (!string.IsNullOrWhiteSpace(oid)) { 
                    return k1 + oid;
                }
                if (!string.IsNullOrWhiteSpace(code))
                {
                    return k1 + code;
                }
                return k1 + id;
            }
        }
    }
    /// <summary>
    /// 用于过滤的资源包
    /// </summary>
    public class AuthGoodsBag
    {

        public AuthGoodsBag()
        {
            this.Goods = new List<AuthGoods>();
        }

        public List<AuthGoods> Goods;


        public virtual bool addGoods(AuthGoods goods)
        {
            if (goods == null || string.IsNullOrWhiteSpace(goods.code))
            {
                return false;
            }
            foreach (AuthGoods good in this.Goods)
            {
                if (good.code == goods.code)
                {
                    return false;
                }
            }
            Goods.Add(goods);
            return true;
        }
    }



}