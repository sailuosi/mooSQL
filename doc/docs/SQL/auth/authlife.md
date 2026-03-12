---
outline: deep
---

# 概述
::: info
权限执行生命周期和功能设计说明
:::


## AuthorBuilder
本类是整个执行的驱动类，承载了权限的整个执行过程，驱动各阶段的执行。其主要成员如下：

### AuthDialect--dialect
抽象方言对象，用于处理权限的数据加载和解析部分，对应于各子类如RespLoader、AuthLoader等。

### SQLBuilder--kit
用于构建SQL的工具，业务侧传入，以便直接织入权限SQL

### WordBagDialect--wordBag
词条集合，同样方言化，便于业务侧进行个性化的词条定义。默认实现为 WordGroupBag，即分组词条。可以通过重写抽象工厂AuthFactory，以更换词条产品。

### AuthUser--authUser
权限主体，用户。

### roleIds
需要计算权限的角色ID

### dataScopes
未解析的原始词条

### AuthGoodsBag--goodsBag
过滤权限的资源集合

## 执行权限生成的过程

### 前置时刻
此时，主要需要初始化环境信息，如登录人，需要过滤词条范围的资源集合等。

登录人
```` c#
.useDuty(_userManager, (duty) => {
    duty....
})
````


资源范围限定
```` c#
duty.useMenu(Pageid)  //设置要过滤的菜单
    .useVCLink(entity.cellLinkOID)
    .useLoginVisitBag(false) //禁用访客加载时的登录人过滤条件
    .useManVisit(manCode) //当不使用登录人时，指定访客过滤人
    .useOrgVisit(entity.KB_OrgOID)//指定访客过滤的单位
    .useWordPara("{manCode}", manCode)
````

### 加载词条
加载角色，调用loadDataScopes方法，从数据库读取权限主体拥有的词条，放入到 dataScopes中
加载完毕后，触发 AuthBuilder 中通过 onLoadedWords 注入的动作
```` c#
public override List<AuthWord> loadDataScopes()
{
    return dialect.loadRole(user, (kit) =>
    {
        if (goodsBag !=null && goodsBag.Goods.Count>0)
        {
            var codes = goodsBag.Goods.map((goo) => goo.code);
            //带有BPO参数时，按照BPO进行过滤
            kit.whereIn("r.UCML_RESPONSIBILITYOID", (e) =>
            {   //传入了菜单的全面码时，使用权限码进行角色过滤。
                e.select("m.UCML_RESPONSIBILITYOID").distinct()
                    .from("UCML_RESPONSIBILITYBPOMAP as m left join BusinessList as b on m.BusinessListOID = b.BusinessListOID ")
                    .whereIn("b.BPOID", codes)
                    .where("m.UCML_RESPONSIBILITYOID is not null");
            });
        }

        if (_loadRoles != null)
        {
            _loadRoles(kit);
        }
    });

}
````
### 空权限检查
检查词条是否为空，触发 AuthBuilder中通过 onEmpty 注入的动作
注入时如下：
```` c#
resp.onEmpty((duty) => {
    //没有配置权限时，可以查自己单位的。
    kit.whereLikeLeft("b.Varchar1", myCode);
    return "";
})
````
### 解析词条
调用 AuthBuilder 中 readRoleDataScope 方法，放入到 wordBag 中，
一般此处会调用子方法readRole，然后readRole唤起 过滤器管道 PipelineDialect的readRoleScopeCode方法。

AuthBuilder需要进行重写，实现如下
```` c#
public override void readRoleDataScope()
{
    if (user.UserID.ToLower() == "admin")
    {
        //超级管理员，直接添加所有权限
        wordBag.addAll();
        return;
    }
    var au = CastToUser(user);
    foreach (AuthWord roleScope in this.dataScopes)
    {
        this.readRole(roleScope, au);
    }

}
````
然后调用的过滤器管道

```` c#
public class RespPipeline : PipelineDialect
{
    public override int readRoleScopeCode(WordBagDialect range, AuthWord role, AuthUser user)
    {

        if (role.scopeCode == "4")//所有数据
        {
            range.addAll();
            return 1;
        }
        else if (role.scopeCode == "28")//本管理员
        {
            var orgs = user.loadBindWorkOrg(role);
            foreach (var org in orgs)
            {
                range.addBindOrg(org);
            }
        }
        else if (role.scopeCode == "29")//本管理员
        {
            var orgs = user.loadBindWorkOrg(role);
            foreach (var org in orgs)
            {
                range.addContainOrg(org);
            }
        }
        else if (role.scopeCode == "3")//本部门及下级
        {
            range.addContainOrg(user.HisDivision);
        }
        else if (role.scopeCode == "2")//本部门
        {
            range.addBindOrg(user.HisDivision);
        }
        else if (role.scopeCode == "1")//本人数据
        {
            range.addMan(user);
        }
        else if (role.scopeCode == "7")//本单位及下级
        {
            range.addContainOrg(user.HisOrg);
        }
        else if (role.scopeCode == "6")//本单位
        {
            range.addBindOrg(user.HisOrg);
        }
        else if (role.scopeCode == "999")//没有岗位
        {
            var post = user.HisPost;
            range.addBindPost(post);
        }
        else if (role.scopeCode == "999")//没有岗位
        {
            var post = user.HisPost;
            range.addContainPost(post);
        }
        return 0;
    }
}
````


### 动态词条解析前时刻
调用 AuthBuilder中 onLoadWord 注入的数据范围加载方法，主要用于在动态词条解析前，放入动态词条所需的动态业务参数变量。
```` c#
//调用词条加载完毕事件
private void invokeOnLoadedWords() {
    if (_onAfterLoadWordActions != null) {
        foreach (var action in _onAfterLoadWordActions)
        {
            action(dialect);
        }
    }
}
````

### 动态词条解析
调用 wordTranslator 词条翻译器，进行词条解析,内置翻译器为 WordTranslator
```` c#
    resp.onBuildLiveWord((child, k) => {
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
````
### 静态词条解析
调用 doWordBagBuild，触发 wordBag的 build 方法，
每个wordBag的build方法会调用其注册的 use系列、where系列解析方法，如默认的 useOrgIsField、whereOrgIs等，真正的将权限SQL织入到SQL中。
```` c#
    resp.useOrgIsField("a.D_OrgOID")  //设置指定组织的字段
        .useOrgLikeField("a.D_DeptCode") //指定组织及下级使用的字段
````
### 最后的空检查
如果构造的权限为空，调用 dealEmptyAuth ，唤起 onEmpty ，或者 创建 “1=2”条件。