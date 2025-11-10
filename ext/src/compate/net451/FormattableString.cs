#if NET451
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq
{
    /// <summary>
    /// 兼容
    /// </summary>
    public sealed class FormattableString : IFormattable
    {
        private readonly string _format;

        private readonly object[] _arguments;

        public string Format { 
            get { return _format; }
        }
        public object[] GetArguments() { 
            return _arguments;
        }
        public FormattableString(string format, object[] arguments)
        {
            _format = format;
            _arguments = arguments;
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString(formatProvider);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, _format, _arguments);
        }

        public static string Invariant(FormattableString formattable)
        {
            if (formattable == null)
            {
                throw new ArgumentNullException("formattable");
            }

            return formattable.ToString(CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }
    }


    public static class FormattableStringFactory
    {
        public static FormattableString Create(string format, params object[] arguments)
        {
            return new FormattableString(format, arguments);
        }
    }
}

#endif