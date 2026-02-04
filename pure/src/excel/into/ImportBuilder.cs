// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.excel
{
    public class ImportBuilder
    {

        public InConfig config(
            // 数据查询模式，默认为local:local/database。一般不需要修改。 分为local内存查询和database数据库查询。内存查询优点是速度快，缺点是准备时间长，数据量越大越明显。数据库查询优点是无前摇，但执行写入速度慢。

            string checkMode = "local",

            // 输出日志时输出[提示]类的信息，默认NO:YES/NO。因提示性的涉及的太多，提示信息过多会导致用户难以找到关键性的信息。

            string logtips = "NO",

            // 默认NO:YES/NO,是否启用批量更新,启用时，将使用StrSQLMaker类的UpdateTable进行批量提交，否则，使用dealUpdate方法生成的SQL逐行更新

            string batchUpdate = "NO",

            // 标题，导入日志的数据库表中，将以它作为标题列的值。

            string title = "导入培训班",

            // 必填，其值为列集合中的某一列的key。行日志下的列标识，输出列信息

            string outInfoCol = "idcard",

            // 默认insert:write/insert/update。分别指代执行同时更新和插入，只插入，只更新。

            string mode = "insert",

            // 默认为 1 ，格式为数字。导入表格的标题所在的excel行号，其值与excel中所显示的行号相同（从1开始）。多个以逗号分隔，如果"1,2,3,5"。

            string titleRowNum = "1",

            // 默认为 2-，即从第2行开始，支持类似于 3,4,9-15,56-这样的格式，即区间以“-”分隔，其值与excel中所显示的行号

            string dataRowNum = "2-",

            // excel导入模板的地址，仅为vue版调起页面专用。aspx版调起页面无效。../../PXGL/ExcelModels/班级管理培训班导入模板.xlsx

            string demoUrl = "",

            // 导入的帮助与提示信息，格式为html文本。仅为vue版调起页面专用。aspx版调起页面无效。本导入模板为固定格式，请不要修改列头、数据体的行位置，本模板用来导入培训中心->培训办班->班级管理  的计划外培训班，计划内的培训班请在创建办班计划后进行创建。<br/>第一列分类，必填，是指系统中左侧的分类中分类名，请确保培训类别名称与分类中的某个分类一致。培训时间，即培训开始时间，是系统判断培训班所在年月的依据，请务必填写！<br/>年度、期次，填写数字即可。也可以按照标准的“2020年”,“第1期”这样的格式写，任选。<br/>主办单位自动设置当前用户的机构，不需要输入，因此，禁止导入非自己管理的培训班。

            string note = "",

            // 传入参数为1个，1参 ExcelRead  



            // 必填，复合一级属性。为导入所需的目标数据库表，包含导入表、查询表。



            // 是否回写excel

            string saveMsgToExcel = "",

            // 匹配时是否忽视大小写

            string ignoreCase = "",

            string logColNum = "",

            string titleScanScope = "",

            List<string> titleScanReg = null
            )
        {
            //  InBPOMethod beforeSave,
            //InBPOMethod afterSave,
            // BPO服务端的导入配置
            //      List<InTable> tables,
            // 与表无关的写入列集合。其语法与表内的相同。只是没有field属性。而表内列集合，必须设置field属性。
            //List< InField > KVs,
            //    List<InField> shareCol,
            //InBPOMethod loadConfig,
            var config = new InConfig();
            return config;
        }


    }

}