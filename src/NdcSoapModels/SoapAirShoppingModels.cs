using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace NdcSoapModels;

public static class SoapNamespaces
{
    public const string SoapEnvelope = "http://schemas.xmlsoap.org/soap/envelope/";
    public const string Xxs = "xxs";
    public const string FarelogixAugmentation = "http://ndc.farelogix.com/aug";
}

public static class SoapAirShoppingDeserializer
{
    private static readonly XmlSerializer Serializer = new(typeof(SoapEnvelope));

    public static SoapEnvelope Deserialize(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        using var reader = new StringReader(xml);
        return Deserialize(reader);
    }

    public static SoapEnvelope Deserialize(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = XmlReader.Create(stream);
        return Deserialize(reader);
    }

    public static SoapEnvelope Deserialize(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var result = Serializer.Deserialize(reader);
        return result as SoapEnvelope
            ?? throw new InvalidOperationException("The XML payload is not a SOAP AirShopping response envelope.");
    }

    public static SoapEnvelope Deserialize(XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var result = Serializer.Deserialize(reader);
        return result as SoapEnvelope
            ?? throw new InvalidOperationException("The XML payload is not a SOAP AirShopping response envelope.");
    }
}

[XmlRoot("Envelope", Namespace = SoapNamespaces.SoapEnvelope)]
public class SoapEnvelope
{
    [XmlAttribute("encodingStyle", Namespace = SoapNamespaces.SoapEnvelope, Form = XmlSchemaForm.Qualified)]
    public string? EncodingStyle { get; set; }

    [XmlElement("Header", Namespace = SoapNamespaces.SoapEnvelope)]
    public SoapHeader? Header { get; set; }

    [XmlElement("Body", Namespace = SoapNamespaces.SoapEnvelope)]
    public SoapBody? Body { get; set; }
}

public class SoapHeader
{
    [XmlElement("Transaction", Namespace = SoapNamespaces.Xxs)]
    public TransactionHeader? Transaction { get; set; }
}

public class TransactionHeader
{
    [XmlElement("tc", Namespace = "")]
    public TransactionContext? TransactionContext { get; set; }
}

public class TransactionContext
{
    [XmlElement("pid")]
    public string? ProcessId { get; set; }

    [XmlElement("tid")]
    public string? TransactionId { get; set; }

    [XmlElement("dt")]
    public string? DateTime { get; set; }
}

public class SoapBody
{
    [XmlElement("XXTransactionResponse", Namespace = SoapNamespaces.Xxs)]
    public XxTransactionResponse? TransactionResponse { get; set; }
}

public class XxTransactionResponse
{
    [XmlElement("RSP", Namespace = "")]
    public ResponsePayload? ResponsePayload { get; set; }
}

public class ResponsePayload
{
    [XmlElement("AirShoppingRS")]
    public AirShoppingResponse? AirShoppingResponse { get; set; }
}

public class AirShoppingResponse
{
    [XmlAttribute]
    public string? Version { get; set; }

    [XmlAttribute]
    public string? TransactionIdentifier { get; set; }

    [XmlElement("Document")]
    public ResponseDocument? Document { get; set; }

    [XmlElement("Success")]
    public EmptyElement? Success { get; set; }

    [XmlElement("ShoppingResponseID")]
    public ShoppingResponseId? ShoppingResponseId { get; set; }

    [XmlElement("OffersGroup")]
    public OffersGroup? OffersGroup { get; set; }

    [XmlElement("DataLists")]
    public DataLists? DataLists { get; set; }

    [XmlElement("Metadata")]
    public ResponseMetadata? Metadata { get; set; }
}

public class ResponseDocument
{
    [XmlAttribute("id")]
    public string? Id { get; set; }
}

public class EmptyElement
{
}

public class ShoppingResponseId
{
    [XmlElement("Owner")]
    public string? Owner { get; set; }

    [XmlElement("ResponseID")]
    public string? ResponseId { get; set; }
}

public class OffersGroup
{
    [XmlElement("AirlineOffers")]
    public AirlineOffers? AirlineOffers { get; set; }
}

public class AirlineOffers
{
    [XmlElement("AirlineOfferSnapshot")]
    public AirlineOfferSnapshot? AirlineOfferSnapshot { get; set; }

    [XmlElement("Offer")]
    public List<Offer> Offers { get; set; } = [];
}

public class AirlineOfferSnapshot
{
    [XmlElement("PassengerQuantity")]
    public string? PassengerQuantity { get; set; }

    [XmlElement("Highest")]
    public EncodedPriceLimit? Highest { get; set; }

    [XmlElement("Lowest")]
    public EncodedPriceLimit? Lowest { get; set; }

    [XmlElement("MatchedOfferQuantity")]
    public string? MatchedOfferQuantity { get; set; }
}

public class EncodedPriceLimit
{
    [XmlElement("EncodedCurrencyPrice")]
    public CurrencyAmount? EncodedCurrencyPrice { get; set; }
}

public class Offer
{
    [XmlAttribute]
    public string? OfferID { get; set; }

    [XmlAttribute]
    public string? Owner { get; set; }

    [XmlElement("Parameters")]
    public OfferParameters? Parameters { get; set; }

    [XmlElement("ValidatingCarrier")]
    public string? ValidatingCarrier { get; set; }

    [XmlElement("TimeLimits")]
    public TimeLimits? TimeLimits { get; set; }

    [XmlElement("TotalPrice")]
    public TotalPrice? TotalPrice { get; set; }

    [XmlElement("Match")]
    public OfferMatch? Match { get; set; }

    [XmlElement("FlightsOverview")]
    public FlightsOverview? FlightsOverview { get; set; }

    [XmlElement("OfferItem")]
    public List<OfferItem> OfferItems { get; set; } = [];
}

public class OfferParameters
{
    [XmlElement("TotalItemQuantity")]
    public string? TotalItemQuantity { get; set; }

    [XmlElement("PTC_Priced")]
    public List<PtcPriced> PtcPriced { get; set; } = [];
}

public class PtcPriced
{
    [XmlAttribute("refs")]
    public string? Refs { get; set; }

    [XmlElement("Requested")]
    public PassengerTypeQuantity? Requested { get; set; }

    [XmlElement("Priced")]
    public PassengerTypeQuantity? Priced { get; set; }
}

public class PassengerTypeQuantity
{
    [XmlAttribute]
    public string? Quantity { get; set; }

    [XmlText]
    public string? PassengerTypeCode { get; set; }
}

public class TimeLimits
{
    [XmlElement("OfferExpiration")]
    public DateTimeLimit? OfferExpiration { get; set; }

    [XmlElement("Payment")]
    public DateTimeLimit? Payment { get; set; }

    [XmlElement("OtherLimits")]
    public OtherLimits? OtherLimits { get; set; }
}

public class DateTimeLimit
{
    [XmlAttribute]
    public string? DateTime { get; set; }
}

public class OtherLimits
{
    [XmlElement("OtherLimit")]
    public List<OtherLimit> OtherLimit { get; set; } = [];
}

public class OtherLimit
{
    [XmlElement("PriceGuaranteeTimeLimit")]
    public EmptyElement? PriceGuaranteeTimeLimit { get; set; }

    [XmlElement("TicketByTimeLimit")]
    public TicketByTimeLimit? TicketByTimeLimit { get; set; }
}

public class TicketByTimeLimit
{
    [XmlElement("TicketBy")]
    public string? TicketBy { get; set; }
}

public class TotalPrice
{
    [XmlElement("DetailCurrencyPrice")]
    public DetailCurrencyPrice? DetailCurrencyPrice { get; set; }
}

public class DetailCurrencyPrice
{
    [XmlElement("Total")]
    public CurrencyAmount? Total { get; set; }
}

public class CurrencyAmount
{
    [XmlAttribute]
    public string? Code { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class OfferMatch
{
    [XmlElement("Application")]
    public string? Application { get; set; }

    [XmlElement("MatchResult")]
    public string? MatchResult { get; set; }
}

public class FlightsOverview
{
    [XmlElement("FlightRef")]
    public List<FlightRef> FlightRefs { get; set; } = [];
}

public class FlightRef
{
    [XmlAttribute]
    public string? ODRef { get; set; }

    [XmlAttribute]
    public string? PriceClassRef { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class OfferItem
{
    [XmlAttribute]
    public string? OfferItemID { get; set; }

    [XmlAttribute]
    public string? MandatoryInd { get; set; }

    [XmlElement("TotalPriceDetail")]
    public TotalPriceDetail? TotalPriceDetail { get; set; }

    [XmlElement("Service")]
    public List<Service> Services { get; set; } = [];

    [XmlElement("FareDetail")]
    public FareDetail? FareDetail { get; set; }
}

public class TotalPriceDetail
{
    [XmlElement("TotalAmount")]
    public TotalAmount? TotalAmount { get; set; }
}

public class TotalAmount
{
    [XmlElement("DetailCurrencyPrice")]
    public DetailCurrencyPrice? DetailCurrencyPrice { get; set; }
}

public class Service
{
    [XmlAttribute]
    public string? ServiceID { get; set; }

    [XmlElement("PassengerRefs")]
    public string? PassengerRefs { get; set; }

    [XmlElement("FlightRefs")]
    public string? FlightRefs { get; set; }

    [XmlElement("ServiceDefinitionRef")]
    public ServiceDefinitionReference? ServiceDefinitionRef { get; set; }
}

public class ServiceDefinitionReference
{
    [XmlAttribute]
    public string? SegmentRefs { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class FareDetail
{
    [XmlElement("FareIndicatorCode")]
    public string? FareIndicatorCode { get; set; }

    [XmlElement("PassengerRefs")]
    public string? PassengerRefs { get; set; }

    [XmlElement("Price")]
    public FarePrice? Price { get; set; }

    [XmlElement("FareComponent")]
    public List<FareComponent> FareComponents { get; set; } = [];
}

public class FarePrice
{
    [XmlElement("BaseAmount")]
    public CurrencyAmount? BaseAmount { get; set; }

    [XmlElement("FareFiledIn")]
    public FareFiledIn? FareFiledIn { get; set; }

    [XmlElement("Taxes")]
    public Taxes? Taxes { get; set; }
}

public class FareFiledIn
{
    [XmlElement("BaseAmount")]
    public CurrencyAmount? BaseAmount { get; set; }

    [XmlElement("ExchangeRate")]
    public string? ExchangeRate { get; set; }
}

public class Taxes
{
    [XmlElement("Total")]
    public CurrencyAmount? Total { get; set; }

    [XmlElement("Breakdown")]
    public TaxBreakdown? Breakdown { get; set; }
}

public class TaxBreakdown
{
    [XmlElement("Tax")]
    public List<Tax> Taxes { get; set; } = [];
}

public class Tax
{
    [XmlElement("Amount")]
    public CurrencyAmount? Amount { get; set; }

    [XmlElement("TaxCode")]
    public string? TaxCode { get; set; }

    [XmlElement("Description")]
    public string? Description { get; set; }
}

public class FareComponent
{
    [XmlElement("FareBasis")]
    public FareBasis? FareBasis { get; set; }

    [XmlElement("FareRules")]
    public FareRules? FareRules { get; set; }

    [XmlElement("PriceClassRef")]
    public string? PriceClassRef { get; set; }

    [XmlElement("SegmentRefs")]
    public SegmentRefs? SegmentRefs { get; set; }
}

public class FareBasis
{
    [XmlElement("FareBasisCode")]
    public FareBasisCode? FareBasisCode { get; set; }

    [XmlElement("FareBasisCityPair")]
    public string? FareBasisCityPair { get; set; }

    [XmlElement("RBD")]
    public string? Rbd { get; set; }

    [XmlElement("CabinType")]
    public CabinType? CabinType { get; set; }
}

public class FareBasisCode
{
    [XmlAttribute("refs")]
    public string? Refs { get; set; }

    [XmlElement("Code")]
    public string? Code { get; set; }
}

public class CabinType
{
    [XmlElement("CabinTypeCode")]
    public string? CabinTypeCode { get; set; }

    [XmlElement("CabinTypeName")]
    public string? CabinTypeName { get; set; }
}

public class FareRules
{
    [XmlElement("Penalty")]
    public Penalty? Penalty { get; set; }
}

public class Penalty
{
    [XmlAttribute]
    public string? RefundableInd { get; set; }
}

public class SegmentRefs
{
    [XmlAttribute]
    public string? ON_Point { get; set; }

    [XmlAttribute]
    public string? OFF_Point { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class DataLists
{
    [XmlElement("PassengerList")]
    public PassengerList? PassengerList { get; set; }

    [XmlElement("FareList")]
    public FareList? FareList { get; set; }

    [XmlElement("FlightSegmentList")]
    public FlightSegmentList? FlightSegmentList { get; set; }

    [XmlElement("FlightList")]
    public FlightList? FlightList { get; set; }

    [XmlElement("OriginDestinationList")]
    public OriginDestinationList? OriginDestinationList { get; set; }

    [XmlElement("PriceClassList")]
    public PriceClassList? PriceClassList { get; set; }

    [XmlElement("ServiceDefinitionList")]
    public ServiceDefinitionList? ServiceDefinitionList { get; set; }
}

public class PassengerList
{
    [XmlElement("Passenger")]
    public List<Passenger> Passengers { get; set; } = [];
}

public class Passenger
{
    [XmlAttribute]
    public string? PassengerID { get; set; }

    [XmlElement("PTC")]
    public string? Ptc { get; set; }
}

public class FareList
{
    [XmlElement("FareGroup")]
    public List<FareGroup> FareGroups { get; set; } = [];
}

public class FareGroup
{
    [XmlAttribute("refs")]
    public string? Refs { get; set; }

    [XmlAttribute]
    public string? ListKey { get; set; }

    [XmlElement("Fare")]
    public Fare? Fare { get; set; }

    [XmlElement("FareBasisCode")]
    public FareBasisCode? FareBasisCode { get; set; }
}

public class Fare
{
    [XmlElement("FareCode")]
    public string? FareCode { get; set; }
}

public class FlightSegmentList
{
    [XmlElement("FlightSegment")]
    public List<FlightSegment> FlightSegments { get; set; } = [];
}

public class FlightSegment
{
    [XmlAttribute]
    public string? SegmentKey { get; set; }

    [XmlAttribute]
    public string? ConnectInd { get; set; }

    [XmlAttribute]
    public string? ElectronicTicketInd { get; set; }

    [XmlElement("Departure")]
    public FlightEndpoint? Departure { get; set; }

    [XmlElement("Arrival")]
    public FlightEndpoint? Arrival { get; set; }

    [XmlElement("MarketingCarrier")]
    public MarketingCarrier? MarketingCarrier { get; set; }

    [XmlElement("Equipment")]
    public Equipment? Equipment { get; set; }

    [XmlElement("FlightDetail")]
    public FlightDetail? FlightDetail { get; set; }
}

public class FlightEndpoint
{
    [XmlElement("AirportCode")]
    public string? AirportCode { get; set; }

    [XmlElement("Date")]
    public string? Date { get; set; }

    [XmlElement("Time")]
    public string? Time { get; set; }

    [XmlElement("ChangeOfDay")]
    public string? ChangeOfDay { get; set; }

    [XmlElement("AirportName")]
    public string? AirportName { get; set; }
}

public class MarketingCarrier
{
    [XmlElement("AirlineID")]
    public string? AirlineId { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("FlightNumber")]
    public string? FlightNumber { get; set; }
}

public class Equipment
{
    [XmlElement("AircraftCode")]
    public string? AircraftCode { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }
}

public class FlightDetail
{
    [XmlElement("FlightDistance")]
    public Distance? FlightDistance { get; set; }

    [XmlElement("FlightDuration")]
    public FlightDuration? FlightDuration { get; set; }
}

public class Distance
{
    [XmlElement("Value")]
    public string? Value { get; set; }

    [XmlElement("UOM")]
    public string? Uom { get; set; }
}

public class FlightDuration
{
    [XmlElement("Value")]
    public string? Value { get; set; }
}

public class FlightList
{
    [XmlElement("Flight")]
    public List<Flight> Flights { get; set; } = [];
}

public class Flight
{
    [XmlAttribute]
    public string? FlightKey { get; set; }

    [XmlElement("Journey")]
    public Journey? Journey { get; set; }

    [XmlElement("SegmentReferences")]
    public SegmentReferences? SegmentReferences { get; set; }
}

public class Journey
{
    [XmlElement("Time")]
    public string? Time { get; set; }

    [XmlElement("Distance")]
    public Distance? Distance { get; set; }
}

public class SegmentReferences
{
    [XmlAttribute]
    public string? OnPoint { get; set; }

    [XmlAttribute]
    public string? OffPoint { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class OriginDestinationList
{
    [XmlElement("OriginDestination")]
    public List<OriginDestination> OriginDestinations { get; set; } = [];
}

public class OriginDestination
{
    [XmlAttribute("refs")]
    public string? Refs { get; set; }

    [XmlAttribute]
    public string? OriginDestinationKey { get; set; }

    [XmlElement("DepartureCode")]
    public string? DepartureCode { get; set; }

    [XmlElement("ArrivalCode")]
    public string? ArrivalCode { get; set; }

    [XmlElement("FlightReferences")]
    public FlightReferences? FlightReferences { get; set; }
}

public class FlightReferences
{
    [XmlAttribute]
    public string? OnPoint { get; set; }

    [XmlAttribute]
    public string? OffPoint { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class PriceClassList
{
    [XmlElement("PriceClass")]
    public List<PriceClass> PriceClasses { get; set; } = [];
}

public class PriceClass
{
    [XmlAttribute]
    public string? PriceClassID { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("Code")]
    public string? Code { get; set; }

    [XmlElement("Descriptions")]
    public Descriptions? Descriptions { get; set; }

    [XmlElement("DisplayOrder")]
    public string? DisplayOrder { get; set; }
}

public class Descriptions
{
    [XmlElement("Description")]
    public List<Description> DescriptionItems { get; set; } = [];
}

public class Description
{
    [XmlElement("OriginDestinationReference")]
    public string? OriginDestinationReference { get; set; }

    [XmlElement("Text")]
    public string? Text { get; set; }

    [XmlElement("Application")]
    public string? Application { get; set; }

    [XmlElement("Media")]
    public Media? Media { get; set; }
}

public class Media
{
    [XmlElement("ObjectID")]
    public string? ObjectId { get; set; }
}

public class ServiceDefinitionList
{
    [XmlElement("ServiceDefinition")]
    public List<ServiceDefinition> ServiceDefinitions { get; set; } = [];
}

public class ServiceDefinition
{
    [XmlAttribute]
    public string? ServiceDefinitionID { get; set; }

    [XmlAttribute]
    public string? Owner { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("Encoding")]
    public ServiceDefinitionEncoding? Encoding { get; set; }

    [XmlElement("FeeMethod")]
    public string? FeeMethod { get; set; }

    [XmlElement("Descriptions")]
    public Descriptions? Descriptions { get; set; }

    [XmlElement("BookingInstructions")]
    public BookingInstructions? BookingInstructions { get; set; }

    [XmlElement("ValidatingCarrier")]
    public string? ValidatingCarrier { get; set; }
}

public class ServiceDefinitionEncoding
{
    [XmlElement("RFIC")]
    public string? Rfic { get; set; }

    [XmlElement("Type")]
    public string? Type { get; set; }

    [XmlElement("Code")]
    public string? Code { get; set; }

    [XmlElement("SubCode")]
    public string? SubCode { get; set; }
}

public class BookingInstructions
{
    [XmlElement("Method")]
    public string? Method { get; set; }
}

public class ResponseMetadata
{
    [XmlElement("Other")]
    public OtherMetadataCollection? Other { get; set; }
}

public class OtherMetadataCollection
{
    [XmlElement("OtherMetadata")]
    public List<OtherMetadata> OtherMetadata { get; set; } = [];
}

public class OtherMetadata
{
    [XmlElement("CodesetMetadatas")]
    public CodesetMetadatas? CodesetMetadatas { get; set; }

    [XmlElement("CurrencyMetadatas")]
    public CurrencyMetadatas? CurrencyMetadatas { get; set; }

    [XmlElement("PriceMetadatas")]
    public PriceMetadatas? PriceMetadatas { get; set; }
}

public class CodesetMetadatas
{
    [XmlElement("CodesetMetadata")]
    public List<CodesetMetadata> CodesetMetadata { get; set; } = [];
}

public class CodesetMetadata
{
    [XmlAttribute]
    public string? MetadataKey { get; set; }

    [XmlElement("Source")]
    public MetadataSource? Source { get; set; }
}

public class MetadataSource
{
    [XmlElement("OwnerID")]
    public string? OwnerId { get; set; }
}

public class CurrencyMetadatas
{
    [XmlElement("CurrencyMetadata")]
    public List<CurrencyMetadata> CurrencyMetadata { get; set; } = [];
}

public class CurrencyMetadata
{
    [XmlAttribute]
    public string? MetadataKey { get; set; }

    [XmlElement("Application")]
    public string? Application { get; set; }

    [XmlElement("Decimals")]
    public string? Decimals { get; set; }
}

public class PriceMetadatas
{
    [XmlElement("PriceMetadata")]
    public List<PriceMetadata> PriceMetadata { get; set; } = [];
}

public class PriceMetadata
{
    [XmlAttribute]
    public string? MetadataKey { get; set; }

    [XmlElement("AugmentationPoint")]
    public AugmentationPoint? AugmentationPoint { get; set; }
}

public class AugmentationPoint
{
    [XmlElement("AugPoint")]
    public AugPoint? AugPoint { get; set; }
}

public class AugPoint
{
    [XmlElement("FareRefKey", Namespace = SoapNamespaces.FarelogixAugmentation)]
    public string? FareRefKey { get; set; }
}
