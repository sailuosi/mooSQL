表单卡片
1 基本说明

    具有卡片外观、标题、工具栏、内容体的区块

2 基本使用

引入
```` js
import formcard from "@/ui/panels/formcard.vue"
````
案例
```` html
<formcard title="关联印鉴信息" class="mb8">
    <template #toolbar>
        <livevcbar name="VC_SK_AccSealList" :bpo="bpo" />
    </template>
    <livevc name="VC_SK_AccSealList"  :bpo="bpo" height="50vh" :showTitle="false"></livevc>
</formcard>
````

3. 属性说明

  title --标题

   插槽

  #toolbar  ---标题栏右侧工具栏的内容；

  #default  --- 内容体