using SupplierFlightPricing;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: SupplierFlightPricing.Tests <path-to-offer-price-response.xml>");
    return 2;
}

await using var stream = File.OpenRead(args[0]);
var response = SupplierFlightPricingResponse.Deserialize(stream);
var offerPrice = response.Body?.TransactionResponse?.Rsp?.OfferPrice
    ?? throw new InvalidOperationException("OfferPriceRS was not deserialized.");
var pricedOffer = offerPrice.PricedOffer
    ?? throw new InvalidOperationException("PricedOffer was not deserialized.");
var dataLists = offerPrice.DataLists
    ?? throw new InvalidOperationException("DataLists were not deserialized.");
var otherMetadata = offerPrice.Metadata?.Other?.OtherMetadata
    ?? throw new InvalidOperationException("Metadata was not deserialized.");

AssertEqual("17.2", offerPrice.Version, nameof(offerPrice.Version));
AssertEqual("702471085534928", offerPrice.TransactionIdentifier, nameof(offerPrice.TransactionIdentifier));
AssertEqual("WY", offerPrice.ShoppingResponseId?.Owner, "ShoppingResponseID.Owner");
AssertEqual("PF0632874-6F94-41FF-BE49-1", pricedOffer.OfferId, nameof(pricedOffer.OfferId));
AssertEqual("WY", pricedOffer.Owner, nameof(pricedOffer.Owner));
AssertEqual(273400m, pricedOffer.TotalPrice?.DetailCurrencyPrice?.Total?.Value, "PricedOffer.TotalPrice");
AssertEqual("BHD", pricedOffer.TotalPrice?.DetailCurrencyPrice?.Total?.Code, "PricedOffer.TotalPrice.Code");
AssertEqual(3, pricedOffer.Parameters?.TotalItemQuantity, "Parameters.TotalItemQuantity");
AssertEqual(3, pricedOffer.Parameters?.PassengerTypePriced.Count, "Parameters.PTC_Priced count");
AssertEqual(2, pricedOffer.FlightsOverview?.FlightRefs.Count, "FlightsOverview.FlightRef count");
AssertEqual(3, pricedOffer.OfferItems.Count, "PricedOffer.OfferItem count");
AssertEqual(73, pricedOffer.OfferItems.Sum(item => item.Services.Count), "OfferItem.Service total count");
AssertEqual(10, pricedOffer.BaggageAllowances.Count, "PricedOffer.BaggageAllowance count");
AssertEqual(4, dataLists.PassengerList?.Passengers.Count, "PassengerList.Passenger count");
AssertEqual(10, dataLists.BaggageAllowanceList?.BaggageAllowances.Count, "BaggageAllowanceList.BaggageAllowance count");
AssertEqual(6, dataLists.FareList?.FareGroups.Count, "FareList.FareGroup count");
AssertEqual(2, dataLists.FlightSegmentList?.FlightSegments.Count, "FlightSegmentList.FlightSegment count");
AssertEqual(2, dataLists.FlightList?.Flights.Count, "FlightList.Flight count");
AssertEqual(2, dataLists.OriginDestinationList?.OriginDestinations.Count, "OriginDestinationList.OriginDestination count");
AssertEqual(1, dataLists.PriceClassList?.PriceClasses.Count, "PriceClassList.PriceClass count");
AssertEqual(7, dataLists.ServiceDefinitionList?.ServiceDefinitions.Count, "ServiceDefinitionList.ServiceDefinition count");

var fareRefKeyCount = otherMetadata
    .SelectMany(metadata => (IEnumerable<PriceMetadata>?)metadata.PriceMetadatas?.PriceMetadata ?? Enumerable.Empty<PriceMetadata>())
    .Count(metadata => !string.IsNullOrWhiteSpace(metadata.AugmentationPoint?.AugPoint?.FareRefKey));
AssertEqual(6, fareRefKeyCount, "Metadata.PriceMetadata FareRefKey count");

Console.WriteLine("Supplier pricing response deserialized successfully.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}
