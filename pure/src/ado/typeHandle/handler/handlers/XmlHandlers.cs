using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace mooSQL.data
{
    /// <summary>
    /// XML 类型处理器的抽象基类
    /// </summary>
    /// <typeparam name="T">XML 类型</typeparam>
    internal abstract class XmlTypeHandler<T> : StringTypeHandler<T>
    {
        public override void SetValue(IDbDataParameter parameter, T value)
        {
            base.SetValue(parameter, value);
            parameter.DbType = DbType.Xml;
        }
    }

    /// <summary>
    /// XmlDocument 类型处理器
    /// </summary>
    internal sealed class XmlDocumentHandler : XmlTypeHandler<XmlDocument>
    {
        protected override XmlDocument Parse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        protected override string Format(XmlDocument xml) => xml.OuterXml;
    }

    /// <summary>
    /// XDocument 类型处理器
    /// </summary>
    internal sealed class XDocumentHandler : XmlTypeHandler<XDocument>
    {
        protected override XDocument Parse(string xml) => XDocument.Parse(xml);
        protected override string Format(XDocument xml) => xml.ToString();
    }

    /// <summary>
    /// XElement 类型处理器
    /// </summary>
    internal sealed class XElementHandler : XmlTypeHandler<XElement>
    {
        protected override XElement Parse(string xml) => XElement.Parse(xml);
        protected override string Format(XElement xml) => xml.ToString();
    }
}
