using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace OfferPriceSoapModels;

public static class OfferPriceSoapSerializer
{
    public static string Serialize(SoapEnvelope envelope, bool omitXmlDeclaration = true)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var serializer = new XmlSerializer(typeof(SoapEnvelope));
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = omitXmlDeclaration
        };

        using var writer = new Utf8StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, settings);
        serializer.Serialize(xmlWriter, envelope, CreateNamespaces());

        return writer.ToString();
    }

    public static SoapEnvelope Deserialize(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML content is required.", nameof(xml));
        }

        var serializer = new XmlSerializer(typeof(SoapEnvelope));
        using var reader = new StringReader(xml);

        return (SoapEnvelope)serializer.Deserialize(reader)!;
    }

    public static XmlSerializerNamespaces CreateNamespaces()
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("SOAP-ENV", SoapNamespaces.SoapEnvelope);
        namespaces.Add("ns1", SoapNamespaces.Xxs);
        return namespaces;
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
