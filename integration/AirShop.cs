using Logger;
using Logger.Interfcae;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using XchangeFlightService.Contracts.Common;
using XchangeFlightService.Contracts.Credential;
using XchangeFlightService.Interfaces;
using XchangeFlightService.Models.Common;
using XchangeFlightService.Models.FlightSearch;
using XchangeFlightService.OmanNDC.Common;
using XchangeFlightService.OmanNDC.Converter.AirSearch;
using static XchangeFlightService.Contracts.Common.Helpers;
namespace XchangeFlightService.OmanNDC
{
    internal class AirShop : IAvailabilityEngine
    {
        private readonly CommonLayer _commonLayer;
        private readonly clsLogWriter _logger;
        private readonly IConfiguration _configuration;
        private string CabinPreference = "Y";
        public AirShop(CommonLayer CommonLayer, ILogWriter logger, IConfiguration configuration)
        {
            _commonLayer = CommonLayer;
            _logger = new clsLogWriter(configuration);
            _configuration = configuration;
        }
        private bool IsCredentials = false;
        DynamicProperties property = new();
        private string? SupplierId = string.Empty;
        private AirSearchContext? context;

        public async Task<SearchResponse> AirShopping(SearchRequest searchRequest, LoginResponse loginResponse, CancellationToken cancellationToken)
        {
            SearchResponse FinalResult = new SearchResponse();
            string? CallSupplierCred = string.Empty;
            string? sDepCountryCode = string.Empty;
            try
            {
                SupplierId = searchRequest.AirShoppingRq?.DistributionChain?.DistributionChainLink?.FirstOrDefault()?.ParticipatingOrg?.OrgId;
                _logger.ppLogFolderName = $"{Constants.Logging.Folder.Search.Path}{OmanNDCHelper.General.MapperName}";
                _logger.SetLogger(_commonLayer.CompanyID!, $"{_commonLayer.sSessionId}_{SupplierId}_AirShopping.txt");


                _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} AirShopping Start :{Environment.NewLine}  Payload : {Environment.NewLine}", searchRequest!, LogEventNames.AirShopping, blnWriteComplete: true);
                if (loginResponse != null)
                {

                    CallSupplierCred = await _commonLayer.CallSupplierAsync(loginResponse, _configuration["ApiBaseUrl:BaseUrl"] + ApiConstants.SupplierCredential, SupplierId, _logger);
                }
                else
                {
                    _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirBooking, LogEventNames.AirBooking, $"{Environment.NewLine} Supplier Credentials exception for {SupplierId} : {Environment.NewLine}{CallSupplierCred}", blnWriteComplete: true);
                    FinalResult.Status = false;
                    FinalResult.SupplierResponse = new List<SupplierResponse>
                    {
                        new SupplierResponse
                        {
                            Status = false,
                            SourceSupplierId = SupplierId,
                            SupplierCode = "WYNDC",
                            Message = MessageConstants.Authentication.NoTokens
                        }
                    };
                    return FinalResult;
                }
                CredentialRoot? credentials = JsonSerializer.Deserialize<CredentialRoot>(CallSupplierCred!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                (IsCredentials, property) = await _commonLayer.SetCredentials(credentials!, _logger);

                if (IsCredentials)
                {
                    context = new AirSearchContext(searchRequest, credentials, property);

                    CabinPreference = searchRequest.AirShoppingRq?.Request?.FlightRelatedCriteria?.CabinCriteria?.FirstOrDefault()?.CabinTypeCode switch
                    {
                        "1" => "F",
                        "2" => "C",
                        "3" => "Y",
                        "4" => "W",
                        _ => "Y" // or null, depending on your requirement
                    };



                    var airSearchResponse = await CreateSearchRequest(searchRequest);
                    FinalResult = ConvertToQLFormat(searchRequest, airSearchResponse);



                }
            }
            catch (OperationCanceledException ex)
            {
                _ = _logger.WriteLogEntry(enuLogEntryType.Error, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} Request Cancelled : {Environment.NewLine}", ex.GetDetails(), LogEventNames.AirShopping, blnWriteComplete: true);

                throw;
            }
            catch (Exception ex)
            {
                SearchResponse results = new SearchResponse();
                results.Status = false;
                results.SupplierResponse = new List<SupplierResponse>
                {
                    new SupplierResponse
                    {
                        Status = false,
                        SourceSupplierId = SupplierId,
                        SupplierCode = "WYNDC" ,
                        Message = ex.Message
                    }
                };
                _ = _logger.WriteLogEntry(enuLogEntryType.Error, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} Unhandled Exception : {Environment.NewLine}", ex.GetDetails(), LogEventNames.AirShopping, blnWriteComplete: true);
                return results;
            }
            _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} Final Result {Environment.NewLine}", FinalResult, LogEventNames.AirShopping, blnWriteComplete: true);

            return FinalResult;
        }

        private SearchResponse ConvertToQLFormat(SearchRequest searchRequest, AirShoppingResponseEnvelope airSearchResponse)
        {
            _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} Executing ConvertToQLFormate Start : {Environment.NewLine}", LogEventNames.AirShopping, blnWriteComplete: true);
            var paxList = context!.PaxList?.Pax;

            List<ResponseOffer> offers = airSearchResponse?.Body?.TransactionResponse?.ResponsePayload?.AirShoppingResponse?.OffersGroup?.AirlineOffers?.Offers ?? new List<ResponseOffer>();
            Dictionary<string, List<ResponseOffer>> flightsByOD = offers.Where((ResponseOffer offer) => offer.FlightsOverview?.FlightRef?.ODRef != null).GroupBy((ResponseOffer offer) => offer.FlightsOverview!.FlightRef!.ODRef!).ToDictionary((IGrouping<string, ResponseOffer> group) => group.Key, (IGrouping<string, ResponseOffer> group) => group.ToList());
            string? od1 = flightsByOD.Keys.FirstOrDefault();
            if (!flightsByOD.TryGetValue(od1!, out List<ResponseOffer>? owFlights) || owFlights is not { Count: > 0 })
            {
                return new SearchResponse
                {
                    Status = true,
                    SupplierResponse = new List<SupplierResponse> {
                        new SupplierResponse {
                            Status = true,
                            SourceSupplierId = SupplierId,
                            SupplierCode ="WYNDC",
                            Message = AirShopMessages.NoTokens
                        }
                    }
                };
            }

            List<Flight> flights = airSearchResponse?.Body?.TransactionResponse?.ResponsePayload?.AirShoppingResponse?.DataLists?.FlightList?.Flights ?? new List<Flight>();
            Dictionary<string, Flight> flightLookup = flights.Where((Flight flight) => !string.IsNullOrWhiteSpace(flight.FlightKey)).ToDictionary((Flight flight) => flight.FlightKey!, (Flight flight) => flight);
            List<FlightSegment> flightSegments = airSearchResponse?.Body?.TransactionResponse?.ResponsePayload?.AirShoppingResponse?.DataLists?.FlightSegmentList?.FlightSegments ?? new List<FlightSegment>();
            Dictionary<string, FlightSegment> flightSegmentLookup = flightSegments.Where((FlightSegment segment) => !string.IsNullOrWhiteSpace(segment.SegmentKey)).ToDictionary((FlightSegment segment) => segment.SegmentKey!, (FlightSegment segment) => segment);

            List<Offer> offerList = new();
            bool checkRT = flightsByOD.Count > 1;

            List<ResponseOffer> owflights = new();
            List<ResponseOffer> rtflights = new();

            string? outboundOD = flightsByOD.Keys.FirstOrDefault();
            if (!string.IsNullOrEmpty(outboundOD))
            {
                flightsByOD.TryGetValue(outboundOD, out owflights!);
                owflights ??= new List<ResponseOffer>();
            }

            if (checkRT)
            {
                string? returnOD = flightsByOD.Keys.Skip(1).FirstOrDefault();

                if (!string.IsNullOrEmpty(returnOD))
                {
                    flightsByOD.TryGetValue(returnOD, out rtflights!);
                    rtflights ??= new List<ResponseOffer>();
                }
            }

            int i = 0;
            for (int oc = 0; oc < owflights?.Count; oc++)
            {
                var Oneway_Return = checkRT ? rtflights!.Count() : 1;
                for (int rc = 0; rc < Oneway_Return; rc++)
                {
                    List<ResponseOffer> journeys = new List<ResponseOffer>();
                    journeys.Add(owflights![oc]);
                    if (checkRT) { journeys.Add(rtflights![rc]); }
                    var offer = new Offer
                    {
                        OfferID = searchRequest?.AirShoppingRq?.BoundWiseFareSearchSequence + "-" + SupplierId + "-" + ++i,
                        OwnerId = SupplierId,
                        OwnerCode = "",
                        OfferItem = new OfferItem
                        {
                            FareDetail = new List<FareDetail>()
                        },
                        SegmentList = new SegmentList
                        {
                            Segments = new List<Segments>()
                        }
                    };
                    var specialServices = new List<SpecialService>();
                    #region Build Segment
                    int seg_idx = 0;
                    foreach (var journey in journeys)
                    {
                        Segments segmentContainer = new()
                        {
                            SegmentInfo = new List<SegmentInfo>()
                        };

                        FlightRef? flightRef = journey.FlightsOverview?.FlightRef;

                        if (flightRef != null &&
                            !string.IsNullOrWhiteSpace(flightRef.Value) &&
                            flightLookup.TryGetValue(flightRef.Value, out Flight? flight))
                        {
                            string[] segmentKeys = flight.SegmentReferences?.Value?
                                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                ?? Array.Empty<string>();

                            foreach (string segmentKey in segmentKeys)
                            {
                                if (!flightSegmentLookup.TryGetValue(segmentKey, out FlightSegment? segment))
                                    continue;

                                seg_idx++;

                                segmentContainer.SegmentInfo.Add(new SegmentInfo
                                {
                                    SegmentID = $"SEG{seg_idx}",

                                    DepartureAirport = segment.Departure?.AirportCode,
                                    ArrivalAirport = segment.Arrival?.AirportCode,

                                    DepartureDate = segment.Departure?.Date,
                                    DepartureTime = segment.Departure?.Time,

                                    ArrivalDate = segment.Arrival?.Date,
                                    ArrivalTime = segment.Arrival?.Time,

                                    Airline = segment.MarketingCarrier?.AirlineId,
                                    MarketingAirline = segment.MarketingCarrier?.AirlineId,
                                    OperatingAirline = segment.MarketingCarrier?.AirlineId,

                                    FlightNumber = segment.MarketingCarrier?.FlightNumber,

                                    AirCraftType = segment.Equipment?.AircraftCode,

                                    Duration = segment.FlightDetail?.FlightDuration?.Value
                                });
                            }
                        }

                        offer.SegmentList.Segments.Add(segmentContainer);
                    }
                    #endregion Build Segment

                    List<LinkedOfferFareItem> outboundFareItems = BuildPassengerOfferItemLinks(owflights[oc]);
                    List<LinkedOfferFareItem> returnFareItems = checkRT ? BuildPassengerOfferItemLinks(rtflights![rc]) : new List<LinkedOfferFareItem>();
                    Dictionary<string, List<LinkedOfferFareItem>> returnLookup = returnFareItems
                        .GroupBy((LinkedOfferFareItem item) => item.PassengerTypeCode, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            (IGrouping<string, LinkedOfferFareItem> group) => group.Key,
                            (IGrouping<string, LinkedOfferFareItem> group) => group.ToList(),
                            StringComparer.OrdinalIgnoreCase);

                    #region Build Fare
                    int j = 0;
                    foreach (LinkedOfferFareItem outboundFareItem in outboundFareItems)
                    {
                        LinkedOfferFareItem? returnFareItem = null;
                        if (checkRT)
                        {
                            if (!returnLookup.TryGetValue(outboundFareItem.PassengerTypeCode, out List<LinkedOfferFareItem>? samePaxReturnItems) ||
                                samePaxReturnItems.Count == 0)
                            {
                                continue;
                            }

                            int returnIndex = samePaxReturnItems.FindIndex((LinkedOfferFareItem item) =>
                                string.Equals(item.FareKey, outboundFareItem.FareKey, StringComparison.OrdinalIgnoreCase));
                            if (returnIndex < 0)
                            {
                                returnIndex = 0;
                            }

                            returnFareItem = samePaxReturnItems[returnIndex];
                            samePaxReturnItems.RemoveAt(returnIndex);
                        }

                        ResponseOfferItem owFare = outboundFareItem.OfferItem;
                        ResponseOfferItem? rtFare = returnFareItem?.OfferItem;

                        if (owFare.FareDetail == null)
                            continue;
                        j++;

                        decimal owBase = ReadAmount(owFare.FareDetail.Price?.BaseAmount?.Value);
                        decimal owTax = ReadAmount(owFare.FareDetail.Price?.Taxes?.Total?.Value);
                        decimal rtBase = 0m;
                        decimal rtTax = 0m;

                        if (rtFare?.FareDetail != null)
                        {
                            rtBase = ReadAmount(rtFare.FareDetail.Price?.BaseAmount?.Value);
                            rtTax = ReadAmount(rtFare.FareDetail.Price?.Taxes?.Total?.Value);
                        }

                        int paxQuantity = Math.Max(outboundFareItem.Quantity, returnFareItem?.Quantity ?? 0);
                        if (paxQuantity <= 0)
                        {
                            paxQuantity = 1;
                        }

                        decimal perPaxBase = owBase + rtBase;
                        decimal perPaxTax = owTax + rtTax;
                        decimal perPaxTotal = perPaxBase + perPaxTax;
                        string currencyCode = FirstNotBlank(
                            owFare.FareDetail.Price?.BaseAmount?.Code,
                            owFare.FareDetail.Price?.Taxes?.Total?.Code,
                            rtFare?.FareDetail?.Price?.BaseAmount?.Code,
                            rtFare?.FareDetail?.Price?.Taxes?.Total?.Code);
                        List<string> passengerRefs = outboundFareItem.PassengerRefs
                            .Concat(returnFareItem?.PassengerRefs ?? new List<string>())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        FareDetail fareDetail = new()
                        {
                            OfferItemID = $"F{j}",
                            FareType = "",
                            CabinClass =
                                owFare.FareDetail
                                      .FareComponent?
                                      .FareBasis?
                                      .CabinType?
                                      .CabinTypeName,

                            BrandName = "",
                            BrandId = "",
                            FareSource = "CRS",
                            PromoCode = "",
                            FareComponent = new FareComponent
                            {
                                Guid = "",
                                FareSellkey = string.Join(Constants.General.Separator, new[]{
                                    owFare.OfferItemID,
                                    rtFare?.OfferItemID
                                }.Where(x => !string.IsNullOrWhiteSpace(x))),
                                SegSellKey = string.Join(Constants.General.Separator, journeys.Select(x => x.OfferID)),
                                Rbd = new[]{
                                    owFare.FareDetail.FareComponent?.FareBasis?.Rbd,
                                    rtFare?.FareDetail?.FareComponent?.FareBasis?.Rbd
                                }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct().ToList(),
                                PaxType = new List<FarePaxType>()
                            },
                            MiscInfo = new MiscInfo
                            {
                                SpecialServices = specialServices
                            }
                        };

                        fareDetail.FareComponent.PaxType.Add(BuildFarePaxType(
                            outboundFareItem.PassengerTypeCode,
                            paxQuantity,
                            perPaxBase,
                            perPaxTax,
                            perPaxTotal,
                            perPaxTotal * paxQuantity,
                            currencyCode,
                            passengerRefs));
                        offer.OfferItem.FareDetail.Add(fareDetail);
                    }
                        offerList.Add(offer);
                    #endregion Build Fare
                }



            }














            var response = new SearchResponse
            {
                Status = true,
                SupplierResponse = new List<SupplierResponse> {
                    new SupplierResponse
                {
                        Status = true,
                        SourceSupplierId = SupplierId,
                        SupplierCode ="WYNDC",
                        Message = AirShopMessages.StatusSuccess
                } },
                AirShoppingRS = new AirShoppingRS
                {
                    Response = new Response
                    {
                        Processing = new Processing
                        {
                            CurParameter = new List<CurParameter>()
                            {
                                new CurParameter
                                {
                                    Name = "Company",
                                    CurCode = "",
                                    DecimalsAllowedNumber = 2
                                },
                                new CurParameter
                                {
                                    Name = "Customer",
                                    CurCode = context?.SupplierCreds.Currency,
                                    DecimalsAllowedNumber = 2
                                },
                                new CurParameter {
                                    Name= "Supplier",
                                    CurCode= ""
                                }
                            }
                        },
                        DataLists = new DataLists
                        {
                            PaxList = new PaxList { Pax = paxList }
                        },

                        OffersGroup = new OffersGroup
                        {
                            CarrierOffers = new List<CarrierOffers> { new CarrierOffers { Offer = offerList } }
                        }
                    }
                }
            };
            _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} Final Search Response : {Environment.NewLine}", response, LogEventNames.AirShopping, blnWriteComplete: true);
            _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirShopping, $"{OmanNDCHelper.General.MapperName} Executing ConvertToQLFormate end : {Environment.NewLine}", LogEventNames.AirShopping, blnWriteComplete: true);

            return response;
        }
        private static List<LinkedOfferFareItem> BuildPassengerOfferItemLinks(ResponseOffer offer)
        {
            Dictionary<string, ResponseOfferItem> offerItemById = offer.OfferItems?
                .Where((ResponseOfferItem item) => !string.IsNullOrWhiteSpace(item.OfferItemID))
                .ToDictionary(
                    (ResponseOfferItem item) => item.OfferItemID!,
                    (ResponseOfferItem item) => item,
                    StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, ResponseOfferItem>(StringComparer.OrdinalIgnoreCase);

            List<LinkedOfferFareItem> linkedItems = new();
            foreach (object pricedPtc in GetEnumerableProperty(offer.Parameters, "PtcPriced", "PTC_Priced", "PTC_Priceds"))
            {
                string refs = FirstNotBlank(GetStringProperty(pricedPtc, "Refs", "refs", "Reference", "ReferenceId"));
                if (string.IsNullOrWhiteSpace(refs) ||
                    !offerItemById.TryGetValue(refs, out ResponseOfferItem? offerItem) ||
                    offerItem.FareDetail == null)
                {
                    continue;
                }

                object? priced = GetObjectProperty(pricedPtc, "Priced");
                object? requested = GetObjectProperty(pricedPtc, "Requested");
                string passengerTypeCode = FirstNotBlank(
                    GetPassengerTypeCode(priced),
                    GetPassengerTypeCode(requested),
                    "UNK");
                int quantity = ParseQuantity(FirstNotBlank(
                    GetStringProperty(priced, "Quantity"),
                    GetStringProperty(requested, "Quantity"),
                    "1"));

                linkedItems.Add(CreateLinkedOfferFareItem(offerItem, passengerTypeCode, quantity));
            }

            if (linkedItems.Count > 0)
            {
                return linkedItems;
            }

            return offer.OfferItems?
                .Where((ResponseOfferItem item) => item.FareDetail != null)
                .Select((ResponseOfferItem item) => CreateLinkedOfferFareItem(
                    item,
                    "UNK",
                    Math.Max(SplitRefs(item.FareDetail?.PassengerRefs).Count, 1)))
                .ToList() ?? new List<LinkedOfferFareItem>();
        }

        private static LinkedOfferFareItem CreateLinkedOfferFareItem(ResponseOfferItem offerItem, string passengerTypeCode, int quantity)
        {
            return new LinkedOfferFareItem
            {
                OfferItem = offerItem,
                PassengerTypeCode = passengerTypeCode,
                Quantity = Math.Max(quantity, 1),
                FareKey = GetFareKey(offerItem),
                PassengerRefs = SplitRefs(offerItem.FareDetail?.PassengerRefs)
            };
        }

        private static FarePaxType BuildFarePaxType(
            string passengerTypeCode,
            int quantity,
            decimal perPaxBase,
            decimal perPaxTax,
            decimal perPaxTotal,
            decimal passengerTypeTotal,
            string currencyCode,
            List<string> passengerRefs)
        {
            FarePaxType paxType = new();

            SetPropertyIfExists(paxType, passengerTypeCode, "PaxType", "Ptc", "PTC", "PassengerType", "PassengerTypeCode", "Code");
            SetPropertyIfExists(paxType, quantity, "Quantity", "PaxQuantity", "PassengerQuantity", "PaxCount", "Count");
            SetPropertyIfExists(paxType, currencyCode, "Currency", "CurrencyCode", "CurCode");
            SetPropertyIfExists(paxType, passengerRefs, "PassengerRefs", "PaxRefs", "PaxRef", "PassengerRef");
            SetPropertyIfExists(paxType, perPaxBase, "BaseAmount", "BaseFare", "Base", "PerPaxBaseAmount", "PerPassengerBaseAmount");
            SetPropertyIfExists(paxType, perPaxTax, "TaxAmount", "Tax", "PerPaxTaxAmount", "PerPassengerTaxAmount");
            SetPropertyIfExists(paxType, perPaxTotal, "TotalAmount", "TotalFare", "Total", "PerPaxTotalAmount", "PerPassengerTotalAmount");
            SetPropertyIfExists(paxType, passengerTypeTotal, "PassengerTypeTotal", "TotalAmountForPassengerType", "TotalForPaxType", "PaxTypeTotalAmount");

            return paxType;
        }

        private static decimal ReadAmount(string? value)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
                ? amount
                : 0m;
        }

        private static int ParseQuantity(string? value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) && quantity > 0
                ? quantity
                : 1;
        }

        private static List<string> SplitRefs(string? refs)
        {
            return refs?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new List<string>();
        }

        private static string FirstNotBlank(params string?[] values)
        {
            return values.FirstOrDefault((string? value) => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static IEnumerable<object> GetEnumerableProperty(object? source, params string[] propertyNames)
        {
            object? value = GetObjectProperty(source, propertyNames);
            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                return enumerable.Cast<object>();
            }

            return Enumerable.Empty<object>();
        }

        private static object? GetObjectProperty(object? source, params string[] propertyNames)
        {
            if (source == null)
            {
                return null;
            }

            foreach (string propertyName in propertyNames)
            {
                PropertyInfo? property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    return property.GetValue(source);
                }
            }

            return null;
        }

        private static string? GetStringProperty(object? source, params string[] propertyNames)
        {
            object? value = GetObjectProperty(source, propertyNames);
            return value?.ToString();
        }

        private static string? GetPassengerTypeCode(object? passengerTypeNode)
        {
            if (passengerTypeNode is string passengerTypeCode)
            {
                return passengerTypeCode;
            }

            return FirstNotBlank(
                GetStringProperty(passengerTypeNode, "PassengerTypeCode", "PaxType", "Ptc", "PTC", "Value", "Text"),
                passengerTypeNode?.GetType().IsPrimitive == true ? passengerTypeNode.ToString() : null);
        }

        private static void SetPropertyIfExists(object target, object? value, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property == null || !property.CanWrite)
                {
                    continue;
                }

                try
                {
                    object? convertedValue = ConvertToPropertyType(value, property.PropertyType);
                    property.SetValue(target, convertedValue);
                    return;
                }
                catch
                {
                    // Keep integration tolerant across contract versions with different FarePaxType shapes.
                }
            }
        }

        private static object? ConvertToPropertyType(object? value, Type propertyType)
        {
            if (value == null)
            {
                return null;
            }

            Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            if (targetType == typeof(string))
            {
                return value is IEnumerable<string> refs ? string.Join(" ", refs) : value.ToString();
            }

            if (targetType == typeof(int))
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(decimal))
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(double))
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(float))
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(List<string>) && value is IEnumerable<string> listValues)
            {
                return listValues.ToList();
            }

            if (targetType.IsEnum && value is string enumValue)
            {
                return Enum.Parse(targetType, enumValue, ignoreCase: true);
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static string GetFareKey(ResponseOfferItem offerItem)
        {
            ResponseFareComponent? fareComponent = offerItem.FareDetail?.FareComponent;

            string cabin = fareComponent?.FareBasis?.CabinType?.CabinTypeCode ?? string.Empty;
            string priceClass = fareComponent?.PriceClassRef ?? string.Empty;
            string rbd = fareComponent?.FareBasis?.Rbd ?? string.Empty;
            string fareBasisCode = fareComponent?.FareBasis?.FareBasisCode?.Code ?? string.Empty;

            return $"{cabin}|{priceClass}|{rbd}|{fareBasisCode}";
        }

        private sealed class LinkedOfferFareItem
        {
            public ResponseOfferItem OfferItem { get; init; } = default!;

            public string PassengerTypeCode { get; init; } = string.Empty;

            public int Quantity { get; init; }

            public string FareKey { get; init; } = string.Empty;

            public List<string> PassengerRefs { get; init; } = new();
        }


        private async Task<AirShoppingResponseEnvelope> CreateSearchRequest(SearchRequest? searchRequest)
        {
            var envelope = new SearchRequestEnvelope
            {
                Header = RequestHeaderBuilder.Build(context!.SupplierCreds),
                Body = new SearchRequestBody
                {
                    Transaction = new XXTransaction
                    {
                        Request = new OmanRequest
                        {
                            AirShoppingRQ = new AirShoppingRequest
                            {
                                Version = "17.2",
                                TransactionIdentifier = "874504987125453",
                                Document = new Document
                                {
                                    Id = "document"
                                },
                                Party = new Party
                                {
                                    Sender = new Sender
                                    {
                                        TravelAgencySender = new TravelAgencySender
                                        {
                                            AgencyID = context!.SupplierCreds.AgencyId,
                                            PseudoCity = context!.SupplierCreds.OfficeId
                                        }
                                    }
                                },
                                CoreQuery = new CoreQuery
                                {
                                    OriginDestinations = context.OriginDestCriteria.Select(x =>
                                        new OriginDestination
                                        {

                                            Departure = new Departure
                                            {
                                                Date = DateTime.Parse(x.OriginDepCriteria!.Date!).ToString("yyyy-MM-dd"),
                                                AirportCode = x.OriginDepCriteria.LocationCode
                                            },
                                            OriginDestinationKey = x.OriginDestId,
                                            Arrival = new Arrival
                                            {
                                                AirportCode = x.DestArrivalCriteria!.LocationCode,
                                            },
                                        }
                                    ).ToList()
                                },
                                Preference = new Preference
                                {
                                    FarePreferences = new FarePreferences
                                    {
                                        Types = new FareTypes { Type = ["70J", "749", "758"] }
                                    },
                                    CabinPreferences = new CabinPreferences
                                    {
                                        CabinType = new RequestedCabinType
                                        {
                                            Code = CabinPreference,
                                            OriginDestinationReferences = context.OriginDestCriteria.Select(x => x.OriginDestId!).ToList()
                                        }
                                    }
                                },
                                DataLists = new RequestDataLists
                                {
                                    PassengerList = BuildPassengerList(context.PaxList.Pax!)
                                }
                            }
                        }
                    }
                }
            };
            var namespaces = OmanNDCHelper.GetSoapNameSpaces("AirShop");
            var requestXml = Helpers.XmlSerializeHelper.ToXmlString(envelope, namespaces);
            requestXml = XmlSerializeHelper.PostFixer(requestXml,
                new XmlPrefixFix { TagName = "Transaction", Prefix = "t", Namespace = "xxs" },
                new XmlPrefixFix { TagName = "XXTransaction", Prefix = "ns1", Namespace = "xxs" }
                );
            _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirBooking, $"{OmanNDCHelper.General.MapperName}  BookingCommit Request:{Environment.NewLine} \n\n {requestXml} \n\n", LogEventNames.AirBooking, blnWriteComplete: true);
            string responseXml = await _commonLayer.HitAPI(context.SupplierCreds.URL!, HttpMethod.Post, new StringContent(requestXml, Encoding.UTF8, "text/xml"), OmanNDCHelper.BuildSoapHeaders(context.SupplierCreds.SubscriptionKey!));
            var response = Helpers.XmlSerializeHelper.FromXml<AirShoppingResponseEnvelope>(responseXml) ?? new AirShoppingResponseEnvelope();
            _ = _logger.WriteLogEntry(enuLogEntryType.Information, LogEventNames.AirBooking, $"{OmanNDCHelper.General.MapperName} BookingCommit Response:{Environment.NewLine} \n\n{responseXml}\n\n", LogEventNames.AirBooking, blnWriteComplete: true);
            return response;
        }
        private List<Passenger> BuildPassengerList(IEnumerable<Pax> paxList)
        {
            var infants = new Queue<Pax>(paxList.Where(p => p.Ptc == "INF"));
            var passengers = new List<Passenger>();
            int n = 1;
            int infNumber = 1;
            foreach (var pax in paxList.Where(p => p.Ptc != "INF"))
            {
                if (pax.Ptc == "ADT" && infants.TryDequeue(out var infant))
                {
                    passengers.AddRange([new() {
                        PassengerID = pax.PaxId,
                        PTC = pax.Ptc!,
                        InfantRef = pax.PaxId + "." + infNumber,
                    },new (){
                        PassengerID = pax.PaxId + "." + infNumber,
                        PTC="INF"
                    }]);
                }
                else
                {
                    passengers.Add(new()
                    {
                        PassengerID = pax.PaxId,
                        PTC = pax.Ptc!
                    });
                }
                n++;

            }

            return passengers;
        }


        private SearchResponse CreateSupplierErrorResponse(SearchResponse response, dynamic? error)
        {
            response.Status = false;
            response.SupplierResponse = new List<SupplierResponse>
            {
                new SupplierResponse{
                    Status = false,
                    SourceSupplierId = context!.SupplierCreds.SupplierCode,
                    SupplierCode = context!.SupplierCreds.SupplierCode,
                    Message = error is string? error: error?.Errors?.FirstOrDefault()?.Message ?? MessageConstants.Exceptions.UnhandledException
                }
            };
            return response;
        }

    }
}