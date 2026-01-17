
using mooSQL.data.builder;

using System.Text;
using System.Text.RegularExpressions;


namespace mooSQL.data
{
    public partial class Dialect
    {

        public string BuildInsertOne(FragSQL frag) { 
            var sql= new StringBuilder();
            //INSERT INTO table_name VALUES (value1,value2,value3,...);
            sql.Append("INSERT INTO ");
            sql.Append(frag.insertInto+ " VALUES ");
            if (Regex.IsMatch(frag.insertValue,@"\s*[(].*[)]\s*")) {
                //包含开始
                sql.Append(frag.insertValue);
            }
            return sql.ToString();
        }


        protected DBVersion CheckVersion() {
            if (this.Versions != null) {
                //倒序检查
                var verCode = this.db.version;
                var verNum = this.db.versionNumber;
                for (int i = this.Versions.Count - 1; i >= 0; i--) {
                    var v=this.Versions[i];
                    if (verCode.HasText() && Regex.IsMatch(verCode, v.MatchRegex)) {
                        return v;
                    }
                    //设置了版本号，
                    if (verNum > 0 && v.VersionNumber <= verNum) { 
                        return v;
                    }
                }
            
            }
            return null;
        }
    }
}
