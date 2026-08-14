// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel { 
/// <summary>
/// 导入任务在客户端或服务端的简单进度与日志载体。
/// </summary>
public class InWorkProgress
{
    /// <summary>进度描述（如百分比或阶段文案）。</summary>
    public string progress;
    /// <summary>是否已全部完成。</summary>
    public bool isdone;
    /// <summary>最近一次或累计的日志文本。</summary>
    public string log;
}
}

