重要的操作命令
一些常用的命名

## 创建并添加项目到解决方案
# 创建 Web API 项目
```` bash
dotnet new webapi -o MyWebApi

# 创建空解决方案

dotnet new sln -o MySolution

# 进入解决方案目录

cd MySolution

# 将 Web API 项目添加到解决方案中
dotnet sln add ../MyWebApi/MyWebApi.csproj
```` 
## 发布为独立部署应用（带运行时）
```` bash
# 发布 Web API 项目，配置为 Release，平台为 win-x64，且自带运行时
dotnet publish ../MyWebApi/MyWebApi.csproj -c Release -r win-x64 --self-contained
````
## 查看 SDK 和运行时信息
```` bash
# 查看所有已安装的 .NET SDK 版本
dotnet --list-sdks

# 查看所有已安装的 .NET 运行时版本
dotnet --list-runtimes
````
## 运行命令
```` bash
# 指定运行环境为 Development / Production
dotnet run --environment Development

# 指定监听地址（适用于 ASP.NET Core）
dotnet run --urls "http://*:5000;https://localhost:5001"
#localhost:5000 表示仅本机访问。
# 0.0.0.0:5001 表示允许外部机器通过 IP 访问。 Ubantu使用添加

# 运行指定项目的代码（不切换目录）
dotnet run -p ../MyWebApi/MyWebApi.csproj

# 显示详细构建日志输出
dotnet run --verbosity detailed
````
## 📁 一、项目与解决方案管理
✅ 创建新项目
```` bash
# 创建控制台应用程序
dotnet new console -o MyConsoleApp

# 创建类库项目（Class Library）
dotnet new classlib -o MyLibrary

# 创建 ASP.NET Core Web API 项目
dotnet new webapi -o MyWebApi

# 创建 Blazor Server 项目
dotnet new blazorserver -o MyBlazorApp
````
## 📂 管理解决方案（.sln）
````
# 创建一个新的解决方案文件
dotnet new sln -o MySolution

# 将项目添加到解决方案中
dotnet sln add MyProject.csproj

# 从解决方案中移除项目
dotnet sln remove MyProject.csproj

# 查看当前解决方案包含的所有项目
dotnet sln list
````
## ⚙️ 二、构建与运行项目
```` bash
# 恢复所有 NuGet 包依赖项
dotnet restore

# 构建项目，默认是 Debug 配置
dotnet build

# 构建发布版本（Release）
dotnet build --configuration Release

# 运行项目（自动先构建再运行）
dotnet run
````
## 📦 三、发布应用程序
你可以选择发布为独立部署（Self-contained）或框架依赖（Framework-dependent）：

```` bash
# 发布为独立部署应用（包含运行时），适用于目标机器无 .NET 安装的情况
dotnet publish -r win-x64 --self-contained

# 发布为框架依赖应用（需目标机器安装对应 .NET 运行时）
dotnet publish -r win-x64 --framework-dependent
```` 
## 🧪 四、测试与维护
```` bash
# 运行项目中的所有单元测试
dotnet test

# 只运行指定名称的测试方法/类
dotnet test --filter MyTest

# 列出当前项目直接引用的 NuGet 包
dotnet list package

# 列出包括传递依赖在内的所有包信息
dotnet list package --include-transitive

# 添加一个 NuGet 包引用
dotnet add package Newtonsoft.Json

# 移除一个 NuGet 包引用
dotnet remove package Newtonsoft.Json

# 添加对本地项目的引用（如类库）
dotnet add reference ../MyLib/MyLib.csproj
````
## 🧰 五、SDK 与环境信息
```` bash
# 显示当前默认使用的 SDK 和运行时详细信息
dotnet --info

# 列出系统中已安装的所有 .NET SDK 版本
dotnet --list-sdks

# 列出系统中已安装的所有 .NET 运行时版本
dotnet --list-runtimes

# 显示 dotnet CLI 的帮助信息
dotnet help

# 使用指定版本的 SDK 执行某个命令（例如使用 6.0.100）
dotnet --sdk-version 6.0.100 build
````
## 📦 六、NuGet 包管理
```` bash
# 恢复所有 NuGet 包（等价于 restore）
dotnet restore

# 清除本地 NuGet 缓存（解决缓存导致的问题）
dotnet nuget locals all --clear

# 将项目打包成 .nupkg 文件（用于私有或公共 NuGet 源发布）
dotnet pack

# 将 NuGet 包推送到指定源（如 nuget.org）
dotnet nuget push MyPackage.nupkg --source https://api.nuget.org/v3/index.json
````
## 🔧 七、其他实用命令

```` bash
# 调用 MSBuild 并执行自定义目标（Target）
dotnet msbuild /t:CustomTarget

# 查看详细的构建日志输出（用于调试构建问题）
dotnet build --verbosity detailed

# 启动 .NET Interactive 工具（支持 C# 即时执行和 Jupyter Notebook）
dotnet interactive

# 查看构建性能摘要（优化构建耗时）
dotnet build --performance-summary

# 使用本地安装的 dotnet tool
dotnet tool run <tool-name>

# 启用源码链接调试（调试 NuGet 包时跳转 GitHub）
dotnet run --sourceLink
````
## 🧪 八、调试与性能分析相关命令

```` bash
# 启用 AOT 编译（.NET 7+ 功能）
dotnet run --aot

# 收集内存使用情况（用于 PerfView 分析）
dotnet run --collect:Memory

# 启用单文件发布并运行（.NET 6+）
dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet run --no-build --framework net6.0
````