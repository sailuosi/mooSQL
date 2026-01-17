using mooSQL.linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.data.clip
{
    internal class ClipWhereVisitor
    {
        private DBInstance DB;

        private ClipConditionVisitor whereExpressionVisitor;

        private SQLClip clip;
        /// <summary>
        /// 创建
        /// </summary>
        /// <param name="DB"></param>
        public ClipWhereVisitor(DBInstance DB, SQLClip clip)
        {
            this.DB = DB;

            var context = new FastCompileContext();
            context.initByBuilder(clip.Context.Builder);

            this.whereExpressionVisitor = new ClipConditionVisitor(context, clip);
            this.clip = clip;
        }

        public void CopyLayer(LayerContext layer)
        {
            this.whereExpressionVisitor.Context.CurrentLayer.Copy(layer);
        }

        /// <summary>
        /// 访问并获取结果
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Expression Visit(Expression expression)
        {
            var tar = this.whereExpressionVisitor.Visit(expression);
            //var exp = expression.ToString();
            //var parameters = expression.GetParametersValue();
            return tar;
        }
    }
}
