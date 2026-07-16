using System.Xml.Serialization;

namespace OfferPriceSoapModels;

public static class SoapNamespaces
{
    public const string SoapEnvelope = "http://schemas.xmlsoap.org/soap/envelope/";
    public const string Xxs = "xxs";
}

[XmlRoot("Envelope", Namespace = SoapNamespaces.SoapEnvelope)]
public sealed class SoapEnvelope
{
    [XmlElement("Body", Namespace = SoapNamespaces.SoapEnvelope)]
    public SoapBody Body { get; set; } = new();
}

public sealed class SoapBody
{
    [XmlElement("XXTransaction", Namespace = SoapNamespaces.Xxs)]
    public XxTransaction Transaction { get; set; } = new();
}

public sealed class XxTransaction
{
    [XmlElement("REQ", Namespace = "")]
    public TransactionRequest Request { get; set; } = new();
}

public sealed class TransactionRequest
{
    [XmlElement("OfferPriceRQ", Namespace = "")]
    public OfferPriceRequest OfferPriceRequest { get; set; } = new();
}

public sealed class OfferPriceRequest
{
    [XmlAttribute]
    public string Version { get; set; } = string.Empty;

    [XmlAttribute]
    public string TransactionIdentifier { get; set; } = string.Empty;

    [XmlElement("Document")]
    public OfferPriceDocument Document { get; set; } = new();

    [XmlElement("Party")]
    public Party Party { get; set; } = new();

    [XmlElement("Query")]
    public OfferPriceQuery Query { get; set; } = new();

    [XmlElement("Preference")]
    public OfferPricePreference Preference { get; set; } = new();

    [XmlElement("Qualifier")]
    public OfferPriceQualifier Qualifier { get; set; } = new();

    [XmlElement("DataLists")]
    public OfferPriceDataLists DataLists { get; set; } = new();
}

public sealed class OfferPriceDocument
{
    [XmlAttribute("id")]
    public string Id { get; set; } = string.Empty;
}

public sealed class Party
{
    [XmlElement("Sender")]
    public Sender Sender { get; set; } = new();
}

public sealed class Sender
{
    [XmlElement("TravelAgencySender")]
    public TravelAgencySender TravelAgencySender { get; set; } = new();
}

public sealed class TravelAgencySender
{
    [XmlElement("PseudoCity")]
    public string PseudoCity { get; set; } = string.Empty;

    [XmlElement("AgencyID")]
    public string AgencyId { get; set; } = string.Empty;
}

public sealed class OfferPriceQuery
{
    [XmlElement("Offer")]
    public List<Offer> Offers { get; set; } = [];
}

public sealed class Offer
{
    [XmlAttribute]
    public string OfferID { get; set; } = string.Empty;

    [XmlAttribute]
    public string Owner { get; set; } = string.Empty;

    [XmlAttribute]
    public string ResponseID { get; set; } = string.Empty;

    [XmlElement("OfferItem")]
    public List<OfferItem> OfferItems { get; set; } = [];
}

public sealed class OfferItem
{
    [XmlAttribute]
    public string OfferItemID { get; set; } = string.Empty;

    [XmlElement("PassengerRefs")]
    public string PassengerRefs { get; set; } = string.Empty;
}

public sealed class OfferPricePreference
{
    [XmlElement("FarePreferences")]
    public FarePreferences FarePreferences { get; set; } = new();

    [XmlElement("PricingMethodPreference")]
    public PricingMethodPreference PricingMethodPreference { get; set; } = new();

    [XmlElement("ServicePricingOnlyPreference")]
    public ServicePricingOnlyPreference ServicePricingOnlyPreference { get; set; } = new();
}

public sealed class FarePreferences
{
    [XmlArray("Types")]
    [XmlArrayItem("Type")]
    public List<string> Types { get; set; } = [];

    [XmlElement("Exclusion")]
    public FareExclusion Exclusion { get; set; } = new();
}

public sealed class FareExclusion
{
    [XmlElement("NoMinStayInd")]
    public bool NoMinStayInd { get; set; }

    [XmlElement("NoMaxStayInd")]
    public bool NoMaxStayInd { get; set; }

    [XmlElement("NoAdvPurchaseInd")]
    public bool NoAdvPurchaseInd { get; set; }

    [XmlElement("NoPenaltyInd")]
    public bool NoPenaltyInd { get; set; }
}

public sealed class PricingMethodPreference
{
    [XmlElement("BestPricingOption")]
    public string BestPricingOption { get; set; } = string.Empty;
}

public sealed class ServicePricingOnlyPreference
{
    [XmlElement("ServicePricingOnlyInd")]
    public bool ServicePricingOnlyInd { get; set; }
}

public sealed class OfferPriceQualifier
{
}

public sealed class OfferPriceDataLists
{
    [XmlElement("PassengerList")]
    public PassengerList PassengerList { get; set; } = new();
}

public sealed class PassengerList
{
    [XmlElement("Passenger")]
    public List<Passenger> Passengers { get; set; } = [];
}

public sealed class Passenger
{
    [XmlAttribute]
    public string PassengerID { get; set; } = string.Empty;

    [XmlElement("PTC")]
    public string Ptc { get; set; } = string.Empty;

    [XmlElement("InfantRef")]
    public string? InfantRef { get; set; }
}
