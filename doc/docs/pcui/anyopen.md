点击弹窗
1

引入
```` js
import anyopen from "@/ui/panels/anyopen.vue"
````

模版
```` html
<anyopen :width="width" type="popover">
    <template #default="dp">
        <el-tag type="primary" @click="dp.open">打开</el-tag>
    </template>
    <template #header="hp">
        
    </template>
    <template #body="bp">

    </template>
    <template #footer="fp">
        
    </template>
</anyopen>
````

2 属性

 with -- 弹窗的宽度

 type -- 弹窗的类型，支持 popover dialog drawer base

