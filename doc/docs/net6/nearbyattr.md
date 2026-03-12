一些特性
作用在接口上的方法

1 允许接口匿名访问：可以不带token访问
```` c#
[AllowAnonymous]  
````


2 请求方式
```` c#
[HttpPost("mdmzhdangerSubCount")]
````
3 禁止规范化处理：默认接口会被自动规范化处理，返回结果自动带一层状态；
```` c#
[NonUnify]
````


4 关闭接口审计日志：该接口不记录操作日志
```` c#
[SuppressMonitor]
````


5 接口的显示名称
```` c#
[DisplayName("Swagger登录检查")]
````


作用在方法的参数上

```` c#

[FromBody]



[FromQuery]



[Required]
````
依赖注入
登录人
```` c#
UserManager _userManager;
````
  2. 缓存服务
```` c#
SysCacheService sysCacheService
````
  3. 请求上下文
```` c#
IHttpContextAccessor _httpContextAccessor
````

```` c#
/// <summary>
/// 系统登录授权服务
/// </summary>
[ApiDescriptionSettings(Order = 500)]
public class SysAuthService : IDynamicApiController, ITransient
{
    
    private readonly UserManager _userManager;
    private readonly SqlSugarRepository<SysUser> _sysUserRep;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SysMenuService _sysMenuService;
    private readonly SysOnlineUserService _sysOnlineUserService;
    private readonly SysConfigService _sysConfigService;
    private readonly ICaptcha _captcha;
    private readonly SysCacheService _sysCacheService;
    private MatchUser matchUser= new MatchUser();
    public SysAuthService(UserManager userManager,
        SqlSugarRepository<SysUser> sysUserRep,
        IHttpContextAccessor httpContextAccessor,
        SysMenuService sysMenuService,
        SysOnlineUserService sysOnlineUserService,
        SysConfigService sysConfigService,
        ICaptcha captcha,
        SysCacheService sysCacheService)
    {
        _userManager = userManager;
        _sysUserRep = sysUserRep;
        _httpContextAccessor = httpContextAccessor;
        _sysMenuService = sysMenuService;
        _sysOnlineUserService = sysOnlineUserService;
        _sysConfigService = sysConfigService;
        _captcha = captcha;
        _sysCacheService = sysCacheService;
    }


````





其它快捷方法
1. 抛出友好的异常，可被用户使用的。


```` c#
throw Oops.Oh("验证码不存在或已失效，请重新获取！");
````


2. 打印SQLBuilder的SQL

//调用后该sqlkit将一直输出SQL到控制台。
```` c#
kit.println();
````