using System.Globalization;

namespace NdcSoapModels;

public static class AirShoppingFareBuilder
{
    public static IReadOnlyList<JourneyPassengerFareSet> BuildJourneyPassengerFareSets(AirShoppingResponse? response)
    {
        if (response?.OffersGroup?.AirlineOffers?.Offers is not { Count: > 0 } offers)
        {
            return [];
        }

        var offersByOriginDestination = GroupOffersByOriginDestination(response, offers);
        if (offersByOriginDestination.Count == 0)
        {
            return [];
        }

        if (offersByOriginDestination.Count == 1)
        {
            return offersByOriginDestination[0].Offers
                .Select(outbound => BuildFareSet(outbound, returnOffer: null))
                .Where(fareSet => fareSet.PassengerFares.Count > 0)
                .ToList();
        }

        var outboundOffers = offersByOriginDestination[0].Offers;
        var returnOffers = offersByOriginDestination[1].Offers;
        var fareSets = new List<JourneyPassengerFareSet>();

        foreach (var outboundOffer in outboundOffers)
        {
            foreach (var returnOffer in returnOffers)
            {
                var fareSet = BuildFareSet(outboundOffer, returnOffer);
                if (fareSet.PassengerFares.Count > 0)
                {
                    fareSets.Add(fareSet);
                }
            }
        }

        return fareSets;
    }

    public static IReadOnlyList<LinkedPassengerOfferItem> BuildPassengerOfferItemLinks(Offer offer)
    {
        var offerItemById = offer.OfferItems
            .Where(item => !string.IsNullOrWhiteSpace(item.OfferItemID))
            .ToDictionary(item => item.OfferItemID!, StringComparer.OrdinalIgnoreCase);

        var linkedItems = new List<LinkedPassengerOfferItem>();

        foreach (var pricedPtc in offer.Parameters?.PtcPriced ?? [])
        {
            if (string.IsNullOrWhiteSpace(pricedPtc.Refs) ||
                !offerItemById.TryGetValue(pricedPtc.Refs, out var offerItem) ||
                offerItem.FareDetail is null)
            {
                continue;
            }

            var passengerTypeCode = FirstNotBlank(
                pricedPtc.Priced?.PassengerTypeCode,
                pricedPtc.Requested?.PassengerTypeCode,
                "UNK");

            var quantity = ParseQuantity(FirstNotBlank(
                pricedPtc.Priced?.Quantity,
                pricedPtc.Requested?.Quantity,
                "1"));

            linkedItems.Add(CreateLinkedItem(offer, offerItem, passengerTypeCode, quantity));
        }

        if (linkedItems.Count > 0)
        {
            return linkedItems;
        }

        return offer.OfferItems
            .Where(item => item.FareDetail is not null)
            .Select(item => CreateLinkedItem(offer, item, "UNK", SplitRefs(item.FareDetail?.PassengerRefs).Count))
            .ToList();
    }

    private static JourneyPassengerFareSet BuildFareSet(Offer outboundOffer, Offer? returnOffer)
    {
        var outboundItems = BuildPassengerOfferItemLinks(outboundOffer);
        var returnItemsByPtc = (returnOffer is null ? [] : BuildPassengerOfferItemLinks(returnOffer))
            .GroupBy(item => item.PassengerTypeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var passengerFares = new List<PassengerTypeFare>();
        var passengerTypeCodes = outboundItems
            .Select(item => item.PassengerTypeCode)
            .Concat(returnItemsByPtc.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(PassengerTypeSortOrder)
            .ThenBy(ptc => ptc, StringComparer.OrdinalIgnoreCase);

        foreach (var passengerTypeCode in passengerTypeCodes)
        {
            var outboundByPtc = outboundItems
                .Where(item => string.Equals(item.PassengerTypeCode, passengerTypeCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
            returnItemsByPtc.TryGetValue(passengerTypeCode, out var returnByPtc);
            returnByPtc ??= [];

            var maxCount = Math.Max(outboundByPtc.Count, returnByPtc.Count);
            for (var index = 0; index < maxCount; index++)
            {
                var outboundItem = index < outboundByPtc.Count ? outboundByPtc[index] : null;
                var returnItem = TakeBestReturnMatch(outboundItem, returnByPtc);
                var breakdowns = new[] { outboundItem, returnItem }
                    .Where(item => item is not null)
                    .Select(item => BuildBreakdown(item!))
                    .ToList();

                if (breakdowns.Count == 0)
                {
                    continue;
                }

                var quantity = Math.Max(outboundItem?.Quantity ?? 0, returnItem?.Quantity ?? 0);
                if (quantity <= 0)
                {
                    quantity = 1;
                }

                var perPassengerBase = breakdowns.Sum(part => part.PerPassengerBaseAmount);
                var perPassengerTax = breakdowns.Sum(part => part.PerPassengerTaxAmount);
                var passengerRefs = breakdowns
                    .SelectMany(part => part.PassengerRefs)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                passengerFares.Add(new PassengerTypeFare
                {
                    PassengerTypeCode = passengerTypeCode,
                    Quantity = quantity,
                    CurrencyCode = FirstNotBlank(breakdowns.Select(part => part.CurrencyCode).ToArray()),
                    PassengerRefs = passengerRefs,
                    OfferItemIds = breakdowns.Select(part => part.OfferItemId).ToList(),
                    PerPassengerBaseAmount = perPassengerBase,
                    PerPassengerTaxAmount = perPassengerTax,
                    PerPassengerTotalAmount = perPassengerBase + perPassengerTax,
                    TotalAmountForPassengerType = (perPassengerBase + perPassengerTax) * quantity,
                    SupplierTotalItemAmount = breakdowns.Sum(part => part.SupplierTotalItemAmount),
                    Breakdowns = breakdowns
                });
            }
        }

        return new JourneyPassengerFareSet
        {
            OutboundOfferId = outboundOffer.OfferID,
            ReturnOfferId = returnOffer?.OfferID,
            OfferIds = new[] { outboundOffer.OfferID, returnOffer?.OfferID }
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .ToList(),
            FareSellKey = string.Join("|", passengerFares.SelectMany(fare => fare.OfferItemIds)),
            PassengerFares = passengerFares
        };
    }

    private static LinkedPassengerOfferItem CreateLinkedItem(
        Offer offer,
        OfferItem offerItem,
        string passengerTypeCode,
        int quantity)
    {
        var flightRef = offer.FlightsOverview?.FlightRefs.FirstOrDefault();
        var fareComponent = offerItem.FareDetail?.FareComponents.FirstOrDefault();

        return new LinkedPassengerOfferItem
        {
            Offer = offer,
            OfferItem = offerItem,
            PassengerTypeCode = passengerTypeCode,
            Quantity = Math.Max(quantity, 1),
            OriginDestinationRef = flightRef?.ODRef,
            FlightRef = flightRef?.Value,
            FareKey = BuildFareKey(offerItem),
            CabinCode = fareComponent?.FareBasis?.CabinType?.CabinTypeCode,
            CabinName = fareComponent?.FareBasis?.CabinType?.CabinTypeName,
            PriceClassRef = fareComponent?.PriceClassRef,
            Rbd = fareComponent?.FareBasis?.Rbd,
            FareBasisCode = fareComponent?.FareBasis?.FareBasisCode?.Code
        };
    }

    private static DirectionalFareBreakdown BuildBreakdown(LinkedPassengerOfferItem item)
    {
        var fareDetail = item.OfferItem.FareDetail;
        var price = fareDetail?.Price;
        var baseAmount = ReadAmount(price?.BaseAmount);
        var taxAmount = ReadAmount(price?.Taxes?.Total);

        return new DirectionalFareBreakdown
        {
            OriginDestinationRef = item.OriginDestinationRef,
            OfferId = item.Offer.OfferID,
            OfferItemId = item.OfferItem.OfferItemID ?? string.Empty,
            FlightRef = item.FlightRef,
            FareKey = item.FareKey,
            CabinCode = item.CabinCode,
            CabinName = item.CabinName,
            PriceClassRef = item.PriceClassRef,
            Rbd = item.Rbd,
            FareBasisCode = item.FareBasisCode,
            CurrencyCode = FirstNotBlank(
                price?.BaseAmount?.Code,
                price?.Taxes?.Total?.Code,
                item.OfferItem.TotalPriceDetail?.TotalAmount?.DetailCurrencyPrice?.Total?.Code),
            Quantity = item.Quantity,
            PassengerRefs = SplitRefs(fareDetail?.PassengerRefs),
            PerPassengerBaseAmount = baseAmount,
            PerPassengerTaxAmount = taxAmount,
            PerPassengerTotalAmount = baseAmount + taxAmount,
            SupplierTotalItemAmount = ReadAmount(item.OfferItem.TotalPriceDetail?.TotalAmount?.DetailCurrencyPrice?.Total),
            Taxes = price?.Taxes?.Breakdown?.Taxes
                .Select(tax => new TaxAmount
                {
                    TaxCode = tax.TaxCode,
                    Description = tax.Description,
                    CurrencyCode = tax.Amount?.Code,
                    Amount = ReadAmount(tax.Amount)
                })
                .ToList() ?? []
        };
    }

    private static LinkedPassengerOfferItem? TakeBestReturnMatch(
        LinkedPassengerOfferItem? outboundItem,
        List<LinkedPassengerOfferItem> returnItems)
    {
        if (returnItems.Count == 0)
        {
            return null;
        }

        var matchIndex = outboundItem is null
            ? 0
            : returnItems.FindIndex(item => string.Equals(item.FareKey, outboundItem.FareKey, StringComparison.OrdinalIgnoreCase));

        if (matchIndex < 0)
        {
            matchIndex = 0;
        }

        var match = returnItems[matchIndex];
        returnItems.RemoveAt(matchIndex);
        return match;
    }

    private static List<OriginDestinationOffers> GroupOffersByOriginDestination(
        AirShoppingResponse response,
        IReadOnlyList<Offer> offers)
    {
        var groupedOffers = offers
            .Select(offer => new { Offer = offer, FlightRef = offer.FlightsOverview?.FlightRefs.FirstOrDefault() })
            .Where(item => !string.IsNullOrWhiteSpace(item.FlightRef?.ODRef))
            .GroupBy(item => item.FlightRef!.ODRef!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Offer).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var orderedKeys = response.DataLists?.OriginDestinationList?.OriginDestinations
            .Select(originDestination => originDestination.OriginDestinationKey)
            .Where(key => !string.IsNullOrWhiteSpace(key) && groupedOffers.ContainsKey(key!))
            .Select(key => key!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        orderedKeys.AddRange(groupedOffers.Keys.Where(key => !orderedKeys.Contains(key, StringComparer.OrdinalIgnoreCase)));

        return orderedKeys
            .Select(key => new OriginDestinationOffers { OriginDestinationRef = key, Offers = groupedOffers[key] })
            .ToList();
    }

    private static string BuildFareKey(OfferItem offerItem)
    {
        var fareComponent = offerItem.FareDetail?.FareComponents.FirstOrDefault();
        var cabin = fareComponent?.FareBasis?.CabinType?.CabinTypeCode ?? string.Empty;
        var priceClass = fareComponent?.PriceClassRef ?? string.Empty;
        var rbd = fareComponent?.FareBasis?.Rbd ?? string.Empty;
        var fareBasis = fareComponent?.FareBasis?.FareBasisCode?.Code ?? string.Empty;

        return string.Join("|", cabin, priceClass, rbd, fareBasis);
    }

    private static decimal ReadAmount(CurrencyAmount? amount)
    {
        return decimal.TryParse(amount?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;
    }

    private static int ParseQuantity(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) && quantity > 0
            ? quantity
            : 1;
    }

    private static List<string> SplitRefs(string? refs)
    {
        return refs?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? [];
    }

    private static string FirstNotBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static int PassengerTypeSortOrder(string passengerTypeCode)
    {
        return passengerTypeCode.ToUpperInvariant() switch
        {
            "ADT" => 0,
            "CHD" => 1,
            "CNN" => 1,
            "INF" => 2,
            _ => 3
        };
    }
}

public sealed class JourneyPassengerFareSet
{
    public string? OutboundOfferId { get; init; }

    public string? ReturnOfferId { get; init; }

    public IReadOnlyList<string> OfferIds { get; init; } = [];

    public string FareSellKey { get; init; } = string.Empty;

    public IReadOnlyList<PassengerTypeFare> PassengerFares { get; init; } = [];
}

public sealed class PassengerTypeFare
{
    public string PassengerTypeCode { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public IReadOnlyList<string> PassengerRefs { get; init; } = [];

    public IReadOnlyList<string> OfferItemIds { get; init; } = [];

    public decimal PerPassengerBaseAmount { get; init; }

    public decimal PerPassengerTaxAmount { get; init; }

    public decimal PerPassengerTotalAmount { get; init; }

    public decimal TotalAmountForPassengerType { get; init; }

    public decimal SupplierTotalItemAmount { get; init; }

    public IReadOnlyList<DirectionalFareBreakdown> Breakdowns { get; init; } = [];
}

public sealed class DirectionalFareBreakdown
{
    public string? OriginDestinationRef { get; init; }

    public string? OfferId { get; init; }

    public string OfferItemId { get; init; } = string.Empty;

    public string? FlightRef { get; init; }

    public string FareKey { get; init; } = string.Empty;

    public string? CabinCode { get; init; }

    public string? CabinName { get; init; }

    public string? PriceClassRef { get; init; }

    public string? Rbd { get; init; }

    public string? FareBasisCode { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public IReadOnlyList<string> PassengerRefs { get; init; } = [];

    public decimal PerPassengerBaseAmount { get; init; }

    public decimal PerPassengerTaxAmount { get; init; }

    public decimal PerPassengerTotalAmount { get; init; }

    public decimal SupplierTotalItemAmount { get; init; }

    public IReadOnlyList<TaxAmount> Taxes { get; init; } = [];
}

public sealed class TaxAmount
{
    public string? TaxCode { get; init; }

    public string? Description { get; init; }

    public string? CurrencyCode { get; init; }

    public decimal Amount { get; init; }
}

public sealed class LinkedPassengerOfferItem
{
    public required Offer Offer { get; init; }

    public required OfferItem OfferItem { get; init; }

    public string PassengerTypeCode { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string? OriginDestinationRef { get; init; }

    public string? FlightRef { get; init; }

    public string FareKey { get; init; } = string.Empty;

    public string? CabinCode { get; init; }

    public string? CabinName { get; init; }

    public string? PriceClassRef { get; init; }

    public string? Rbd { get; init; }

    public string? FareBasisCode { get; init; }
}

internal sealed class OriginDestinationOffers
{
    public string OriginDestinationRef { get; init; } = string.Empty;

    public IReadOnlyList<Offer> Offers { get; init; } = [];
}
