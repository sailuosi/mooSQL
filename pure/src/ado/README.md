
## 文件夹说明

	builder  -- 承载用户侧主类 SQLBuilder类

	data     -- 执行环境的所有动作
		context --执行上下文
		database-- 数据库配置和应用实例
		dialect -- 方言定义
		executor -- 执行器
		exstend -- 扩展执行方法，如批量更新、bulk等
		instance -- 实例工厂
		log      -- 日志
		watch    -- 监听器、切面、拦截器

	typehandle -- 执行结果的类型化处理


	//扩展部分---SQL结构化，使用访问者模式进行构建

	clause     --  SQL语句结构化的部分。仅承载基础的SQL语句结构化表现，不拥有转换功能。



## 执行器说明

### ExeSession 代表一个数据库执行会话
功能包含打开连接、打开事务等

### CmdExecutor 
代表命令执行功能，同位SqlCommand 
'
### DBExecutor
代表数据库层面的查询执行功能，它持有数据库信息DBInstance，输入SQLCmd,输出查询结果。
由于承载的事务功能具有独占性，同时运行多个查询时，需要多个DBExecutor实例。
但对于顺序的SQL执行，可以使用同一个DBExecutor实例。


### DBRunner 
数据库执行器，它与DBExecutor定位稍有类似，但承载了SQL命令的积累和执行。不再是单纯的执行器。具有了状态。

### DBInstance
数据库的一个可用实例，持有数据库配置、数据库方言，是运行就绪、可缓存的数据库实例。

