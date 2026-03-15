// 基础功能说明：

using mooSQL.auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core.Author.SysRole;
public class MyAuthFactory : AuthFactory<AuthLoader>
{
    public override AuthLoader GetDialect()
    {
        return new AuthLoader() ;
    }
}
