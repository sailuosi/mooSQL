基础
1 基本使用

引入
```` js
import navpanel from "@/ui/panels/navpanel.vue"
````
基础使用
```` html
<navpanel ref="navpanelRef" :navs="navs" :anchors="anchors" :height="p.height" style="height:100%;" class="fmnav">
</navpanel>
````
```` js
 const navs= ref([
    { title: "开户登记", id: "navlv1", anchor: "navlv1", icon: "fa fa-map-marker", children: [] },
    { title: "登记详情", id: "navlv2", anchor: "navlv2", icon: "fa fa-map-signs", children: [] },
    { title: "关联印鉴", id: "navlv3", anchor: "navlv3", icon: "fa fa-map-signs", children: [] },
    { title: "附件信息", id: "navlv4", anchor: "navlv4", icon: "fa fa-map-signs", children: [] },
]);

const anchors= ref([
    "navlv1","navlv2","navlv3","navlv4"
]);
````

2 参数说明

navs : 导航菜单列表

anchors ：定位锚点的id列表

height:  导航内容窗体的高度