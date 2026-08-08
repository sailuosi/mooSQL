// 基础功能说明：

using mooSQL.auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHNY.NET.Core.Author.SysRole;
public class AuthPipeline:PipelineDialect
{
    public override int readRoleScopeCode(WordBagDialect range, AuthWord role, AuthUser user)
    {
        //      { value: 1, label: '全部数据' },
        //{ value: 2, label: '本部门及以下数据' },
        //{ value: 3, label: '本部门数据' },
        //{ value: 4, label: '仅本人数据' },
        //{ value: 5, label: '自定义数据' },
        //      { value: 10, label: '本岗位数据' },
        //      { value: 11, label: '本岗位及下级数据' },

        if (role.scopeCode == "1")
        {
            range.addAll();
            return 1;
        }
        else if (role.scopeCode == "2")
        {
            range.addContainOrg(user.HisDivision);
        }
        else if (role.scopeCode == "3")
        {
            range.addBindOrg(user.HisDivision);
        }
        else if (role.scopeCode == "4")
        {
            range.addMan(user);
        }
        else if (role.scopeCode == "10")
        {
            var post = user.HisPost;
            range.addBindPost(post);
        }
        else if (role.scopeCode == "11")
        {
            var post = user.HisPost;
            range.addContainPost(post);
        }
        return 0;
    }
}
