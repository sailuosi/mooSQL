---
outline: deep
---

# 概述
::: tip
事务在整个顶层设计中，具有多处体现。        
在SQLBuilder层，通过添加：DisPosable接口，实现了事务的功能，具化了基础的事务能力。        
在BatchSQL层，通过SQL语句的批量积累和执行，能够在不扩展影响范围的情况下，最终时刻一次性提交所有变化。        
在实体执行层，通过UnitOfWork类，实现了仓储模式下的事务支持。
:::

::: warning
基于SQLBuilder的事务，目前已实现生态打通：       
kit.beginTransaction()后，相应的useClip useRepo 方法，能够传递事务。 
基于kit的扩展方法 insert/update/delete实体类套件，同样传递了事务。        
insertList的批量插入，同样传递事务。
:::

## SQLBuilder
SQLBuilder类作为一个综合用户侧入口，事务功能默认具有传递性，即：
- 开启事务后，所有的后续操作都会在一个事务中执行。包括创建的Clip、实体类的直接操作、开启的仓储类等
- 提交事务后，所有的操作都会提交到数据库。同时，销毁事务上下文。
- 回滚事务后，所有的操作都会回滚到数据库。
- 为保证能够被正确的释放资源，建议使用using语句来开启事务，这样能够确保事务在使用完成后，能够被正确的释放资源。

### beginTransaction
开启事务，开启事务，此后的所有的操作在commit前都会在一个事务中
```` c#
kit.beginTransaction();
````

### commit
提交事务，如果autoRollBack为true则在执行出错时自动回滚
```` c#
kit.commit();
````
### useTransaction
传递事务，传递事务，将当前的事务传递给下一个操作，下一个操作会在当前事务中执行。
::: warning
只建议在深度熟悉底层实现时使用，防止误用导致扩大事务范围。
::: 
```` c#
kit.useTransaction(transaction);
````
### 综合使用案例
```` c#
using (kit) { 
    kit.beginTransaction();
    foreach (var bill in bills) { 
        kit.clear()
            .setTable("table")
            .set("DATE", bill.SendStart)
            .where("OID", bill.DueInCoalOID)
            .doUpdate();
        cc++;
    }
    if (cc > 0) { 
        kit.commit();
    }
}
````


## BatchSQL
具体见[BatchSQL](/SQL/basis/batchSQLbase)

## 工作单元
具体见[工作单元](/SQL/high/unitofwork)