# 概述
::: info
AuthBuilder 类，设计理念延续自  SQLBuilder，重点解决数据库查询带数据权限的问题。因此也与SQLBuilder深度绑定，借由SQLBuilder可输出为纯粹的SQL，或者参数化的SQL。在类的设计与组织上，仅抽象出权限过程的复杂逻辑和繁复的各种优化判定，将核心的数据权限解析逻辑，交由业务系统侧来实现。因此，在每个业务系统都会有一套或多套伴生的子类，来定义自己的权限加载、解析逻辑。
:::
## 概念关键词

权限词条，简称词条：指某个具有数据权限范围的语义，如本人数据、本单位数据、作为的班主任的数据等；

访客：界定权限生效的对象，可以是某个人、某个部分、某个群组；它必须是系统加载就预先可知晓的。

资源：权限对象授权访问的资源，如菜单、按钮、某项功能等，它必须是系统加载就预先可知晓的。

预定义的权限关键词：

用户： AuthUser

岗位： AuthPost

组织： AuthOrg

词条： AuthWord

资源： AuthGoods



## 代码职责核心类
红色为必被重写的类

AuthorBuilder ---- 权限构建器，用于业务侧个性化权限的各项参数，并执行生成权限的结果；

AuthFactory    ---- 抽象工厂，提供业务侧个性化权限类的具体成员的入口，允许继承和修改 预定义的权限关键词实体类；

AuthDialect     ---- 权限方言，每个权限体系均不同，定义根据个性化的数据库表结构，产生 用户、组织、岗位、词条等权限实体；

PipelineDialect---- 权限“编译”管道，真正根据词条，翻译为 准备好的 词条集合，等待 WordBagDialect 执行打包；

WordBagDialect ---- 准备应用的词条集合，持有一组或数组 WordGroupBag，组间条件 or 组合。

WordGroupBag  ----  权限分组，同组内的条件按 and或 or进行组合。

WordTranslator  ----  动态语义功能支持，翻译器。



## 工具类

Childable  ---可为子集合的

CodeRange  --- 层次码范围，按层次码进行范围圈定的

ItemRange  --- 个体范围，不存在上下级关系，按个体进行圈定的



## 常用

````c#
duty.onEmpty((vf) =>
    {
        //未解析到权限时的处理。
        if (!string.IsNullOrWhiteSpace(manCode)) { 
            kit.whereIn("a.KB_TaskOID", (m) =>
            {
                m.select("m.KB_Task_FK")
                    .from("KB_TaskManType m")
                    //.where("m.SYS_Deleted = false")
                    .where("m.KM_Code", manCode);
                if (role != "")
                {
                    m.where("m.KM_Type", role);
                }
            });                    
        }
        return "";
    })
    .onParseWord((word, range) => {
        if (word.scopeCode == "t01")
        {
                //指代用户的门户人员配置对应的部门
                kit.whereIn("a.D_OrgOID", (mc) =>
                {
                    mc.select("dw.HH_Org_FK")
                    .from("kb_deptworkor dw")
                    .where("dw.Dw_Code", _userManager.Account);
                });
        }
        return null;
    })
    .useManVisit(manCode)
    .useWordPara("{manCode}", manCode)
    .useOrgVisit(entity.KB_OrgOID)
    .onBuildLiveWord((child, k) => {
        if (child.key == "{taskBase}") {
            child.ApplyToSQL(k);
        }
        else if (child.key == "{taskOrg}" && !string.IsNullOrEmpty(entity.KB_OrgOID))
        {
            kit.whereIn("a.KB_TaskOID", (org) =>
            {
                org.select("o.KB_Task_FK")
                    .from("KB_TaskOrgType o left join ucml_organize uo on uo.Varchar1 = o.KO_Code");
                child.ApplyToSQL(org);
            });
            return;
        }
    })
    .useOrgIsField("a.KB_OrgOID")
    .useOrgLikeField("a.KB_OrgCode")
    .useUseIsField("a.SYS_Created")
    .doBuild();
````


## API
AuthorBuilder 

1.过滤期间
//添加一项用于过滤权限的资源
````c#
useGoods(long id=0,string code="",string type="", string group = "2",string name="")
````
为动态语义注册参数
````c#
useWordPara(string key,object val)
````
## 构建期间

权限单位的构建
````c#
whereOrgIs(Func<AuthOrg, string> doOrgFilter)

whereOrgLike(Func<AuthOrg, string> doOrgFilter)

whereOrgIn(Func<List<AuthOrg>, string> doOrgFilter)

whereOrgOne(Func<AuthOrg, bool, string> doOrgFilter)

whereOrgBag(Func<CodeRange<AuthOrg>, string> doOrgFilter)
````
上层方法

````c#
useOrgOIDFK(string fk)

useOrgCode(string classCodeField)
权限用户的条件

whereUserIn(Func<List<AuthUser>, string> userFilter)

whereUserIs(Func<AuthUser, string> userFilter)
//直接注册外键
useUseOIDFK(string fk)
权限岗位的条件

wherePostIs(Func<AuthPost, string> doPostFilter)

wherePostIn(Func<List<AuthPost>, string> doPostFilter)

wherePostLike(Func<AuthPost, string> doPostFilter)

wherePost(Func<AuthPost, bool, string> doPostFilter)
````
3.事件

当角色的数据范围定义为空时的处理。
```` c#
onEmpty(Func<AuthorBuilder<RealDialect>,string> whenRoleIsEmpty)
````
词条加载完毕时刻，此时刚读取完毕角色下的词条，但尚未进行解析。
```` c#
onLoadedWords(Action<RealDialect> action)
````
数据范围解析时刻，此时，需要直接把结果生成到最终的条件中
```` c#
onParseWord(Func<AuthWord, RealDialect, string> parser)
````
语义解析应用时刻
```` c#
onBuildLiveWord(Action<ConditionGroup, SQLBuilder> registerBuilder)
````


## 成员



被过滤权限的用户

AuthUser authUser;

词条集合 

WordBagDialect wordBag

资源集合

AuthGoodsBag goodsBag;

数据权限集合
```` c#
List<AuthWord> dataScopes
````


## 扩展
    由于直接使用 AuthorBuilder仍有所不便，所以一般对它进行二次封装，更加便于使用，但同时也失去一些灵活性。



    U8下
```` c#
public static SQLBuilder useResp(this SQLBuilder kit, ClientUserInfo userManager, Action<RespBuilder> buildAuth)
````

## 责任条目权限的使用
1 概述

      核心类  DutyBuilder

      主要使用方式：获取到DutyBuilder类的实例，执行一系列 配置后，调用 doBuild() 方法，应用权限；

2 结合SQLBuilder类的使用方式，以下为最全的案例。
```` c#
    var portalDt = kit.clear()
        .select("")
        .from("zh_paotallink p j")
        .join(" join zh_portal t on p.ZH_Portal_FK=t.ZH_PortalOID")
        .where("D_HasPortal=1")
        .useDuty(_userManager, (duty) => {
            duty.useMenu(Pageid)  //设置要过滤的菜单
                .useVCLink(entity.cellLinkOID)
                .useLoginVisitBag(false) //禁用访客加载时的登录人过滤条件
                .useManVisit(manCode) //当不使用登录人时，指定访客过滤人
                .useOrgVisit(entity.KB_OrgOID)//指定访客过滤的单位
                .useWordPara("{manCode}", manCode)
                //自定义角色加载，当角色需要根据查询参数进行动态加载时
                .onloadRole((k) =>
                {
                  k.whereIn("d.HH_GoodsBag_FK", (g) =>
                  {
                    g.select("gi.HH_GoodsBag_FK")
                     .from("hh_goodsitem gi")
                     .where("gi.Gi_OID", paotalOID)
                     .where("Gi_Type='3'");
                    });
                });
                .useOrgIsField("a.D_OrgOID")  //设置指定组织的字段
                .useOrgLikeField("a.D_DeptCode") //指定组织及下级使用的字段
                .onParseWord((word, range) => {  //自定义词条解析
                    if (word.scopeCode == "t01")
                    {
                        //指代用户的门户人员配置对应的部门
                        kit.whereIn("a.D_OrgOID", (mc) =>
                        {
                            mc.select("dw.HH_Org_FK")
                            .from("kb_deptworkor dw")
                            .where("dw.Dw_Code", _userManager.Account);
                        });
                    }
                    return null;
                })
                .onBuildLiveWord((child, k) => {
                    if (child.key == "{taskBase}") {
                        child.ApplyToSQL(k);
                    }
                    else if (child.key == "{taskOrg}" && !string.IsNullOrEmpty(entity.KB_OrgOID))
                    {
                        kit.whereIn("a.KB_TaskOID", (org) =>
                        {
                            org.select("o.KB_Task_FK")
                                .from("KB_TaskOrgType o left join ucml_organize uo on uo.Varchar1 = o.KO_Code");
                            child.ApplyToSQL(org);
                        });
                        return;
                    }
                    else if (child.key == "{taskMan}" && !string.IsNullOrWhiteSpace(manCode))
                    {
                        kit.whereIn("a.KB_TaskOID", (man) =>
                        {
                            man.select("m.KB_Task_FK")
                               .from("KB_TaskManType m");
                            child.ApplyToSQL(man);
                        });
                        return;
                    }
                })
                .onEmpty((duty) => {
                    //没有配置权限时，可以查自己单位的。
                    kit.whereLikeLeft("b.Varchar1", myCode);
                    return "";
                })
                //当from语句中不含义可直接使用的字段，可通过自定义的方式创建SQL条件
                .whereOrgIn((orglist) =>
                {
                    var orgOIDs = orglist.map((li) => li.HROID);
                    kit.whereIn("c.MC_Code", (c) =>
                    {
                        c.select("c.CON_EMP_NUM")
                         .from("ucml_contact c")
                         .whereIn("c.HR_division_FK", orgOIDs);
                    });
                    return "";
                })
                .whereOrgLike((org) =>
                {
                    kit.whereIn("c.MC_Code", (c) =>
                    {
                        c.select("c.CON_EMP_NUM")
                         .from("ucml_contact c join ucml_organize o on c.HR_division_FK=o.UCML_OrganizeOID")
                         .whereLikeLeft("o.Varchar1", org.HRCode);
                    });
                    return "";
                })
                .whereUserIn((mans) => {
                    var manids = mans.map((li) => li.Acount);
                    kit.whereIn("c.MC_Code", manids);
                    return "";
                })
                .doBuild()
            ;
        })
        .orderby("D_Idx asc")
        .query();
````
##  简化案例  ，仅按菜单过滤时
```` c#
    .useDuty(_userManager, (duty) =>
    {   //使用权限包的权限进行过滤
        duty.useMenu(manSeePageId)
            .useLoginVisitBag(true)
            .useUseIsField("a.CON_EMP_NUM")//指定人字段
            .useOrgIsField("b.UCML_OrganizeOID")
            .useOrgLikeField("b.Varchar1")
            .onEmpty((duty) => {
                //没有配置权限时，可以查自己单位的。
                kit.whereLikeLeft("b.Varchar1", myCode);
                return "";
            })
            .doBuild();
    })
````


## 核心执行过程简介
1 方法生命周期

    useDuty：完成了DutyBuilder的创建，并把创建好的 duty实例传给委托，便于后续配置

    一系统use方法：主要对权限加载、解析、构建过程中一系统需要个性化的参数进行配置

   doBuild：此时真正的开始加载角色、解析角色、执行SQL条件的应用

2  理念： 

该系统权限主要理念为角色体系，数据权限的过滤逻辑综合考虑了菜单、用户，二者结合定位用户的数据范围。然后执行数据范围的解析和应用。

    

    用户权限=   资源 + 访客 + 词条

    资源： 用户可以拥有的资源项，如菜单、门户、VC等

    访客： 用户范围定义集合，类似于群组

    词条： 数据范围定义，如本人数据



3   代码分层策略

以上权限的基础逻辑，具有普适性。但在不同的系统中，对资源、用户、词条的具体定义和解析均有所区别。因此，核心库中封装了通用的权限加载、解析、应用过程，并把具体的动作通过委托、方言等方式抽象出来，由具体业务系统侧进行落实。

    表现在代码上，主要为 核心库 → 系统库的类的继承上

    核心库                                              系统库

    AuthorBuilder<RealDialect>   →   DutyBuilder:AuthorBuilder<DutyLoader>      核心权限编织器类

    AuthDialect                  →   DutyLoader: AuthDialect                    核心权限方言类