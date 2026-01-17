
using System;
using System.Collections.Generic;

namespace mooSQL.data
{
    /// <summary>
    /// 对列进行赋值的的键值对
    /// </summary>
    public class ColPair {
        /// <summary>
        /// 列名
        /// </summary>
        public string colname;

        private object _value;
        public object value
        {
            get { 
                return _value;
            }
            set
            {
                if (!TypeUntil.isSQLParaType(value)) { 
                    _value=value.ToString();
                    return ;
                }
                _value = value;
            }
        }

        /// <summary>
        /// 参数化的键值
        /// </summary>
         
        public string paramKey = "";
        /// <summary>
        /// 是否自动判断值是否安全，如果是安全的值，比如 int,double,等自动不再进行参数化，用于压缩参数数量。
        /// </summary>
        public bool autoShun=false;
        /// <summary>
        /// 默认开启参数化
        /// </summary>
        public bool paramed = true;

        /**
         * 可用于构建更新语句
         */
        public bool updatable = true;
        /**
         * 可用于构建插入语句
         */
        public bool insetable = true;
    }
    /// <summary>
    /// set语句定义碎片
    /// </summary>
    public class SetFrag
    {
        /// <summary>
        /// 参数化的参数名前缀
        /// </summary>
        public string paraPrefix { get; set; }
        /// <summary>
        /// 字段名
        /// </summary>
        public string key;
        ///// <summary>
        ///// 单个映射值，用于update 或者单个 insert
        ///// </summary>
        //public object value;

        /// <summary>
        /// 当前的set列在全部set字段中的索引
        /// </summary>
        public int fieldIndex=0;
        /// <summary>
        /// 当外界不传入行记录指针时的默认指针
        /// </summary>
        public int defaultIndex = 0;
        /// <summary>
        /// 按照行号映射的多个值，用于批量插入。
        /// </summary>
        public Dictionary<int, ColPair> values = new Dictionary<int, ColPair>();
        /// <summary>
        /// 参数值类型，定义时，提供自动进行参数值类型转换的功能。
        /// </summary>
        public Type valueType;
        /**
         * 可用于构建更新语句
         */
        public bool updatable = true;
        /**
         * 可用于构建插入语句
         */
        public bool insetable = true;

        /// <summary>
        /// 字段的set碎片，必须有字段名作为key
        /// </summary>
        /// <param name="key"></param>
        public SetFrag(string key)
        {
            this.key = key;
        }


        /// <summary>
        /// 设置列值，必须指定行索引，因此本方法只能被持有行索引信息的 SqlGroup类调用。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="val"></param>
        /// <param name="valType"></param>
        /// <param name="paramed"></param>
        /// <param name="updatable"></param>
        /// <param name="insetable"></param>
        /// <param name="type"></param>
        public void setValue(int index,object val,Type valType=null, bool paramed = true, bool updatable=true, bool insetable = true,Type type=null)
        {
            if(valType !=null)
            {
                this.valueType = valType;
            }
            var pair = new ColPair();
            pair.value = val;
            pair.paramed = paramed;
            pair.updatable = updatable;
            pair.insetable = insetable;
            if (pair.paramed && (pair.paramKey == null || pair.paramKey == ("")))
            {
                pair.paramKey = this.paraPrefix + "_" + fieldIndex + "_" + index;
            }
            values[index] = pair;
        }

        public object getValue(int index)
        {
            if (!values.ContainsKey(index)) return null;
            return values[index].value;
        }

    }



}