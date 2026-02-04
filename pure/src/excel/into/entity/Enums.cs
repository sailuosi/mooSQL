

namespace mooSQL.excel.context
{
    public enum breakPoint
    {
        none = 0,
        excel = 9,
        excelRowContine = 1,
        excelRowBreak = 2,
        tableContinue = 3,
        tableBreak = 4,
        clear = 6,
    }
    public enum writeMode
    {
        none = 0,
        insert,
        update,
        write,
        check
    }
    public enum checkFailAct
    {
        none = 0,
        self,
        row,
        silent,
        next,
        before,
        excel,

    }
    public enum valueType
    {
        none = 0,
        stringi = 1,
        number = 2,
        date = 3,
        guid = 4,
        boolean = 6,
        free,
    }
    public enum columnType
    {
        none = 0,
        match = 1,
        function = 2,
        reckon = 3,
        select = 4,
        /// <summary>
        /// 固定值，对应前端设置的fixed
        /// </summary>
        fix = 5,
        cell = 6,
        dynamic = 7,
        head = 10,//表头信息列
        focusHead = 11,//动态列的当前表头
    }
}
