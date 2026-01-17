
IoTSharp.Data.Taos  是 TDengine的ADO.Net提供程序。 它将允许你通过原生动态库、WebSocket、RESTful 三种协议访问TDengine，通过 Schemaless 完美实现了ExecuteBulkInsert批量插入、Stmt 实现了参数化执行。

连接协议说明
---
| 协议    | 使用|依赖| 说明                                                     |
| ----------|--- | --------  | ------------------------------------------------------------ |
| WebSocket |builder.UseWebSocket()|无依赖 | 纯C#实现， 支持 Schemaless 和 Stmt参数化
| Cloud DSN |builder_cloud.UseCloud_DSN()|无依赖 | 纯C#实现， 支持 Schemaless 和 Stmt参数化  
| Native | builder.UseNative()|libtaos | 原生协议， 支持3.0.x  libtaos 动态库，支持 Schemaless 和 Stmt参数化。使用前必须安装 TDengine-client 
| RESTful | builder.UseRESTful() |无依赖|   纯C#实现， 不支持  Schemaless 和 Stmt参数化

连接字符串示例
---
| 连接方式    |  示例                                                     |
| ----------| ------------------------------------------------------------ |
| TDengine云服务 | Data Source=gw.us-east.azure.cloud.tdengine.com;DataBase=iotsharp;Username=root;Password=taosdata;Port=80;PoolSize=20;Protocol=WebSocket;Token=4592d868d1b57c812edb3d8c11b4bbd1ffc747c0
| 使用原生库libtaos  |Data Source=DEVPER;DataBase=db_20230301123636;Username=root;Password=taosdata;Port=6030;PoolSize=20;Protocol=Native
| 使用 Http RESTful  |Data Source=DEVPER;DataBase=db_20230301123636;Username=root;Password=taosdata;Port=6041;PoolSize=20;Protocol=RESTful
| 使用 WebSocket  |Data Source=DEVPER;DataBase=db_20230301123636;Username=root;Password=taosdata;Port=6041;PoolSize=20;Protocol=WebSocket


