using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mooSQL.excel.context;

namespace mooSQL.excel
{

    public class AZSection
    {
        private List<int> solos = new List<int>();
        private List<IntSect> sects = new List<IntSect>();
        /// <summary>
        /// 整数区间
        /// </summary>
        public AZSection() { }
        /// <summary>
        /// 区间大小
        /// </summary>
        public int Count
        {
            get
            {
                return solos.Count + sects.Count;
            }
        }

        public int? Max() { 
            int res= 0;
            foreach (int i in solos) {
                if (i > res) { 
                    res = i;
                }
            }

            foreach (var sect in sects) {
                if (sect.max != null && sect.max > res) { 
                    res=sect.max.Value;
                }
            }
            return res;
        } 

        /// <summary>
        /// 区间是否包含
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contain(int val)
        {
            if (solos.Contains(val)) return true;
            foreach (var se in sects)
            {
                if (se.Contain(val)) return true;
            }
            return false;
        }
        /// <summary>
        /// 添加一个值到区间
        /// </summary>
        /// <param name="val"></param>
        public void addSolo(int val)
        {
            if (!solos.Contains(val)) solos.Add(val);
        }
        /// <summary>
        /// 遍历区间内的每个封闭区间
        /// </summary>
        /// <param name="doing"></param>
        public void ForEachClosed(Action<int> doing)
        {
            List<int> done = new List<int>();
            foreach (var solo in solos)
            {
                if (done.Contains(solo) == false)
                {
                    doing(solo);
                    done.Add(solo);
                }
            }
            foreach (var set in sects)
            {
                if (set.closed)
                {
                    for (int i = (int)set.min; i <= set.max; i++)
                    {
                        if (done.Contains(i) == false)
                        {
                            doing(i);
                            done.Add(i);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 读取配置
        /// </summary>
        /// <param name="con"></param>
        public void readConfig(string con)
        {
            if (con.Contains(","))
            {
                var co = con.Split(',');
                foreach (var c in co)
                {
                    readSpan(c);
                }
            }
            else
            {
                readSpan(con);
            }
        }

        private void readSpan(string sp)
        {
            if (sp.Contains("-"))
            {

                var sets = sp.Split('-');
                int startRow = -1;
                int endRow = -1;
                if (sets.Length > 0)
                {
                    // && int.TryParse(sets[0], out startRow)
                    startRow= ExcelUntil.parCellColCode(sets[0]);
                }
                else { startRow = -1; }
                if (sets.Length > 1 )
                {
                    endRow = ExcelUntil.parCellColCode(sets[1]);
                }
                else { endRow = -1; }
                this.sects.Add(new IntSect(startRow, endRow));
            }
            else
            {
                int rowi=  ExcelUntil.parCellColCode(sp);

                this.solos.Add(rowi);
                
            }
        }
    }
}
