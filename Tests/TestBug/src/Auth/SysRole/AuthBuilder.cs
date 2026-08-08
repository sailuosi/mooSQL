// 基础功能说明：

using HHNY.NET.Core;
using HHNY.NET.Core.Author.SysRole;
using mooSQL.auth;
using mooSQL.Pure.Tests.src.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core.Author;
public class AuthBuilder:AuthorBuilder<AuthLoader>
{


    public AuthBuilder() {
        this.factory = new MyAuthFactory();
    }
    public WordBagDialect wordBag
    {
        get
        {
            return dialect.wordBag;
        }
    }


    public UserManager user { get; set; }


    /// <summary>
    /// 读取所有角色的数据范围编码
    /// </summary>
    public override void readRoleDataScope()
    {
        if (user.SuperAdmin)
        {
            //超级管理员，直接添加所有权限
            wordBag.addAll();
            return;
        }
        AuthUser au = null;
        foreach (AuthWord roleScope in this.dataScopes)
        {
            readRole(roleScope, au);
        }

    }
    /// <summary>
    /// 根据配置的页面、权限码、或自定义where条件，加载角色
    /// </summary>
    public override List<AuthWord> loadDataScopes()
    {
        return dialect.loadRole(user, (kit) =>
        {
            if(this.goodsBag !=null && goodsBag.Goods.Count > 0)
            {
                foreach (var good in this.goodsBag.Goods) { 
                    if (good.type=="1" && good.id>0)
                    {
                        kit.whereIn("r.Id", (e) =>
                        {   //传入了菜单的全面码时，使用权限码进行角色过滤。
                            e.select("mn.RoleId")
                             .from("hh_sysrolemenu mn ")
                             .where("mn.MenuId", good.id);
                        });
                    }
                    if (good.type == "1" && good.id==0 && !string.IsNullOrWhiteSpace(good.code))
                    {
                        //SELECT distinct r.RoleId from hh_sysmenu e join hh_sysrolemenu r on e.Id=r.MenuId
                        kit.whereIn("r.Id", (e) =>
                        {   //传入了菜单的全面码时，使用权限码进行角色过滤。
                            e.select("u.RoleId")
                             .from("hh_sysmenu e join hh_sysrolemenu u on e.Id=u.MenuId")
                             .where("e.Permission", good.code);
                        });
                    }                
                }
            }

            if (_loadRoles != null)
            {
                _loadRoles(kit);
            }
        });

    }

}
