// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    public interface IDialectChild
    {
        Dialect dialect { get; }
    }
}
