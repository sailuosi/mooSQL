

using System;
using System.Data.Common;


namespace mooSQL.data.taos
{
    /// <summary>
    ///     Represents a Taos error.
    /// </summary>
    public class TaosException : DbException
    {
        TaosErrorResult _taosError;
        /// <summary>
        /// TaosException type.
        /// </summary>

        public TaosException(TaosErrorResult taosError) : base(taosError.Error, null)
        {
            _taosError = taosError;
            base.HResult = _taosError.Code;
        }
        /// <summary>
        /// TaosException type.
        /// </summary>

        public TaosException(TaosErrorResult taosError, Exception ex) : base(taosError.Error, ex)
        {
            _taosError = taosError;
            base.HResult = _taosError.Code;
        }
      /// <summary>
      /// Message
      /// </summary>
 


   

      
        public override string Message => _taosError?.Error;
 /// <summary>
 /// ErrorCode
 /// </summary>
 
        public override int ErrorCode =>   (int) _taosError?.Code;
        /// <summary>
        /// 根据命令文本与 Taos 错误结果抛出异常。
        /// </summary>
        /// <param name="_commandText">相关 SQL/命令文本，写入异常 Data。</param>
        /// <param name="taosError">Taos 错误结果。</param>
        public static void ThrowExceptionForRC(string _commandText, TaosErrorResult taosError)
        {
            var te = new TaosException(taosError);
            te.Data.Add("commandText", _commandText);
            throw te;
        }

        /// <summary>
        /// 根据 Taos 错误结果抛出异常。
        /// </summary>
        /// <param name="taosError">Taos 错误结果。</param>
        public static void ThrowExceptionForRC(TaosErrorResult taosError)
        {
            var te = new TaosException(taosError);
            throw te;
        }

        /// <summary>
        /// 根据命令文本与连接句柄读取错误信息并抛出异常。
        /// </summary>
        /// <param name="_commandText">相关 SQL/命令文本。</param>
        /// <param name="_taos">Taos 连接句柄。</param>
        public static void ThrowExceptionForRC(string _commandText, IntPtr _taos)
        {
            var te = new TaosException(new TaosErrorResult() { Code = TDengine.ErrorNo(_taos), Error = TDengine.Error(_taos) });
            te.Data.Add("commandText", _commandText);
            throw te;
        }

        /// <summary>
        /// 根据预处理语句错误信息抛出异常。
        /// </summary>
        /// <param name="_step">出错步骤标识。</param>
        /// <param name="_commandText">相关命令文本。</param>
        /// <param name="_code">错误码。</param>
        /// <param name="_stmt">语句句柄。</param>
        public static void ThrowExceptionForStmt(string _step,string _commandText,int _code, IntPtr _stmt)
        {
            var te = new TaosException(new TaosErrorResult() { Code = _code, Error = TDengine.StmtErrorStr(_stmt) });
            te.Data.Add("commandText", _commandText);
            te.Data.Add("step", _step);
            throw te;
        }

        /// <summary>
        /// 根据连接句柄读取错误信息并抛出异常。
        /// </summary>
        /// <param name="_taos">Taos 连接句柄。</param>
        public static void ThrowExceptionForRC(IntPtr _taos)
        {
            var te = new TaosException(new TaosErrorResult() { Code = TDengine.ErrorNo(_taos), Error = TDengine.Error(_taos) });
            throw te;
        }

        /// <summary>
        /// 根据错误码、消息与内部异常抛出 <see cref="TaosException"/>。
        /// </summary>
        /// <param name="code">错误码。</param>
        /// <param name="message">错误消息。</param>
        /// <param name="ex">内部异常。</param>
        public static void ThrowExceptionForRC(int code, string message, Exception ex)
        {
            var te = new TaosException(new TaosErrorResult() { Code = code, Error = message }, ex);
            throw te;
        }
    }
}
