// 基础功能说明：

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.meta
{
    /// <summary>
    /// 带有深度编码的树节点
    /// </summary>
    public class DeepTreeNode
    {
        /// <summary>
        /// 节点主键
        /// </summary>
        public string PK;
        /// <summary>
        /// 节点的父级组件
        /// </summary>
        public string parentKey;

        public string name;

        public int minBorder;

        public int maxBorder;

        public int deepId;

        public DeepTreeNode parent;

        public List<DeepTreeNode> children;

        public DataRow data;
        /// <summary>
        /// 开始深度遍历
        /// </summary>
        /// <param name="startNum"></param>
        /// <param name="BrotherGap"></param>
        /// <returns></returns>
        public int calcDeep(int startNum,int BrotherGap=0) {
            this.minBorder = startNum;
            var nowNum = startNum;
            //深度优先，先处理子级
            if (children != null && children.Count > 0)
            {
                foreach (DeepTreeNode child in children)
                {
                    //每进入一个兄弟节点。添加步距；
                    nowNum += BrotherGap;
                    nowNum = child.calcDeep(nowNum);
                }
            }

            //所有子级处理好后，处理自己
            this.maxBorder = nowNum;
            nowNum++;
            this.deepId = nowNum;
            return nowNum;
        }


        public static List<DeepTreeNode> readData(DataTable dt, string pk, string parentOID)
        {

            var nodeMap = new Dictionary<string, DeepTreeNode>();
            foreach (DataRow row in dt.Rows)
            {
                var oid = row[pk].ToString().ToLower();
                var pOID = row[parentOID].ToString().ToLower();
                var node = new DeepTreeNode();
                node.PK = oid;
                node.parentKey = pOID;
                node.data = row;
                nodeMap[oid] = node;
            }

            var res = new List<DeepTreeNode>();

            foreach (var kv in nodeMap)
            {
                //挂载父节点关系
                var pid = kv.Value.parentKey;
                if (!string.IsNullOrWhiteSpace(pid) && nodeMap.ContainsKey(pid))
                {
                    var pa = nodeMap[pid];
                    kv.Value.parent = pa;
                    //挂载子级关系
                    if (pa.children == null)
                    {
                        pa.children = new List<DeepTreeNode>();
                    }
                    pa.children.Add(kv.Value);
                }
                else
                {
                    //找不到父级的，为根级
                    res.Add(kv.Value);
                }
            }

            return res;

        }

        /// <summary>
        /// 深度优先的逐个遍历
        /// </summary>
        /// <param name="onFeel"></param>
        public void feelEach(Action<DeepTreeNode> onFeel) {
            if (children != null && children.Count > 0)
            {
                foreach (DeepTreeNode child in children)
                {
                    child.feelEach(onFeel);
                }
            }
            onFeel(this);
        }

    }
}