using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.data.builder;

namespace mooSQL.data
{
    /// <summary>
    /// merge语句的条件分支
    /// </summary>
    public class MergeBranch
    {
        public MergeIntoBuilder root;
        /// <summary>
        /// 条件部分，放在这里。
        /// </summary>
        public SQLBuilder Condtion { get; set; }
        /// <summary>
        /// 更新部分，放在这里。
        /// </summary>
        public SQLBuilder SetPart { get; set; }

        /// <summary>
        /// 是否匹配，默认为null。
        /// </summary>
        public bool? IsMatched { get; set; }

        public MergeAction ThenAction { get; set; }


        public MergeBranch(MergeIntoBuilder parent) { 
            this.root = parent;
            this.SetPart= parent.parent.getBrotherBuilder();
        }
        /// <summary>
        /// 设置匹配条件，默认为null。
        /// </summary>
        /// <returns></returns>
        public MergeBranch whenMatched()
        {
            this.IsMatched = true;
            return this;
        }
        public MergeBranch whenNotMatched()
        {
            this.IsMatched = false;
            return this;
        }

        public MergeBranch useSQL() {
            if (this.Condtion == null)
            {
                this.Condtion = root.parent.getBrotherBuilder();
            }
            return this;
        }
        /// <summary>
        /// 设置分支条件，不设置则无条件
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MergeBranch whenMatched(Action<SQLBuilder> action) {
            useSQL();


            action(this.Condtion);
            this.IsMatched = true;
            return this;
        }
        public MergeBranch whenNotMatched(Action<SQLBuilder> action)
        {
            if (this.Condtion == null)
            {
                this.Condtion = root.parent.getBrotherBuilder();
            }

            action(this.Condtion);
            this.IsMatched = false;
            return this;
        }

        public MergeIntoBuilder thenInsert(Action<SQLBuilder> action) {
            if (this.SetPart == null) {
                this.SetPart = root.parent.getBrotherBuilder();
            }
            action(this.SetPart);
            this.ThenAction = MergeAction.insert;
            return root;
        }

        public MergeIntoBuilder thenUpdate(Action<SQLBuilder> action)
        {
            if (this.SetPart == null)
            {
                this.SetPart = root.parent.getBrotherBuilder();
            }
            action(this.SetPart);
            this.ThenAction = MergeAction.update;
            return root;
        }

        public MergeIntoBuilder thenDelete()
        {

            this.ThenAction = MergeAction.update;
            return root;
        }

        /// <summary>
        /// 初步编译成片段。
        /// </summary>
        /// <returns></returns>
        public FragMergeWhen toFrag()
        {
            var frag = new FragMergeWhen();
            frag.matched = this.IsMatched == true;
            frag.action = this.ThenAction;
            if (this.ThenAction == MergeAction.insert)
            {
                frag.fieldInner = this.SetPart.current.buildInsertFields();
                frag.valueInner = this.SetPart.current.buildInsertValOne();
            }
            else if (this.ThenAction == MergeAction.update)
            {
                frag.setInner = this.SetPart.current.buildSetFragCur();
            }
            if (this.Condtion != null) { 
                frag.whenWhere = this.Condtion.buildWhereContent();
            }
            return frag;
        }
    }
    /// <summary>
    /// merge语句的动作类型
    /// </summary>
    public enum MergeAction { 
        insert=0,
        update=1,
        delete=2
    }
}
