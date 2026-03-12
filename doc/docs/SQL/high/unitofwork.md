# 工作单元

一、概述
工作单元是一组操作，这些操作要么全部成功，要么全部失败。工作单元是一个原子操作，它是一个事务的边界。工作单元的目的是确保一组操作的原子性，即要么全部成功，要么全部失败。

二、工作单元的实现
工作单元的底层实际上还是使用的事务，只不过是将事务的操作封装到了一个类中，使得事务的操作更加方便。在面对实体类操作数据的场景下，它被设计为直接接受要操作的实体类，而不需要关心的具体的SQL语句

## 初始化

1、通过常规的DBCash工厂获取
工作单元一般情况下的完整名称为 UnitOfWork，但为使用快捷，我们提供了一个别名 useWork，使用起来更加方便。
````c#
//使用全称
var work= DBCash.useUnitOfWork(0);
//使用简称
var work2= DBCash.useWork(0);

````
2、在业务侧没有注册的情况下，我们也可以通过以下方式获取
````c#
//获取数据库实例
var db = DBCash.GetDBInstance(0);
var work3= db.useWork();

````

## 工作单元的使用
工作单元的使用非常简单，只需要在业务层调用即可，不需要关心具体的实现细节。
````c#
work.Insert(new HHDutyItem());
work.InsertRange(new List<HHDutyItem>());
work.Update(new HHDutyItem());
work.UpdateRange(new List<HHDutyItem>());
work.Delete(new HHDutyItem());
work.DeleteRange(new List<HHDutyItem>());

//或者，也可以直接操作SQL
work.AddSQL(new SQLCmd("insert into HHDutyItem(id) values('123')"));
work.AddSQLs(new List<SQLCmd>() { });
//提示：此处的SQLCmd对象，是SQLBuilder的构建产物之一。

//或者，也可以直接操作SQL
work.InsertBySQL((kit) =>
{
    kit.setTable("HHDutyItem")
    .set("id", 1)
    .set("name", "123");  
}
);
work.UpdateBySQL((kit) =>
{
    kit.setTable("HHDutyItem")
        .set("sex", 1)
        .set("name", "123")
        .where("id", 1);
}
);
work.DeleteBySQL((kit) =>
{
    kit.setTable("HHDutyItem")
    .where("id", 1);
}
);

//最后执行提交
work.Commit();
````
## 工作单元的提交
工作单元的提交是通过调用Commit方法来实现的，该方法会将所有的操作提交到数据库中。
````c#
work.Commit();

````

# API

## AddSQL
添加SQL语句到工作单元中，该方法接受一个SQLCmd对象，该对象是SQLBuilder的构建产物之一。
````c#
work.AddSQL(new SQLCmd("insert into HHDutyItem(id) values('123')"));
````
## AddSQLs
添加SQL语句到工作单元中，该方法接受一个SQLCmd对象的集合，该对象是SQLBuilder的构建产物之一。
````c#
work.AddSQLs(new List<SQLCmd>() { });
````
## InsertBySQL
添加插入语句，用户只需自定义插入语句的表定义、字段赋值，其他的都由工作单元自动处理。
````c#
work.InsertBySQL((kit) =>
{
    kit.setTable("HHDutyItem")
    .set("id", 1)
    .set("name", "123");
});
````
## Insert
Insert允许你添加一个要新增的实体记录，等待保存到数据库。
````c#
work.Insert(new HHDutyItem());
````

## InsertRange
InsertRange允许你添加一组要新增的实体记录的集合，等待保存到数据库。
````c#
work.InsertRange(new List<HHDutyItem>());
````

## Update
Update允许你添加一个要更新的实体记录，等待保存到数据库。
````c#
work.Update(new HHDutyItem());
````

## UpdateRange
UpdateRange允许你添加一组要更新的实体记录的集合，等待保存到数据库。
````c#
work.UpdateRange(new List<HHDutyItem>());
````

## UpdateBySQL
UpdateBySQL允许你更新的SQL语句，等待保存到数据库。可以在某个表没有对应的实体类时，使用该方法。
````c#
work.UpdateBySQL((kit) =>
{
    kit.setTable("HHDutyItem")
    .set("sex", 1)
    .set("name", "123")
    .where("id", 1);
});
````

## Delete
Delete允许你添加一个要删除的实体记录，等待保存到数据库。
````c#
work.Delete(new HHDutyItem());
````

## DeleteRange
DeleteRange允许你添加一组要删除的实体记录的集合，等待保存到数据库。
````c#
work.DeleteRange(new List<HHDutyItem>(
    
))
````

## DeleteById
DeleteById允许你添加一个要删除的实体记录的ID，等待保存到数据库。
````c#
work.DeleteById<HHDutyItem>(1);
````

## DeleteByIds
DeleteByIds允许你添加一组要删除的实体记录的ID的集合，等待保存到数据库。
````c#
work.DeleteByIds<HHDutyItem>(new List<int>() { 1, 2, 3 });
````

## DeleteBySQL
DeleteBySQL允许你添加一个要删除的SQL语句，等待保存到数据库。
````c#
work.DeleteBySQL((kit) =>
{
    kit.setTable("HHDutyItem")
    .where("id", 1);
});
````

## Commit
Commit允许你提交所有的操作到数据库中。
````c#
work.Commit();
````