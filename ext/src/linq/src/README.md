


项目说明：
    子项目主要实现 对核心库的增强，增加基于实体类的linq特性，以及其衍生的用法。


代码目录
    src:    一级目录
        clause   SQL语句的代表目录。  用于构建一个能够表示一个SQL语句完整结构的对象。

        linq     暴露给用户的核心表达式目录

        entity   实体有关的功能
            mapping   映射
            metadata  特性的解析
            schemaProvider  特性上的解析成果信息


代码清理：
    子项目不包含任何数据库执行器、数据库方言等与核心库重合的功能点。

    intercepters  拦截器。  由于这里主要是对数据库执行操作处理，可替代。



职能变更/简化

    IQueryRunner:
        原来承担时表达式的执行功能，持有要执行的信息，并执行。非函数式。

    IDataContext
        承载linq 配置、工厂组装等职能

    DataConnection：IDataContext
        承载 数据库信息、待执行SQL、配置信息、各类上下文。
        过于复杂，剔除持有的SQL信息，剔除执行职能
        执行的职能仅由 IQueryRunner 保留
        保留数据库信息、配置信息；



