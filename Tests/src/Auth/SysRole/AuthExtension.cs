// 基础功能说明：

using mooSQL.auth;
using mooSQL.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core.Author.SysRole;
public static class AuthExtension
{

    public static SQLBuilder useAuthor(this SQLBuilder kit, UserManager userManager,Action<AuthBuilder> buildAuth) { 
        var tar= new AuthBuilder();
        
        tar.setUser(userManager);
        tar.useSQLBuilder(kit);
        buildAuth(tar);
        return kit;
    }

    public static SQLBuilder useAuthor(this SQLBuilder kit, AuthBuilder auth, Action<AuthBuilder> buildAuth)
    {
        buildAuth(auth);
        return kit;
    }


}
