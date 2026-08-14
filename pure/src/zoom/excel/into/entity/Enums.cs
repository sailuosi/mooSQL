

namespace mooSQL.excel.context
{
    /// <summary>
    /// 导入流程中的断点/阶段标记，用于控制是否继续读取 Excel 行或表数据等。
    /// </summary>
    public enum breakPoint
    {
        /// <summary>无断点。</summary>
        none = 0,
        /// <summary>在 Excel 层面暂停或标记。</summary>
        excel = 9,
        /// <summary>当前 Excel 行处理完毕，继续下一行。</summary>
        excelRowContine = 1,
        /// <summary>在当前 Excel 行处中断（不继续后续逻辑）。</summary>
        excelRowBreak = 2,
        /// <summary>当前表数据块继续。</summary>
        tableContinue = 3,
        /// <summary>在当前表数据处中断。</summary>
        tableBreak = 4,
        /// <summary>清空或重置相关状态。</summary>
        clear = 6,
    }
    /// <summary>
    /// 表或列的写入策略：仅插入、仅更新、读写兼有或仅校验等。
    /// </summary>
    public enum writeMode
    {
        /// <summary>未指定，通常继承全局配置。</summary>
        none = 0,
        /// <summary>仅插入新行。</summary>
        insert,
        /// <summary>仅更新已存在行。</summary>
        update,
        /// <summary>插入与更新均可（由业务判定）。</summary>
        write,
        /// <summary>仅校验，不写库。</summary>
        check
    }
    /// <summary>
    /// 校验失败时的处理策略（作用范围与是否继续导入等）。
    /// </summary>
    public enum checkFailAct
    {
        /// <summary>不采取特殊动作。</summary>
        none = 0,
        /// <summary>仅标记当前单元格/字段自身。</summary>
        self,
        /// <summary>整行标记或按行处理。</summary>
        row,
        /// <summary>静默忽略，不阻断流程。</summary>
        silent,
        /// <summary>跳过当前项，继续下一项。</summary>
        next,
        /// <summary>回退或提示到上一处理点。</summary>
        before,
        /// <summary>将错误信息写回 Excel 等。</summary>
        excel,

    }
    /// <summary>
    /// 列值在导入解析时的逻辑类型（与数据库类型映射相关）。
    /// </summary>
    public enum valueType
    {
        /// <summary>未指定类型。</summary>
        none = 0,
        /// <summary>字符串类型。</summary>
        stringi = 1,
        /// <summary>数值类型。</summary>
        number = 2,
        /// <summary>日期时间类型。</summary>
        date = 3,
        /// <summary>GUID 类型。</summary>
        guid = 4,
        /// <summary>布尔类型。</summary>
        boolean = 6,
        /// <summary>自由/由规则推断。</summary>
        free,
    }
    /// <summary>
    /// 列在导入配置中的角色：匹配 Excel、函数默认值、计算列、下拉选择等。
    /// </summary>
    public enum columnType
    {
        /// <summary>未指定列类型。</summary>
        none = 0,
        /// <summary>按列名或规则与 Excel 列匹配。</summary>
        match = 1,
        /// <summary>使用内置或配置的函数生成值（如 newid）。</summary>
        function = 2,
        /// <summary>计算列，由表达式或依赖列推算。</summary>
        reckon = 3,
        /// <summary>来自码表/下拉查询的选择值。</summary>
        select = 4,
        /// <summary>
        /// 固定值，对应前端设置的fixed
        /// </summary>
        fix = 5,
        /// <summary>固定单元格坐标取值。</summary>
        cell = 6,
        /// <summary>动态列（如交叉表展开）。</summary>
        dynamic = 7,
        /// <summary>表头信息列。</summary>
        head = 10,//表头信息列
        /// <summary>动态列模式下当前焦点对应的表头列。</summary>
        focusHead = 11,//动态列的当前表头
    }
}
