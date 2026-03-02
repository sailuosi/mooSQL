

using mooSQL.data;
using mooSQL.data.linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace mooSQL.linq
{
    /// <summary>
    /// 求取字段的信息的表达式
    /// </summary>
    internal class FieldVisitor : EntityMemberVisitor
    {

        public FieldVisitor(FastCompileContext context, bool srcNick) : base(context)
        {
            this.srcNick = srcNick;
        }

        protected override EntityColumn GetFieldCol(MemberInfo prop, Expression expression)
        {
            try {
                //在fast模式下，只有来自形参的函数访问可以转换为字段，其它都视为普通值
                if (expression is MemberExpression memExp && memExp.Expression.NodeType != ExpressionType.Parameter) {
                    return null;
                }

                var tar= this.Builder.DBLive.client.EntityCash.getFieldCol(prop,Context.EntityType);
                if (tar == null) {
                    //处理未能正确解析DbBus下的select在Group下使用的问题
                    bool isGroupName1 = prop.DeclaringType.Name.StartsWith("IGrouping`");
                    bool isGroupName2 = prop.DeclaringType.IsGenericType && prop.DeclaringType.GetGenericTypeDefinition() == typeof(IGrouping<,>);
                    if ((isGroupName1||isGroupName2) && prop.Name=="Key")
                    {
                        //此时为group by下的select成员
                        var groupedCols = this.Context.TopLayer.Current.current.groupbyPart;
                        foreach (var col in groupedCols) {
                            Context.TopLayer.Current.select(col);
                        }
                        return null;
                    }
                    throw new Exception($"字段{prop.Name}找不到对应的数据库字段，请检查实体类的特性标注是否正确！当前实体类型为{prop.DeclaringType.Name}！");
                }

                
                var field= new ParsedField();
                field.Column = tar;
                field.Member = prop;
                field.Exp = expression;
                field.EntityType = tar.belongTable.Type;
                
                if (expression is MemberExpression mxp) {
                    if (mxp.Expression is ParameterExpression pxp ) {
                        field.CallerNick = pxp.Name;
                        if (pxp.Type == field.EntityType) { 
                            field.EntityAlias= pxp.Name;
                        }
                    }
                
                }

                this.ParsedFields.Add(field);
                return tar;
            }
            catch (Exception e) {
                return null;
            }
        }
    }
}
