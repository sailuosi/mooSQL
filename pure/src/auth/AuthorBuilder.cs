// 基础功能说明：

using mooSQL.data;
using mooSQL.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.auth
{
    /// <summary>
    /// 数据权限构建体
    /// </summary>
    /// <typeparam name="RealDialect"></typeparam>
    public  abstract partial class AuthorBuilder<RealDialect> where RealDialect : AuthDialect
    {
        /// <summary>
        /// 工厂，所有个性化自定义分发的总入口；
        /// </summary>
        public AuthFactory<RealDialect> factory;

        private RealDialect _realDialect;

        public SQLBuilder kit;
        /// <summary>
        /// 方言对象
        /// </summary>
        public RealDialect dialect{
            get{
                if (_realDialect == null) {
                    _realDialect= factory.GetDialect();
                }
                return _realDialect;
            }
        }
        /// <summary>
        /// 词条集合
        /// </summary>
        internal WordBagDialect wordBag
        {
            get {
                return dialect.wordBag;
            }
        }

        //internal WordTranslator translator
        //{
        //    get {
        //        if (wordBag.wordTranslator == null) { 
        //            wordBag.wordTranslator = new WordTranslator();
        //        }
        //        return wordBag.wordTranslator;
        //    }
        //}
        /// <summary>
        /// 当前正在过滤权限的用户主体
        /// </summary>
        public AuthUser authUser;
        /// <summary>
        /// 需要计算权限的角色ID
        /// </summary>
        public List<string> roleIds;
        /// <summary>
        /// 需要计算权限的权限码
        /// </summary>
        public List<AuthWord> dataScopes;

        /// <summary>
        /// 过滤权限的资源集合
        /// </summary>
        public AuthGoodsBag goodsBag;

        private Func<AuthorBuilder<RealDialect>, string> _OnRoleEmpty;

        /// <summary>
        /// 是否使用登录人的访客集合
        /// </summary>
        protected bool _useLoginVisitorBag = true;

        protected bool adminAll = false;

        public AuthorBuilder()
        {
            this.goodsBag = new AuthGoodsBag();
            this.roleIds = new List<string>();
            this.dataScopes = new List<AuthWord>();
        }

        protected void setUsingRoles(List<string> roleOIDs) { 
            this.roleIds = roleOIDs;
            if (this.authUser != null) { 
                this.authUser.dutyOIDs = roleOIDs;
            }
        }

        /// <summary>
        /// 角色加载动作
        /// </summary>
        protected Action<SQLBuilder> _loadRoles = null;
        /// <summary>
        /// 设置查询构造的SQLBuilder
        /// </summary>
        /// <param name="kit"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> useSQLBuilder(SQLBuilder kit)
        {
            this.kit = kit;
            return this;
        }
        /// <summary>
        /// 自定义角色加载的过滤条件
        /// </summary>
        /// <param name="onload"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onloadRole(Action<SQLBuilder> onload)
        {
            _loadRoles = onload;
            return this;
        }
        /// <summary>
        /// 当角色的数据范围定义为空时的处理。
        /// </summary>
        /// <param name="whenRoleIsEmpty"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onEmpty(Func<AuthorBuilder<RealDialect>,string> whenRoleIsEmpty)
        {
            _OnRoleEmpty = whenRoleIsEmpty;
            return this;
        }

        /// <summary>
        /// 添加一项用于过滤权限的资源
        /// </summary>
        /// <param name="id"></param>
        /// <param name="code"></param>
        /// <param name="type"></param>
        /// <param name="group"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> useGoods(long id=0,string code="",string type="", string group = "2",string name="")
        {
            var tar = new AuthGoods {
                id = id,
                code = code,
                name = name,
                type = type,
                group = group

            };
            if (string.IsNullOrWhiteSpace(code) && id>0) {
                tar.code = tar.id.ToString();
            }
            this.goodsBag.addGoods(tar);
            return this;
        }
        /// <summary>
        /// 指定某人的数据
        /// </summary>
        /// <param name="userFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> whereUserIs(Func<AuthUser, string> userFilter)
        {
            wordBag.whereMan(userFilter);
            return this;
        }
        /// <summary>
        /// 为动态语义注册参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> useWordPara(string key,object val)
        {
            wordBag.useWordPara(key, val);
            return this;
        }
        /// <summary>
        /// 语义解析应用时刻。可以
        /// </summary>
        /// <param name="registerBuilder"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> onBuildLiveWord(Action<ConditionGroup, SQLBuilder> registerBuilder)
        {
            wordBag.onBuildLiveWord(registerBuilder);
            return this;
        }
        /// <summary>
        /// 指定一组用户的权限
        /// </summary>
        /// <param name="userFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> whereUserIn(Func<List<AuthUser>, string> userFilter)
        {
            wordBag.whereMan(userFilter);
            return this;
        }

        //组织过滤动作注册
        /// <summary>
        /// 组织判相同条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> whereOrgIs(Func<AuthOrg, string> doOrgFilter)
        {
            wordBag.whereOrgIs(doOrgFilter);
            return this;
        }
        /// <summary>
        /// 组织范围条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> whereOrgIn(Func<List<AuthOrg>, string> doOrgFilter)
        {
            wordBag.whereOrgIn(doOrgFilter);
            return this;
        }
        /// <summary>
        /// 组织下级条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> whereOrgLike(Func<AuthOrg, string> doOrgFilter)
        {
            wordBag.whereOrgLike(doOrgFilter);
            return this;
        }
        /// <summary>
        /// 组织的过滤条件
        /// </summary>
        /// <param name="doOrgFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> whereOrgOne(Func<AuthOrg, bool, string> doOrgFilter)
        {
            wordBag.whereOrgOne(doOrgFilter);
            return this;
        }
        public AuthorBuilder<RealDialect> whereOrgBag(Func<CodeRange<AuthOrg>, string> doOrgFilter)
        {
            wordBag.whereOrgBag(doOrgFilter);
            return this;
        }
        // 岗位过滤动作注册
        /// <summary>
        /// 岗位是条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> wherePostIs(Func<AuthPost, string> doPostFilter)
        {
            wordBag.wherePostIs(doPostFilter);
            return this;
        }
        /// <summary>
        /// 岗位范围条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> wherePostIn(Func<List<AuthPost>, string> doPostFilter)
        {
            wordBag.wherePostIn(doPostFilter);
            return this;
        }
        /// <summary>
        /// 岗位下级条件
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> wherePostLike(Func<AuthPost, string> doPostFilter)
        {
            wordBag.wherePostLike(doPostFilter);
            return this;
        }

        /// <summary>
        /// 是某个岗位的权限
        /// </summary>
        /// <param name="doPostFilter"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> wherePost(Func<AuthPost, bool, string> doPostFilter)
        {
            wordBag.wherePost(doPostFilter);
            return this;
        }

        /// <summary>
        /// 执行权限条件生成。没有角色时返回1=2，或者执行空事件。
        /// </summary>
        public string doBuild()
        {
            //加载角色
            if (this.dataScopes.Count == 0)
            {
                this.loadRole();
            }
            //调用词条加载完毕事件
            invokeOnLoadedWords();
            //角色仍然为空，调用事件或者返回
            if (wordBag.Empty && this.dataScopes.Count == 0 ) {
                return this.dealEmptyAuth();
            }

            //读取角色权限
            this.readRoleDataScope();
            this.invokeWordPreLoader();
            
            wordBag.parseWord(dialect);
            

            if (wordBag.Empty)
            {
                return this.dealEmptyAuth();
            }
            if (this.kit != null)
            {
                //检查前后是否发生条件变化
                var countPre = kit.ConditionCount;
                var tar = doWordBagBuild();
                var countAfter = kit.ConditionCount;
                if (countPre == countAfter)
                {
                    //未发生变化
                    return this.dealEmptyAuth(); 
                }
                return tar;
            }
            else
            {
                var tar = doWordBagBuild();
                if (string.IsNullOrWhiteSpace(tar))
                {
                    return this.dealEmptyAuth();
                }
                return tar;
            }
            

        }

        /// <summary>
        /// 权限构建结果为空的处理方式。
        /// </summary>
        /// <returns></returns>
        private string dealEmptyAuth() {
            if (this._OnRoleEmpty != null)
            {
                return this._OnRoleEmpty(this);
            }
            else if (kit != null)
            {
                kit.where("1=2");
                return "1=2";
            }
            else {                
                return "1=2";
            }
        }


        private string doWordBagBuild() { 
            
            return wordBag.build(this.kit, (wh) => {
                wh = this.invokeLazyReador(wh, kit);
                return wh;
            });
        }

        /// <summary>
        /// 清空注册的条件生成器，不包含角色加载条件。
        /// </summary>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> reset()
        {
            wordBag.resetBuilder();
            return this;
        }




        private void loadRole()
        {
            this.dataScopes =this.loadDataScopes();
        }

        ///以下为需要重写的抽象方法

        /// <summary>
        /// 读取所有角色的数据范围编码
        /// </summary>
        public abstract void readRoleDataScope();
        /// <summary>
        /// 加载当前用户的数据范围
        /// </summary>
        /// <returns></returns>
        public abstract List<AuthWord> loadDataScopes();




        #region 直接使用的where条件
        /// <summary>
        /// 设置指定人员的条件字段前缀
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> useUseIsField(string fieldName)
        {

            this.whereUserIs((user) =>
            {
                if (this.kit != null) { 
                    kit.where(fieldName, user.UserOID);
                }
                return string.Format("{0}='{1}'", fieldName, user.UserOID);
            })
                .whereUserIn((mans) => {
                    if (mans.Count == 0) return null;
                    var uids = mans.map(m => m.UserOID);
                    if (this.kit != null)
                    {
                        kit.whereIn(fieldName, uids);
                    }
                    return string.Format("{0} in ('{1}')", fieldName, string.Join("','", uids));
                });

            return this;
        }
        /// <summary>
        /// 设置指定组织的条件字段，不包含下级条件
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> useOrgIsField(string fieldName)
        {

            this.whereOrgIs((org) =>
            {
                if (this.kit != null) { 
                    kit.where(fieldName, org.HROID);
                }
                return string.Format("{0}='{1}'", fieldName, org.HROID);
            })
                .whereOrgIn((orgs) => {
                    if (orgs.Count == 0) return null;
                    var uids = orgs.map(m => m.HROID);
                    if (this.kit != null)
                    {
                        kit.whereIn(fieldName, uids);
                    }
                    
                    return string.Format("{0} in ('{1}')", fieldName, string.Join("','", uids));
                });

            return this;
        }
        /// <summary>
        /// 设置指定组织下级的条件
        /// </summary>
        /// <param name="classCodeField"></param>
        /// <returns></returns>
        public AuthorBuilder<RealDialect> useOrgLikeField(string classCodeField)
        {

            this.whereOrgLike((org) =>
            {
                if(this.kit != null)
                {
                    kit.whereLikeLeft(classCodeField, org.HRCode);
                }
                return string.Format("{0} like '{1}%'", classCodeField, org.HRCode);
            });

            return this;
        }

        public AuthorBuilder<RealDialect> useUseOIDFK(string fk)
        {

            this.whereUserIs((user) =>
            {
                kit.where(fk, user.UserOID);
                return string.Format("{0}='{1}'", fk, user.UserOID);
            })
                .whereUserIn((mans) => {
                    if (mans.Count == 0) return null;
                    var uids = mans.map(m => m.UserOID);
                    kit.whereIn(fk, uids);
                    return string.Format("{0} in ('{1}')", fk, string.Join("','", uids));
                });

            return this;
        }

        public AuthorBuilder<RealDialect> useOrgOIDFK(string fk)
        {

            this.whereOrgIs((org) =>
            {
                if (this.kit != null) {
                    kit.where(fk, org.HROID);
                }
                
                return string.Format("{0}='{1}'", fk, org.HROID);
            })
                .whereOrgIn((orgs) => {
                    if (orgs.Count == 0) return null;
                    var uids = orgs.map(m => m.HROID);
                    if(this.kit != null)
                    {
                        kit.whereIn(fk, uids);
                    }
                    return string.Format("{0} in ('{1}')", fk, string.Join("','", uids));
                });

            return this;
        }

        public AuthorBuilder<RealDialect> useOrgCode(string classCodeField)
        {

            this.whereOrgLike((org) =>
            {
                if (this.kit != null) { 
                    kit.whereLikeLeft(classCodeField, org.HRCode);
                }
                return string.Format("{0} like '{1}%'", classCodeField, org.HRCode);
            });

            return this;
        }

        #endregion
        /// <summary>
        /// 添加角色
        /// </summary>
        /// <param name="role"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        protected int readRole(AuthWord role, AuthUser user)
        {
            dialect.wordBag.BindWord(role);
            var cc= dialect.pipeline.readRoleScopeCode(dialect.wordBag, role, user);
            dialect.wordBag.BindWord(null);
            if (cc == 0) {
                if (role.type == "2")
                {
                    dialect.wordBag.addLazyWord(role);
                }
                else if (role.type == "3")
                {
                    dialect.wordBag.addLiveWord(role);
                }
            }
            return cc;
        }
    }


}