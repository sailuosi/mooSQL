---
outline: deep
---

# 特殊集合
如区间、层级范围、值范围等

## 区间Sect
泛型类 Sect代表一个起止区间，包含最大值、最小值、无效值、左右区间的开闭性等标记

### Contain
是否包含

## Section
区间组 ，持有多个Sect类
### Count
成员数
### Contain
检查是否包含

### addSolo
添加一个值

### readConfig
读取区间配置


## CodeRange
代表一个层次码范围，具有自动检测过滤已包含的下级功能

### getAllBind
获取所有已绑定的值

### addBindValue
添加一个绑定值，不包含下级

### addContainValue
添加一个值，包含下级

### buildWhere
执行条件编制的动作