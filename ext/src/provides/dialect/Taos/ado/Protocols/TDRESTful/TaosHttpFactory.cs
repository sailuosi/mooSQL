using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.taos.Protocols.TDRESTful
{
    internal class TaosHttpFactory
    {
#if NET6_0_OR_GREATER
        private Dictionary<string, HttpClient> httpClientMap;

        public TaosHttpFactory() { 
            this.httpClientMap = new Dictionary<string, HttpClient>();
        }
        public HttpClient init() { 
         
            var sock= new SocketsHttpHandler();
            //是否自动重定向
            sock.AllowAutoRedirect = true;
            //自动重定向的最大次数
            sock.MaxAutomaticRedirections = 10;
            //每个请求连接的最大数量，默认是int.MaxValue,可以认为是不限制
            sock.MaxConnectionsPerServer = 10000;
            //连接池中TCP连接最多可以闲置多久,默认2分钟
            sock.PooledConnectionIdleTimeout= TimeSpan.FromMinutes(5);
            // //连接最长的存活时间,默认是不限制的,一般不用设置
            sock.PooledConnectionLifetime=Timeout.InfiniteTimeSpan;
            //建立TCP连接时的超时时间,默认不限制
            //ConnectTimeout = Timeout.InfiniteTimeSpan,
            //等待服务返回statusCode=100的超时时间,默认1秒
            //Expect100ContinueTimeout = TimeSpan.FromSeconds(1),

            var client= new HttpClient(sock);
            return client;
        }

        public HttpClient GetClient(TaosConnectionStringBuilder builder) {
            var key = builder.DataSource + builder.Port + builder.Username + builder.Password + builder.DataBase + builder.ConnectionTimeout;
            if(!httpClientMap.ContainsKey(key)) {

                var _client = this.init();
                string _timez = string.IsNullOrEmpty(builder.TimeZone) ? "" : $"?tz={builder.TimeZone}";
 
                var _uri = new Uri($"http://{builder.DataSource}:{builder.Port}/rest/sql{(!string.IsNullOrEmpty(builder.DataBase) ? "/" : "")}{builder.DataBase}{_timez}");
                _client.BaseAddress = _uri;
                _client.Timeout = TimeSpan.FromSeconds(builder.ConnectionTimeout);
                var authToken = Encoding.ASCII.GetBytes($"{builder.Username}:{builder.Password}");
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
                _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));
                _client.DefaultRequestHeaders.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));
                var _name = typeof(TaosRESTful).Assembly.GetName();
                _client.DefaultRequestHeaders.Add("User-Agent", $"{_name.Name}/{_name.Version}");

                httpClientMap[key]= _client;
                return _client;
            }
            return httpClientMap[key];
        }
#else

        public string getRequest(TaosConnectionStringBuilder builder,string sql) {
            string _timez = string.IsNullOrEmpty(builder.TimeZone) ? "" : $"?tz={builder.TimeZone}";
            var _uri = new Uri($"http://{builder.DataSource}:{builder.Port}/rest/sql{(!string.IsNullOrEmpty(builder.DataBase) ? "/" : "")}{builder.DataBase}{_timez}");
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_uri);
            //字符串转换为字节码
            byte[] bs = Encoding.UTF8.GetBytes(sql);
            //参数类型，这里是json类型
            //还有别的类型如"application/x-www-form-urlencoded"
            httpWebRequest.ContentType = "application/json;";
            //参数数据长度
            httpWebRequest.ContentLength = bs.Length;
            //设置请求类型
            httpWebRequest.Method = "POST";
            var authToken = Encoding.ASCII.GetBytes($"{builder.Username}:{builder.Password}");
            var auth = "Basic " + Convert.ToBase64String(authToken);
            httpWebRequest.Headers.Add("Authorization", auth);
            //设置超时时间
            httpWebRequest.Timeout = 20000;
            //将参数写入请求地址中
            httpWebRequest.GetRequestStream().Write(bs, 0, bs.Length);
            //发送请求
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //读取返回数据
            StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
            string responseContent = streamReader.ReadToEnd();

            //LogUtil.info("调用TD库数据插入接口成功，返回值为：" + responseContent);

            streamReader.Close();
            httpWebResponse.Close();
            httpWebRequest.Abort();
            return responseContent;

        }
#endif

    }
}
