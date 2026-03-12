# 大模型接入功能
命名空间：mooSQL.AI
功能：提供调用通用openAI.SDK接口的能力。支持国内的DeepSeek/千问，集团部署的dify平台等接口。提供接口返回数据的基础解析能力，傻瓜式的返回大模型的答案，而不用关心大模型接口的具体返回值格式。

## 核心成员类
### 配置参考

```` json
{
    "$schema": "https://gitee.com/dotnetchina/Furion/raw/v4/schemas/v4/furion-schema.json",

    // 具体配置含义如下
    "LLMConfig": {
        "Connections": [
            { // DeepSeak1 --
                "Index": 0,
                "Name": "DeepSeek",
                "Type": "DeepSeek", // 
                "ApiBaseUrl": "https://openrouter.ai/api/v1/chat/completions", // url
                "ApiKey": "sk-or-v1-6ca49962bf48a3e08d60283cc377e5a5b418d3c54c011e04c09e19355a332",
                "ModelName": "deepseek/deepseek-chat:free"
            }
        ]
    }
}
````
### LLMConfig
代表大模型的配置信息，包含名称、类型、接口地址、密钥、模型名称等
#### Index
索引，代表连接位，用于LLMCash获取连接使用
#### Type
代表大模型的类型，包含 DeepSeek ChatGPT QianWen Doubao HHNYDify 等
#### ApiBaseUrl
接口地址
#### ApiKey
访问密钥
#### ModelName
模型名称
#### MaxTokens
最大Token数量
#### Temperature
温度系数
#### Stream
是否流
#### SystemHint
系统提示词

## LLMInstance
代表大模型配置的实例对象，包含配置信息，和方言处理工具类 

## HttpLLMClient
调用客户端，供使用的核心工作类

### send
发送方法，可以发送一个请求


### sendAsync
异步的发送请求

```` c#
var client = LLMCash.useClient(0,"test");

var msg = client.sendAsync("你好");
var re = msg.Result;
````

## 配置类LLMCash
代表大模型配置的缓存类，包含配置信息，和实例对象的缓存。

### 基础实现

```` c#
public static class LLMCash
{
    private static LLMCachedBoxBase cash = null;

    public static LLMInstance GetLLMInstance(int position)
    {
        if (cash == null)
        {
            initFactory();

        }
        try
        {
            return cash.getInstance(position);
        }
        catch (Exception ex)
        {

            return cash.getInstance(position);
        }

    }

    public static ILLMClient useClient(int position,string sessionId)
    {
        if (cash == null)
        {
            initFactory();

        }
        try
        {
            return cash.useClient(position,sessionId);
        }
        catch (Exception ex)
        {
            throw new Exception("初始化失败大模型客户端失败，请检查配置文件是否正确，或者网络是否正常");

        }

    }


    private static void initFactory()
    {
        cash = new LLMCachedBox();
        cash.InitConfig();
    }
}

````

### 工厂类
```` c#
public class LLMCachedBox : LLMCachedBoxBase
{
    public override BaseLLMClientFactory GetFactory()
    {
        return new HttpLLMClientFactory();
    }

    public override HttpClient GetHttpClient()
    {
        return App.GetService<HttpClient>();
    }

    public override IExeLog GetLogger()
    {
        return new MooLoger();
    }

    public override void InitConfig()
    {
        var configAll = App.Configuration;
        var myConfigs = App.GetOptions<LLMConfigOptions>();
        if (myConfigs != null && myConfigs.Connections != null && myConfigs.Connections.Count > 0)
        {
            this.AddConfigs(myConfigs.Connections);
        }
    }

    public override LLMConfig LoadConfig(int postion)
    {
        throw new NotImplementedException();
    }
}

````