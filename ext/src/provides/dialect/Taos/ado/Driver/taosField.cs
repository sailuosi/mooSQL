
using mooSQL.data.taos.Driver;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace mooSQL.data.taos
{
    /// <summary>
    /// taosField struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4, Size = 72)]
    public struct taosField
    {
        /// <summary>
        /// _name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        public byte[] _name;
        /// <summary>
        /// type
        /// </summary>
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public byte type;
        //  int32_t bytes;
        /// <summary>
        /// bytes
        /// </summary>
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public int bytes;
#if NET5_0_OR_GREATER
        /// <summary>
        /// Name
        /// </summary>
        /// <summary>
        /// Name
        /// </summary>
        public string Name => Encoding.UTF8.GetString(_name, 0, _name.AsSpan().IndexOf((byte)0));
#else
        public string Name => Encoding.UTF8.GetString(_name, 0, Array.IndexOf( _name,(byte)0));
#endif
        /// <summary>
        /// DataType
        /// </summary>
        /// <summary>
        /// DataType
        /// </summary>
        public TDengineDataType DataType => (TDengineDataType)type;
        /// <summary>
        /// CrlType
        /// </summary>
        /// <summary>
        /// CrlType
        /// </summary>
        public Type CrlType => DataType.ToCrlType();
        /// <summary>
        /// TypeName
        /// </summary>

        public string TypeName =>DataType.ToTypeName();
        /// <summary>
        /// Size
        /// </summary>
     
        public int  Size => bytes;
    }
}