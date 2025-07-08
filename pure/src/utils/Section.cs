using System;
using System.Collections.Generic;


namespace mooSQL.utils
{

    /// <summary>
    /// 区间类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Section<T>
    {
        /// <summary>
        /// 
        /// </summary>
        public List<T> solos = new List<T>();
        /// <summary>
        /// 
        /// </summary>
        public List<Sect<T>> sects = new List<Sect<T>>();
        /// <summary>
        /// 
        /// </summary>
        public Func<string, T> parseString;
        /// <summary>
        /// 区间的缺省值。
        /// </summary>
        public T invalidValue;
        /// <summary>
        /// 
        /// </summary>
        public Func<T, bool> isValid;
        /// <summary>
        /// 比较2个值的大小，如果-1，则小于，0 =，1>
        /// </summary>
        public Func<T, T, int> compare;
        /// <summary>
        /// 左边是闭区间
        /// </summary>
        public bool containLeft = true;
        /// <summary>
        /// 右侧是否是闭区间
        /// </summary>
        public bool containRight = true;
        /// <summary>
        /// 
        /// </summary>
        public Section() { }
        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get
            {
                return solos.Count + sects.Count;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Contain(T val)
        {
            if (solos.Contains(val)) return true;
            foreach (var se in sects)
            {
                if (se.Contain(val)) return true;
            }
            return false;
        }
        /// <summary>
        /// 添加一个
        /// </summary>
        /// <param name="val"></param>
        public void addSolo(T val)
        {
            if (!solos.Contains(val)) solos.Add(val);
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
                if (sets.Length > 1)
                {
                    if (string.IsNullOrWhiteSpace(sets[0]) && string.IsNullOrWhiteSpace(sets[1]))
                    {
                        return;
                    }
                    T valmin = parseString(sets[0]);
                    T valMax = parseString(sets[1]);
                    var sect = new Sect<T>(valmin, valMax);
                    sect.compare = this.compare;
                    sect.containLeft = this.containLeft;
                    sect.containRight = this.containRight;
                    sect.isValid = this.isValid;
                    if (isValid(valmin) || isValid(valMax))
                    {
                        this.sects.Add(sect);
                    }
                }
            }
            else
            {
                T val = parseString(sp);
                if (solos.Contains(val) == false && isValid(val))
                {
                    this.solos.Add(val);
                }
            }
        }
    }
}
