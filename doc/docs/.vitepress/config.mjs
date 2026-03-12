import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "麦穗仓库",
  description: "积小步以致千里",
  base:'/devdoc/',
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: '主页', link: '/' },
      { text: 'mooSQL文档',
        items: [
          { text: '更新日志', link: '/SQL/configs/updatelog' },
          { text: '指南', link: '/moohelp/start/about' },
          { text: 'API', link: '/SQL/basis/initconfig' },
        ]
      },
      { text: '开发平台',
        items: [
          { text: '开发平台', link: '/uplat/uplatindex' },
          { text: 'U8', link: '/ucml/u8/bcgeneral' },
          { text: 'U7', link: '/ucml/u7/vueformu7' },
        ]
      },      
      
      { text: '.Net', link: '/net6/net6index' },
      { text: '制品库',
        items: [
          { text: 'webUI组件', link: '/pcui/pcuiindex' },
          { text: '流程', link: '/ccflow/ccftou8' },
          { text: '大模型', link: '/AI/llmcash' },
        ]
      },

      
    ],

    sidebar: {
      '/moohelp/':[
        {
          text: '开始',
          items: [
            { text: '简介', link: '/moohelp/start/about' },
            { text: '快速上手', link: '/moohelp/start/quickstart' },
          ]
        },
        {
          text: '基础文档',
          items: [
            //{ text: '入门', link: '/moohelp/query/basicquery' },
            { text: '插入', link: '/moohelp/sqlbasis/insertbasis' },
            { text: '修改', link: '/moohelp/sqlbasis/updatebasis' },
            { text: '删除', link: '/moohelp/sqlbasis/deletebasis' },
          ]
        },
        {
          text: '查询',
          items: [
            { text: '基础查询', link: '/moohelp/query/basicquery' },
            { text: '翻页查询', link: '/moohelp/query/pagingquery' },
            { text: '多表查询', link: '/moohelp/query/joinquery' },
            { text: '子查询', link: '/moohelp/query/subquery' },
            { text: '查询条件', link: '/moohelp/query/wherequery' },
          ]
        },
        {
          text: '更多特性',
          items: [
            { text: '合并数据', link: '/moohelp/morelv/mergeintobasis' },
            { text: '事务', link: '/moohelp/morelv/transaction' },
          ]
        },
      ],
      '/SQL/':[
        {
          text: '基础API',
          collapsed: false,
          items: [
            { text: '更新日志', link: '/SQL/configs/updatelog' },
            { text: '初始化配置', link: '/SQL/basis/initconfig' },
            { text: 'DBCash', link: '/SQL/basis/DBCash' },
            { text: 'SQLBuilder', link: '/SQL/basis/SQLBuilder' },
            { text: 'BatchSQL', link: '/SQL/basis/batchSQLbase' },
            { text: 'SQLBuilder案例', link: '/SQL/basis/sqlBuilderdemo' },
            { text: 'BulkCopy', link: '/SQL/basis/BulkCopybase' },
            { text: '事务', link: '/moohelp/morelv/transaction' },
          ]
        },
        {
          text: '高层API',
          collapsed: false,
          items: [
            { text: 'DbBus --表达式', link: '/SQL/high/expression' },
            { text: 'SooReposity ---仓储', link: '/SQL/high/repository' },
            { text: 'SooWorkOfUnit', link: '/SQL/high/unitofwork' },
            { text: 'SQLClip', link: '/SQL/high/sqlclip' },
            { text: 'LLMCash --大模型', link: '/AI/llmcash' },
          ]
        },
        {
          text: '工具类',
          collapsed: false,
          items: [
            { text: '类型处理', link: '/SQL/utils/typeutils' },
            { text: '自定义集合', link: '/SQL/utils/collection' },
          ]
        },
        {
          text: '权限',
          collapsed: false,
          items: [
            { text: '权限基础', link: '/SQL/auth/authbase' },
            { text: '生命周期', link: '/SQL/auth/authlife' },
          ]
        }
      ],
      '/ucml/':[
        {
          text: 'u8',
          items: [
            { text: 'BC常用', link: '/ucml/u8/bcgeneral' },
            { text: 'BPO常用', link: '/ucml/u8/bpogeneral' },
            { text: '接口开发', link: '/ucml/u8/serveapi' },
            { text: 'VC自定义', link: '/ucml/u8/vccustm' },
            { text: 'BPO自定义', link: '/ucml/u8/bpocustom' },
            { text: '弹窗', link: '/ucml/u8/openlayer' },
          ]
        },
        {
          text: 'U7目录',
          items: [
            { text: 'vue表单', link: '/ucml/u7/vueformu7' },
            { text: 'Excel导入', link: '/ucml/u7/excelinto' },
            { text: 'Excel导出', link: '/ucml/u7/exceloutput' },
            { text: '增删改', link: '/ucml/u7/u7vcrud' },
            { text: '弹窗', link: '/ucml/u7/u7opendialog' },
            { text: 'VC配置', link: '/ucml/u7/vcconfig' },
            { text: '代码生成脚本', link: '/ucml/u7/codegene' },
            { text: '接口调用', link: '/ucml/u7/invokeapi' },
          ]
        }
      ],
      '/uplat/':[
        {
          text: '开发平台目录',
          items: [
            { text: '概述', link: '/uplat/uplatindex' },
          ]
        },
        {
          text: '新版指南',
          items: [
            { text: '要点', link: '/uplat/study/u8breaks' },
          ]
        }
      ],
      '/ccflow/':[
        {
          text: '目录',
          items: [
            { text: '集成到U8', link: '/ccflow/ccftou8' },
            { text: 'U7vue版启动器', link: '/ccflow/startoru7v' },
          ]
        }
      ],
      '/net6/':[
        {
          text: '目录',
          items: [
            { text: '首页', link: '/net6/net6index' },
            { text: '常用命令', link: '/net6/cmdoffen' },
            { text: '常用注解', link: '/net6/nearbyattr' },
          ]
        }
      ],
      '/AI/':[
        {
          text: '目录',
          items: [
            { text: '首页', link: '/AI/llmcash' },
          ]
        }
      ],
      '/pcui/':[
        {
          text: '目录',
          items: [
            { text: '首页', link: '/pcui/pcuiindex' },
            { text: 'navpanel', link: '/pcui/navpanel' },
            { text: 'formcard', link: '/pcui/formcard' },  
            { text: 'anyopen', link: '/pcui/anyopen' },
          ]  
        }
      ]
    },

    socialLinks: [
      { icon: 'gitlab', link: 'http://137.12.207.30:8088/maplehan/u8common.git' }
    ]
  }
})

/**
export default {
  themeConfig: {
    sidebar: {
      // 当用户位于 `guide` 目录时，会显示此侧边栏
      '/guide/': [
        {
          text: 'Guide',
          items: [
            { text: 'Index', link: '/guide/' },
            { text: 'One', link: '/guide/one' },
            { text: 'Two', link: '/guide/two' }
          ]
        }
      ],

      // 当用户位于 `config` 目录时，会显示此侧边栏
      '/config/': [
        {
          text: 'Config',
          items: [
            { text: 'Index', link: '/config/' },
            { text: 'Three', link: '/config/three' },
            { text: 'Four', link: '/config/four' }
          ]
        }
      ]
    }
  }
}
 * 
 * 
export default {
  themeConfig: {
    sidebar: [
      {
        text: 'Section Title A',
        collapsed: false,
        items: [...]
      }
    ]
  }
}
 */