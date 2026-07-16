using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SupplierFlightPricing;

[XmlRoot("Envelope")]
public class SupplierFlightPricingResponse
{
    [XmlElement("Header")]
    public SupplierResponseHeader? Header { get; set; }

    [XmlElement("Body")]
    public SupplierResponseBody? Body { get; set; }

    public static SupplierFlightPricingResponse Deserialize(Stream stream)
    {
        var serializer = new XmlSerializer(typeof(SupplierFlightPricingResponse));
        return (SupplierFlightPricingResponse)serializer.Deserialize(stream)!;
    }

    public static SupplierFlightPricingResponse Deserialize(TextReader reader)
    {
        var serializer = new XmlSerializer(typeof(SupplierFlightPricingResponse));
        return (SupplierFlightPricingResponse)serializer.Deserialize(reader)!;
    }
}

public class SupplierResponseHeader
{
    [XmlElement("Transaction")]
    public SupplierTransaction? Transaction { get; set; }
}

public class SupplierTransaction
{
    [XmlElement("tc")]
    public SupplierTransactionContext? Context { get; set; }
}

public class SupplierTransactionContext
{
    [XmlElement("pid")]
    public string? ProcessId { get; set; }

    [XmlElement("tid")]
    public string? TransactionId { get; set; }

    [XmlElement("dt")]
    public string? DateTime { get; set; }
}

public class SupplierResponseBody
{
    [XmlElement("XXTransactionResponse")]
    public XxTransactionResponse? TransactionResponse { get; set; }
}

public class XxTransactionResponse
{
    [XmlElement("RSP")]
    public SupplierRsp? Rsp { get; set; }
}

public class SupplierRsp
{
    [XmlElement("OfferPriceRS")]
    public OfferPriceResponse? OfferPrice { get; set; }
}

public class OfferPriceResponse
{
    [XmlAttribute("Version")]
    public string? Version { get; set; }

    [XmlAttribute("TransactionIdentifier")]
    public string? TransactionIdentifier { get; set; }

    [XmlElement("Document")]
    public ResponseDocument? Document { get; set; }

    [XmlElement("Success")]
    public string? Success { get; set; }

    [XmlElement("ShoppingResponseID")]
    public ShoppingResponseId? ShoppingResponseId { get; set; }

    [XmlElement("PricedOffer")]
    public PricedOffer? PricedOffer { get; set; }

    [XmlElement("DataLists")]
    public OfferPriceDataLists? DataLists { get; set; }

    [XmlElement("Metadata")]
    public OfferPriceMetadata? Metadata { get; set; }
}

public class ResponseDocument
{
    [XmlAttribute("id")]
    public string? Id { get; set; }
}

public class ShoppingResponseId
{
    [XmlElement("Owner")]
    public string? Owner { get; set; }

    [XmlElement("ResponseID")]
    public string? ResponseId { get; set; }
}

public class PricedOffer
{
    [XmlAttribute("OfferID")]
    public string? OfferId { get; set; }

    [XmlAttribute("Owner")]
    public string? Owner { get; set; }

    [XmlElement("Parameters")]
    public PricedOfferParameters? Parameters { get; set; }

    [XmlElement("ValidatingCarrier")]
    public string? ValidatingCarrier { get; set; }

    [XmlElement("TimeLimits")]
    public OfferTimeLimits? TimeLimits { get; set; }

    [XmlElement("TotalPrice")]
    public DetailCurrencyPriceContainer? TotalPrice { get; set; }

    [XmlElement("Match")]
    public OfferMatch? Match { get; set; }

    [XmlElement("FlightsOverview")]
    public FlightsOverview? FlightsOverview { get; set; }

    [XmlElement("OfferItem")]
    public List<OfferItem> OfferItems { get; set; } = new List<OfferItem>();

    [XmlElement("BaggageAllowance")]
    public List<OfferBaggageAllowanceRef> BaggageAllowances { get; set; } = new List<OfferBaggageAllowanceRef>();
}

public class PricedOfferParameters
{
    [XmlElement("TotalItemQuantity")]
    public int TotalItemQuantity { get; set; }

    [XmlElement("PTC_Priced")]
    public List<PtcPriced> PassengerTypePriced { get; set; } = new List<PtcPriced>();
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
    [XmlAttribute("Quantity")]
    public int Quantity { get; set; }

    [XmlText]
    public string? PassengerTypeCode { get; set; }
}

public class OfferTimeLimits
{
    [XmlElement("OfferExpiration")]
    public DateTimeAttribute? OfferExpiration { get; set; }

    [XmlElement("Payment")]
    public DateTimeAttribute? Payment { get; set; }

    [XmlElement("OtherLimits")]
    public OtherLimits? OtherLimits { get; set; }
}

public class DateTimeAttribute
{
    [XmlAttribute("DateTime")]
    public string? DateTime { get; set; }
}

public class OtherLimits
{
    [XmlElement("OtherLimit")]
    public List<OtherLimit> Items { get; set; } = new List<OtherLimit>();
}

public class OtherLimit
{
    [XmlElement("PriceGuaranteeTimeLimit")]
    public string? PriceGuaranteeTimeLimit { get; set; }

    [XmlElement("TicketByTimeLimit")]
    public TicketByTimeLimit? TicketByTimeLimit { get; set; }
}

public class TicketByTimeLimit
{
    [XmlElement("TicketBy")]
    public string? TicketBy { get; set; }
}

public class DetailCurrencyPriceContainer
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
    [XmlAttribute("Code")]
    public string? Code { get; set; }

    [XmlText]
    public decimal Value { get; set; }
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
    public List<FlightRef> FlightRefs { get; set; } = new List<FlightRef>();
}

public class FlightRef
{
    [XmlAttribute("ODRef")]
    public string? OriginDestinationRef { get; set; }

    [XmlAttribute("PriceClassRef")]
    public string? PriceClassRef { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class OfferItem
{
    [XmlAttribute("OfferItemID")]
    public string? OfferItemId { get; set; }

    [XmlAttribute("MandatoryInd")]
    public bool MandatoryInd { get; set; }

    [XmlElement("TotalPriceDetail")]
    public TotalPriceDetail? TotalPriceDetail { get; set; }

    [XmlElement("Service")]
    public List<OfferService> Services { get; set; } = new List<OfferService>();

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

public class OfferService
{
    [XmlAttribute("ServiceID")]
    public string? ServiceId { get; set; }

    [XmlElement("PassengerRefs")]
    public string? PassengerRefs { get; set; }

    [XmlElement("FlightRefs")]
    public string? FlightRefs { get; set; }

    [XmlElement("ServiceDefinitionRef")]
    public ServiceDefinitionRef? ServiceDefinitionRef { get; set; }
}

public class ServiceDefinitionRef
{
    [XmlAttribute("SegmentRefs")]
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
    public List<FareComponent> FareComponents { get; set; } = new List<FareComponent>();

    [XmlElement("FlightMileage")]
    public FlightMileage? FlightMileage { get; set; }
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
    public decimal ExchangeRate { get; set; }
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
    public List<Tax> Taxes { get; set; } = new List<Tax>();
}

public class Tax
{
    [XmlAttribute("CollectionInd")]
    public bool CollectionInd { get; set; }

    [XmlElement("Amount")]
    public CurrencyAmount? Amount { get; set; }

    [XmlElement("Nation")]
    public string? Nation { get; set; }

    [XmlElement("TaxCode")]
    public string? TaxCode { get; set; }

    [XmlElement("Description")]
    public string? Description { get; set; }
}

public class FareComponent
{
    [XmlElement("Price")]
    public FareComponentPrice? Price { get; set; }

    [XmlElement("FareBasis")]
    public FareBasis? FareBasis { get; set; }

    [XmlElement("FareRules")]
    public FareRules? FareRules { get; set; }

    [XmlElement("PriceClassRef")]
    public string? PriceClassRef { get; set; }

    [XmlElement("SegmentRefs")]
    public SegmentRefs? SegmentRefs { get; set; }
}

public class FareComponentPrice
{
    [XmlElement("BaseAmount")]
    public CurrencyAmount? BaseAmount { get; set; }

    [XmlElement("FareFiledIn")]
    public FareFiledIn? FareFiledIn { get; set; }

    [XmlElement("Taxes")]
    public FareComponentTaxes? Taxes { get; set; }
}

public class FareComponentTaxes
{
    [XmlElement("Total")]
    public CurrencyAmount? Total { get; set; }
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

    [XmlElement("Ticketing")]
    public Ticketing? Ticketing { get; set; }
}

public class Penalty
{
    [XmlAttribute("CancelFeeInd")]
    public bool CancelFeeInd { get; set; }

    [XmlAttribute("ChangeFeeInd")]
    public bool ChangeFeeInd { get; set; }

    [XmlAttribute("RefundableInd")]
    public bool RefundableInd { get; set; }

    [XmlElement("Details")]
    public PenaltyDetails? Details { get; set; }
}

public class PenaltyDetails
{
    [XmlElement("Detail")]
    public List<PenaltyDetail> Details { get; set; } = new List<PenaltyDetail>();
}

public class PenaltyDetail
{
    [XmlAttribute("refs")]
    public string? Refs { get; set; }

    [XmlElement("Type")]
    public string? Type { get; set; }
}

public class Ticketing
{
    [XmlElement("Endorsements")]
    public Endorsements? Endorsements { get; set; }
}

public class Endorsements
{
    [XmlElement("Endorsement")]
    public string? Endorsement { get; set; }
}

public class SegmentRefs
{
    [XmlAttribute("ON_Point")]
    public string? OnPoint { get; set; }

    [XmlAttribute("OFF_Point")]
    public string? OffPoint { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class FlightMileage
{
    [XmlElement("Value")]
    public int Value { get; set; }

    [XmlElement("Application")]
    public string? Application { get; set; }
}

public class OfferBaggageAllowanceRef
{
    [XmlElement("FlightRefs")]
    public string? FlightRefs { get; set; }

    [XmlElement("PassengerRefs")]
    public string? PassengerRefs { get; set; }

    [XmlElement("BaggageAllowanceRef")]
    public string? BaggageAllowanceRef { get; set; }
}

public class OfferPriceDataLists
{
    [XmlElement("PassengerList")]
    public PassengerList? PassengerList { get; set; }

    [XmlElement("BaggageAllowanceList")]
    public BaggageAllowanceList? BaggageAllowanceList { get; set; }

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
    public List<Passenger> Passengers { get; set; } = new List<Passenger>();
}

public class Passenger
{
    [XmlAttribute("PassengerID")]
    public string? PassengerId { get; set; }

    [XmlElement("PTC")]
    public string? Ptc { get; set; }

    [XmlElement("InfantRef")]
    public string? InfantRef { get; set; }
}

public class BaggageAllowanceList
{
    [XmlElement("BaggageAllowance")]
    public List<BaggageAllowance> BaggageAllowances { get; set; } = new List<BaggageAllowance>();
}

public class BaggageAllowance
{
    [XmlAttribute("BaggageAllowanceID")]
    public string? BaggageAllowanceId { get; set; }

    [XmlElement("BaggageCategory")]
    public string? BaggageCategory { get; set; }

    [XmlElement("AllowanceDescription")]
    public AllowanceDescription? AllowanceDescription { get; set; }

    [XmlElement("PieceAllowance")]
    public PieceAllowance? PieceAllowance { get; set; }

    [XmlElement("BaggageDeterminingCarrier")]
    public BaggageDeterminingCarrier? BaggageDeterminingCarrier { get; set; }

    [XmlElement("WeightAllowance")]
    public WeightAllowance? WeightAllowance { get; set; }
}

public class AllowanceDescription
{
    [XmlElement("ApplicableParty")]
    public string? ApplicableParty { get; set; }

    [XmlElement("Descriptions")]
    public TextDescriptions? Descriptions { get; set; }
}

public class TextDescriptions
{
    [XmlElement("Description")]
    public List<TextDescription> Descriptions { get; set; } = new List<TextDescription>();
}

public class TextDescription
{
    [XmlElement("OriginDestinationReference")]
    public string? OriginDestinationReference { get; set; }

    [XmlElement("Text")]
    public string? Text { get; set; }
}

public class PieceAllowance
{
    [XmlElement("TotalQuantity")]
    public int TotalQuantity { get; set; }

    [XmlElement("ApplicableBag")]
    public string? ApplicableBag { get; set; }

    [XmlElement("PieceMeasurements")]
    public PieceMeasurements? PieceMeasurements { get; set; }
}

public class PieceMeasurements
{
    [XmlAttribute("Quantity")]
    public int Quantity { get; set; }
}

public class BaggageDeterminingCarrier
{
    [XmlElement("AirlineID")]
    public string? AirlineId { get; set; }
}

public class WeightAllowance
{
    [XmlElement("MaximumWeight")]
    public MaximumWeight? MaximumWeight { get; set; }
}

public class MaximumWeight
{
    [XmlElement("Value")]
    public decimal Value { get; set; }

    [XmlElement("UOM")]
    public string? UnitOfMeasure { get; set; }
}

public class FareList
{
    [XmlElement("FareGroup")]
    public List<FareGroup> FareGroups { get; set; } = new List<FareGroup>();
}

public class FareGroup
{
    [XmlAttribute("ListKey")]
    public string? ListKey { get; set; }

    [XmlAttribute("refs")]
    public string? Refs { get; set; }

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
    public List<FlightSegment> FlightSegments { get; set; } = new List<FlightSegment>();
}

public class FlightSegment
{
    [XmlAttribute("SegmentKey")]
    public string? SegmentKey { get; set; }

    [XmlAttribute("ConnectInd")]
    public bool ConnectInd { get; set; }

    [XmlAttribute("ElectronicTicketInd")]
    public bool ElectronicTicketInd { get; set; }

    [XmlAttribute("SecureFlight")]
    public bool SecureFlight { get; set; }

    [XmlElement("Departure")]
    public FlightPoint? Departure { get; set; }

    [XmlElement("Arrival")]
    public FlightPoint? Arrival { get; set; }

    [XmlElement("MarketingCarrier")]
    public MarketingCarrier? MarketingCarrier { get; set; }

    [XmlElement("Equipment")]
    public Equipment? Equipment { get; set; }

    [XmlElement("FlightDetail")]
    public FlightDetail? FlightDetail { get; set; }
}

public class FlightPoint
{
    [XmlElement("AirportCode")]
    public string? AirportCode { get; set; }

    [XmlElement("Date")]
    public string? Date { get; set; }

    [XmlElement("Time")]
    public string? Time { get; set; }

    [XmlElement("AirportName")]
    public string? AirportName { get; set; }

    [XmlElement("ChangeOfDay")]
    public string? ChangeOfDay { get; set; }
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
}

public class FlightDetail
{
    [XmlElement("FlightDistance")]
    public FlightDistance? FlightDistance { get; set; }

    [XmlElement("FlightDuration")]
    public FlightDuration? FlightDuration { get; set; }
}

public class FlightDistance
{
    [XmlElement("Value")]
    public decimal Value { get; set; }
}

public class FlightDuration
{
    [XmlElement("Value")]
    public string? Value { get; set; }
}

public class FlightList
{
    [XmlElement("Flight")]
    public List<Flight> Flights { get; set; } = new List<Flight>();
}

public class Flight
{
    [XmlAttribute("FlightKey")]
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
}

public class SegmentReferences
{
    [XmlAttribute("OffPoint")]
    public string? OffPoint { get; set; }

    [XmlAttribute("OnPoint")]
    public string? OnPoint { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class OriginDestinationList
{
    [XmlElement("OriginDestination")]
    public List<OriginDestination> OriginDestinations { get; set; } = new List<OriginDestination>();
}

public class OriginDestination
{
    [XmlAttribute("OriginDestinationKey")]
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
    [XmlAttribute("OffPoint")]
    public string? OffPoint { get; set; }

    [XmlAttribute("OnPoint")]
    public string? OnPoint { get; set; }

    [XmlText]
    public string? Value { get; set; }
}

public class PriceClassList
{
    [XmlElement("PriceClass")]
    public List<PriceClass> PriceClasses { get; set; } = new List<PriceClass>();
}

public class PriceClass
{
    [XmlAttribute("PriceClassID")]
    public string? PriceClassId { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("Code")]
    public string? Code { get; set; }

    [XmlElement("Descriptions")]
    public TextDescriptions? Descriptions { get; set; }
}

public class ServiceDefinitionList
{
    [XmlElement("ServiceDefinition")]
    public List<ServiceDefinition> ServiceDefinitions { get; set; } = new List<ServiceDefinition>();
}

public class ServiceDefinition
{
    [XmlAttribute("ServiceDefinitionID")]
    public string? ServiceDefinitionId { get; set; }

    [XmlAttribute("Owner")]
    public string? Owner { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("Descriptions")]
    public TextDescriptions? Descriptions { get; set; }

    [XmlElement("Encoding")]
    public ServiceEncoding? Encoding { get; set; }

    [XmlElement("FeeMethod")]
    public FeeMethod? FeeMethod { get; set; }

    [XmlElement("BookingInstructions")]
    public BookingInstructions? BookingInstructions { get; set; }

    [XmlElement("ValidatingCarrier")]
    public string? ValidatingCarrier { get; set; }
}

public class ServiceEncoding
{
    [XmlElement("RFIC")]
    public string? Rfic { get; set; }

    [XmlElement("Type")]
    public string? Type { get; set; }

    [XmlElement("SubCode")]
    public string? SubCode { get; set; }
}

public class FeeMethod
{
    [XmlElement("Application")]
    public string? Application { get; set; }
}

public class BookingInstructions
{
    [XmlElement("Method")]
    public string? Method { get; set; }
}

public class OfferPriceMetadata
{
    [XmlElement("Other")]
    public OtherMetadataContainer? Other { get; set; }
}

public class OtherMetadataContainer
{
    [XmlElement("OtherMetadata")]
    public List<OtherMetadata> OtherMetadata { get; set; } = new List<OtherMetadata>();
}

public class OtherMetadata
{
    [XmlElement("CurrencyMetadatas")]
    public CurrencyMetadatas? CurrencyMetadatas { get; set; }

    [XmlElement("PriceMetadatas")]
    public PriceMetadatas? PriceMetadatas { get; set; }

    [XmlElement("RuleMetadatas")]
    public RuleMetadatas? RuleMetadatas { get; set; }
}

public class CurrencyMetadatas
{
    [XmlElement("CurrencyMetadata")]
    public List<CurrencyMetadata> CurrencyMetadata { get; set; } = new List<CurrencyMetadata>();
}

public class CurrencyMetadata
{
    [XmlAttribute("MetadataKey")]
    public string? MetadataKey { get; set; }

    [XmlElement("Application")]
    public string? Application { get; set; }

    [XmlElement("Decimals")]
    public int Decimals { get; set; }
}

public class PriceMetadatas
{
    [XmlElement("PriceMetadata")]
    public List<PriceMetadata> PriceMetadata { get; set; } = new List<PriceMetadata>();
}

public class PriceMetadata
{
    [XmlAttribute("MetadataKey")]
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
    [XmlElement("FareRefKey", Namespace = "http://ndc.farelogix.com/aug")]
    public string? FareRefKey { get; set; }
}

public class RuleMetadatas
{
    [XmlElement("RuleMetadata")]
    public List<RuleMetadata> RuleMetadata { get; set; } = new List<RuleMetadata>();
}

public class RuleMetadata
{
    [XmlAttribute("MetadataKey")]
    public string? MetadataKey { get; set; }

    [XmlElement("RuleID")]
    public string? RuleId { get; set; }

    [XmlElement("Values")]
    public RuleValues? Values { get; set; }
}

public class RuleValues
{
    [XmlElement("Value")]
    public List<RuleValue> Value { get; set; } = new List<RuleValue>();
}

public class RuleValue
{
    [XmlElement("Instruction")]
    public string? Instruction { get; set; }
}
