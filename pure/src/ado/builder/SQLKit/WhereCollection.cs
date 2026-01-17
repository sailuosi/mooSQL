
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data
{
    /// <summary>
    /// where条件集合
    /// </summary>
    public class WhereCollection
    {
        /// <summary>
        /// 根编织器
        /// </summary>
        public SQLBuilder root;
        /// <summary>
        /// 参数化前缀
        /// </summary>
        public string paramPrefix;
        /// <summary>
        /// 初始化时，自动建立顶级条件盒子。
        /// </summary>
        /// <param name="root"></param>
        /// <param name="paramPrefix"></param>
        public WhereCollection(SQLBuilder root, string paramPrefix)
        {
            this.root = root;
            this.paramPrefix = paramPrefix;
            //建立默认条件分组，并置为当前分组。
            this.topBox = new WhereBracket();
            topBox.root = this.root;
            topBox.isTop = true;
            topBox.parent = null;
            this.CurrentGroup = topBox;
        }





        private int _addCount=0;

        /// <summary>
        /// 顶级条件项盒子
        /// </summary>
        public Boxable topBox;
        /// <summary>
        /// 当前正在操作的条件项盒子
        /// </summary>
        private Boxable CurrentGroup;
        /// <summary>
        /// 包含的where条件项个数
        /// </summary>
        public int Count {
            get { 
                return _addCount;
            }
        }

        public string CurrentConnector
        {
            get { 
                return CurrentGroup.Connector;
            }
        }
        /// <summary>
        /// 弹出上一次添加的条件
        /// </summary>
        /// <returns></returns>
        public int pop() { 
            if(CurrentGroup.children.Count>0)
            {
                CurrentGroup.children.RemoveAt(CurrentGroup.children.Count-1);
                return 1;
            }
            return 0;
            
        }

        public void and() {
            CurrentGroup.Connector = "AND";
        }

        public void or()
        {
            CurrentGroup.Connector = "OR";
        }

        public void not() { 
            CurrentGroup.isNot = true;
        }

        /// <summary>
        /// 经过切面的处理
        /// </summary>
        /// <param name="frag"></param>
        public void addFrag(WhereFrag frag)
        {
           
            //field.
            if (frag.paramed)
            {
                frag.paramKey = string.Format("k{0}g{1}wp{2}",root.paraSeed, paramPrefix, this._addCount);
            }
            if (frag.leftParamed)
            {
                frag.leftParamKey = string.Format("k{0}g{1}wl{2}", root.paraSeed, paramPrefix, this._addCount);
            }

            if (root.Client != null)
            {
                var ok = root.Client.fireBuildWhereFrag(frag, root);
                if (ok)
                {
                    this.CurrentGroup.Add(frag);
                    _addCount++;
                }
            }
            else
            {
                this.CurrentGroup.Add(frag);
                _addCount++;
            }
        }
        /// <summary>
        /// 创建一个新盒子，并进入其中
        /// </summary>
        /// <param name="connector"></param>
        public void sink(string connector="AND") {
            var tar = new WhereBracket();
            tar.root= this.root;
            tar.isTop = false;
            tar.parent = CurrentGroup;
            tar.Connector=connector;
            CurrentGroup.children.Add(tar);
            this.CurrentGroup = tar;
        }
        /// <summary>
        /// 从当前盒子中出来，并进入到上级盒子中，最多可到顶级。
        /// </summary>
        public void rise() {
            if (CurrentGroup.isTop) return;

            CurrentGroup = CurrentGroup.parent;
        }


        /// <summary>
        /// 清空内容
        /// </summary>
        public void Clear() { 
            this.topBox.children.Clear();
            this.topBox.Connector = "AND";
            this.CurrentGroup = topBox;
            this._addCount = 0;
        }
        /// <summary>
        /// 复制
        /// </summary>
        /// <param name="other"></param>
        public void Copy(WhereCollection other)
        {
            this.topBox = other.topBox;
            this._addCount= other._addCount;
        }
        /// <summary>
        /// 不带参数体的写入
        /// </summary>
        /// <param name="paraPrex"></param>
        /// <returns></returns>
        public string BuildWithoutPara(string paraPrex) {
            if (_addCount == 0) return "";
            string conditon = topBox.ToSQL(";noPara;");
            return conditon;

        }

        /// <summary>
        /// 构造条件
        /// </summary>
        /// <returns></returns>
        public string Build()
        {
            if (_addCount == 0) return "";
            string conditon = topBox.ToSQL() ;
            return conditon;
        }
    }
}
