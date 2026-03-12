# 仓储

一、仓储的定义
仓储，顾名思义，存储数据的仓库。仓储作用之一是用于数据的持久化。从架构层面来说，仓储用于连接领域层和基础结构层，领域层通过仓储访问存储机制，而不用过于关心存储机制的具体细节。按照DDD设计原则，仓储的作用对象的领域模型的聚合根，也就是说每一个聚合都有一个单独的仓储。可能这样说大家未必能理解，相信看了仓储的代码设计，大家能有一个更加透彻的认识。

二、使用仓储的意义
1、站在领域层更过关心领域逻辑的层面，上面说了，仓储作为领域层和基础结构层的连接组件，使得领域层不必过多的关注存储细节。在设计时，将仓储接口放在领域层，而将仓储的具体实现放在基础结构层，领域层通过接口访问数据存储，而不必过多的关注仓储存储数据的细节（也就是说领域层不必关心你用EntityFrameWork还是NHibernate来存储数据），这样使得领域层将更多的关注点放在领域逻辑上面。

2、站在架构的层面，仓储解耦了领域层和ORM之间的联系，这一点也就是很多人设计仓储模式的原因，比如我们要更换ORM框架，我们只需要改变仓储的实现即可，对于领域层和仓储的接口基本不需要做任何改变。


## 初始化

````c#
private readonly SooRepository<SysRegion> _sysRegionRep;

public SysRegionService(SqlSugarRepository<SysRegion> sysRegionRep)
{
    _sysRegionRep = DBCash.useRepo<SysRegion>(0);
}

````

## GetList
查询所有数据

````c#
<List<SysRegion> list= _sysRegionRep.GetList(u => u.Pid == input.Id);
````
获取前100条
````c#
<List<SysRegion> list= _sysRegionRep.GetList(100);
````

## GetFirst
获取第一条结果

````c#
var sysRegion =  _sysRegionRep.GetFirst(u => u.Id == input.Id);

````
## GetPageList
查询获取分页结果，并结合了clip功能实现自定义的条件过滤。
````c#
var res=Rep.GetPageList(input.Page, input.PageSize, (c, d) => {
    c.where(()=>d.CreateTime,input.StartTime,">=")
    .where(()=>d.CreateTime,input.EndTime,"<=")
    .orderByDesc(()=>d.CreateTime);
});

````
## GetTreeList
查询树结构数据，传入外键，然后按主键进行查询。可自定义每层的更多自定义过滤条件（通过Clip）
````c#
var menuList = _sysMenuRep.GetTreeList((m) => m.Pid, 0
    , (c, m) => {
        c.where(() => m.Type, MenuTypeEnum.Btn, "!=")
        .where(() => m.Status, StatusEnum.Enable)
        .orderBy(() => m.OrderNo)
        .orderBy(() => m.Id);
    });
````

## GetChildList
查询子节点列表
````c#
var list = Rep.GetChildList((r) => r.Pid, input.Id);
````

## Insert
新增一条数据，提交到数据库
````c#
_sysRegionRep.Insert(sysRegion)

````
## InsertRange
批量新增数据，提交到数据库
````c#
_sysRegionRep.InsertRange(sysRegions)

````


## IsAny
检查是否存在
````c#
var isExist =  _sysRegionRep.IsAny(u => u.Name == input.Name && u.Code == input.Code);
if (isExist)
    throw Oops.Oh(ErrorCodeEnum.R2002);

````

## Update
更新一条数据，提交到数据库
````c#
_sysRegionRep.Update(new SysRegion());

````
## UpdateRange
批量更新数据，提交到数据库
````c#
_sysRegionRep.UpdateRange(new SysRegion());

````


## DeleteByIds
按主键进行删除，批量
````c#
public async Task DeleteRegion(DeleteRegionInput input)
{
    var regionTreeList = _sysRegionRep.useBus().Where(u => u.Pid == input.Id).ToList();
    var regionIdList = regionTreeList.Select(u => u.Id).ToList();
    regionIdList.Add(input.Id);
    _sysRegionRep.DeleteByIds(regionIdList);
}

````

## Delete
条件删除
````c#
_sysPosRep.Delete(u => u.Id == input.Id);

````
## ChangeTo
获取一个新的仓库，基于某个实体类
````c#
var hasPosEmp =  _sysPosRep.ChangeTo<SysUser>()
    .IsAny(u => u.PosId == input.Id);

````