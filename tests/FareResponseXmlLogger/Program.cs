using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using NdcSoapModels;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: FareResponseXmlLogger <supplier-response.xml> [output-fare-response.xml]");
    Environment.ExitCode = 1;
    return;
}

var inputPath = Path.GetFullPath(args[0]);
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input XML file not found: {inputPath}");
    Environment.ExitCode = 2;
    return;
}

var outputPath = args.Length == 2
    ? Path.GetFullPath(args[1])
    : BuildDefaultOutputPath(inputPath);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var envelope = SoapAirShoppingDeserializer.Deserialize(File.ReadAllText(inputPath));
var response = envelope.Body?.TransactionResponse?.ResponsePayload?.AirShoppingResponse
    ?? throw new InvalidOperationException("AirShoppingRS was not found in the supplier XML.");

var fareSets = AirShoppingFareBuilder.BuildJourneyPassengerFareSets(response);
var log = FareResponseLogMapper.Map(inputPath, response, fareSets);

using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings
{
    Indent = true,
    IndentChars = "  "
});

new XmlSerializer(typeof(FareResponseLog)).Serialize(writer, log);

Console.WriteLine($"Fare response XML written: {outputPath}");
Console.WriteLine($"Offer fare sets: {log.Summary.OfferCount}, pax fares: {log.Summary.PassengerFareCount}");

static string BuildDefaultOutputPath(string inputPath)
{
    var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    var fileName = $"{Path.GetFileNameWithoutExtension(inputPath)}-fare-response-{DateTime.UtcNow:yyyyMMddHHmmss}.xml";
    return Path.Combine(logDirectory, fileName);
}

internal static class FareResponseLogMapper
{
    public static FareResponseLog Map(
        string inputPath,
        AirShoppingResponse response,
        IReadOnlyList<JourneyPassengerFareSet> fareSets)
    {
        var offers = fareSets
            .Select((fareSet, index) => MapOffer(index + 1, fareSet))
            .ToList();

        return new FareResponseLog
        {
            GeneratedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            SourceFile = inputPath,
            Version = response.Version,
            TransactionIdentifier = response.TransactionIdentifier,
            ShoppingResponseId = response.ShoppingResponseId?.ResponseId,
            Owner = response.ShoppingResponseId?.Owner,
            Summary = new FareResponseSummary
            {
                OfferCount = offers.Count,
                PassengerFareCount = offers.Sum(offer => offer.OfferItem.FareDetail.Count),
                PassengerTypes = offers
                    .SelectMany(offer => offer.OfferItem.FareDetail)
                    .SelectMany(fareDetail => fareDetail.FareComponent.PaxType)
                    .Select(paxType => paxType.Type)
                    .Where(type => !string.IsNullOrWhiteSpace(type))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(type => type)
                    .Select(type => type!)
                    .ToList()
            },
            Offers = offers
        };
    }

    private static FareOfferLog MapOffer(int index, JourneyPassengerFareSet fareSet)
    {
        var fareDetails = fareSet.PassengerFares
            .Select((fare, fareIndex) => MapFareDetail(fareIndex + 1, fareSet, fare))
            .ToList();

        return new FareOfferLog
        {
            OfferID = $"OF{index}",
            OutboundOfferId = fareSet.OutboundOfferId,
            ReturnOfferId = fareSet.ReturnOfferId,
            SupplierOfferIds = fareSet.OfferIds.ToList(),
            OfferItem = new FareOfferItemLog
            {
                FareDetail = fareDetails
            }
        };
    }

    private static FareDetailLog MapFareDetail(
        int index,
        JourneyPassengerFareSet fareSet,
        PassengerTypeFare fare)
    {
        var rbd = fare.Breakdowns
            .Select(breakdown => breakdown.Rbd)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(value => value!)
            .ToList();

        return new FareDetailLog
        {
            OfferItemID = $"F{index}",
            FareSource = "CRS",
            CabinClass = fare.Breakdowns.FirstOrDefault()?.CabinName,
            BrandId = fare.Breakdowns.FirstOrDefault()?.PriceClassRef,
            BrandName = fare.Breakdowns.FirstOrDefault()?.PriceClassRef,
            FareComponent = new FareComponentLog
            {
                FareSellkey = string.Join("|", fare.OfferItemIds),
                SegSellKey = string.Join("|", fareSet.OfferIds),
                Rbd = rbd,
                Price = new PriceLog
                {
                    TotalAmount = FormatAmount(fare.TotalAmountForPassengerType),
                    TotalTaxAmount = FormatAmount(fare.PerPassengerTaxAmount * fare.Quantity),
                    CurrencyCode = fare.CurrencyCode
                },
                PaxType =
                [
                    new FarePaxTypeLog
                    {
                        Type = fare.PassengerTypeCode,
                        Quantity = fare.Quantity,
                        PassengerRefs = fare.PassengerRefs.ToList(),
                        PtcPricedCode = fare.OfferItemIds.ToList(),
                        FareBasisCode = fare.Breakdowns
                            .Select(breakdown => breakdown.FareBasisCode)
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList()!,
                        BaseAmount = FormatAmount(fare.PerPassengerBaseAmount),
                        Tax = FormatAmount(fare.PerPassengerTaxAmount),
                        TotalAmount = FormatAmount(fare.PerPassengerTotalAmount),
                        PassengerTypeTotalAmount = FormatAmount(fare.TotalAmountForPassengerType),
                        TaxSummary = MapTaxSummary(fare),
                        Breakup = fare.Breakdowns.Select(MapBreakup).ToList()
                    }
                ]
            }
        };
    }

    private static TaxSummaryLog MapTaxSummary(PassengerTypeFare fare)
    {
        return new TaxSummaryLog
        {
            TotalTaxAmount = FormatAmount(fare.PerPassengerTaxAmount),
            Tax = fare.Breakdowns
                .SelectMany(breakdown => breakdown.Taxes)
                .GroupBy(tax => tax.TaxCode ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(group => new TaxItemLog
                {
                    TaxCode = group.Key,
                    TaxName = group.Select(tax => tax.Description).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
                    Amount = FormatAmount(group.Sum(tax => tax.Amount)),
                    SuppAmount = FormatAmount(group.Sum(tax => tax.Amount)),
                    SuppTax = true
                })
                .ToList()
        };
    }

    private static FareBreakupLog MapBreakup(DirectionalFareBreakdown breakdown)
    {
        return new FareBreakupLog
        {
            OriginDestinationRef = breakdown.OriginDestinationRef,
            OfferId = breakdown.OfferId,
            OfferItemId = breakdown.OfferItemId,
            FlightRef = breakdown.FlightRef,
            FareKey = breakdown.FareKey,
            BaseAmount = FormatAmount(breakdown.PerPassengerBaseAmount),
            Tax = FormatAmount(breakdown.PerPassengerTaxAmount),
            TotalAmount = FormatAmount(breakdown.PerPassengerTotalAmount),
            SupplierTotalItemAmount = FormatAmount(breakdown.SupplierTotalItemAmount)
        };
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

[XmlRoot("FareResponseLog")]
public sealed class FareResponseLog
{
    [XmlAttribute]
    public string GeneratedUtc { get; set; } = string.Empty;

    [XmlAttribute]
    public string SourceFile { get; set; } = string.Empty;

    [XmlAttribute]
    public string? Version { get; set; }

    [XmlAttribute]
    public string? TransactionIdentifier { get; set; }

    [XmlAttribute]
    public string? ShoppingResponseId { get; set; }

    [XmlAttribute]
    public string? Owner { get; set; }

    public FareResponseSummary Summary { get; set; } = new();

    [XmlArray]
    [XmlArrayItem("Offer")]
    public List<FareOfferLog> Offers { get; set; } = [];
}

public sealed class FareResponseSummary
{
    [XmlAttribute]
    public int OfferCount { get; set; }

    [XmlAttribute]
    public int PassengerFareCount { get; set; }

    [XmlArray]
    [XmlArrayItem("Type")]
    public List<string> PassengerTypes { get; set; } = [];
}

public sealed class FareOfferLog
{
    [XmlAttribute]
    public string OfferID { get; set; } = string.Empty;

    [XmlAttribute]
    public string? OutboundOfferId { get; set; }

    [XmlAttribute]
    public string? ReturnOfferId { get; set; }

    [XmlArray]
    [XmlArrayItem("SupplierOfferId")]
    public List<string> SupplierOfferIds { get; set; } = [];

    public FareOfferItemLog OfferItem { get; set; } = new();
}

public sealed class FareOfferItemLog
{
    [XmlElement("FareDetail")]
    public List<FareDetailLog> FareDetail { get; set; } = [];
}

public sealed class FareDetailLog
{
    [XmlAttribute]
    public string OfferItemID { get; set; } = string.Empty;

    [XmlAttribute]
    public string? FareSource { get; set; }

    [XmlAttribute]
    public string? CabinClass { get; set; }

    [XmlAttribute]
    public string? BrandId { get; set; }

    [XmlAttribute]
    public string? BrandName { get; set; }

    public FareComponentLog FareComponent { get; set; } = new();
}

public sealed class FareComponentLog
{
    public PriceLog Price { get; set; } = new();

    [XmlArray]
    [XmlArrayItem("Rbd")]
    public List<string> Rbd { get; set; } = [];

    public string FareSellkey { get; set; } = string.Empty;

    public string SegSellKey { get; set; } = string.Empty;

    [XmlArray]
    [XmlArrayItem("FarePaxType")]
    public List<FarePaxTypeLog> PaxType { get; set; } = [];
}

public sealed class PriceLog
{
    [XmlAttribute]
    public string? CurrencyCode { get; set; }

    public string TotalAmount { get; set; } = "0";

    public string TotalTaxAmount { get; set; } = "0";
}

public sealed class FarePaxTypeLog
{
    [XmlAttribute]
    public string Type { get; set; } = string.Empty;

    [XmlAttribute]
    public int Quantity { get; set; }

    [XmlArray]
    [XmlArrayItem("PassengerRef")]
    public List<string> PassengerRefs { get; set; } = [];

    [XmlArray]
    [XmlArrayItem("OfferItemId")]
    public List<string> PtcPricedCode { get; set; } = [];

    [XmlArray]
    [XmlArrayItem("Code")]
    public List<string> FareBasisCode { get; set; } = [];

    public string BaseAmount { get; set; } = "0";

    public string Tax { get; set; } = "0";

    public string TotalAmount { get; set; } = "0";

    public string PassengerTypeTotalAmount { get; set; } = "0";

    public TaxSummaryLog TaxSummary { get; set; } = new();

    [XmlArray]
    [XmlArrayItem("Direction")]
    public List<FareBreakupLog> Breakup { get; set; } = [];
}

public sealed class TaxSummaryLog
{
    public string TotalTaxAmount { get; set; } = "0";

    [XmlElement("Tax")]
    public List<TaxItemLog> Tax { get; set; } = [];
}

public sealed class TaxItemLog
{
    [XmlAttribute]
    public string? TaxCode { get; set; }

    [XmlAttribute]
    public string? TaxName { get; set; }

    public string? SuppAmount { get; set; }

    public string? Amount { get; set; }

    public bool SuppTax { get; set; }
}

public sealed class FareBreakupLog
{
    [XmlAttribute]
    public string? OriginDestinationRef { get; set; }

    [XmlAttribute]
    public string? OfferId { get; set; }

    [XmlAttribute]
    public string OfferItemId { get; set; } = string.Empty;

    [XmlAttribute]
    public string? FlightRef { get; set; }

    [XmlAttribute]
    public string FareKey { get; set; } = string.Empty;

    public string BaseAmount { get; set; } = "0";

    public string Tax { get; set; } = "0";

    public string TotalAmount { get; set; } = "0";

    public string SupplierTotalItemAmount { get; set; } = "0";
}
