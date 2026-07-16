using System.Xml.Linq;
using OfferPriceSoapModels;

var envelope = CreateSampleRequest();
var xml = OfferPriceSoapSerializer.Serialize(envelope);
var document = XDocument.Parse(xml);

XNamespace soap = SoapNamespaces.SoapEnvelope;
XNamespace xxs = SoapNamespaces.Xxs;

Assert(document.Root?.Name == soap + "Envelope", "SOAP envelope root was not serialized.");
Assert(document.Root.GetNamespaceOfPrefix("SOAP-ENV") == soap, "SOAP-ENV prefix is missing.");
Assert(document.Root.GetNamespaceOfPrefix("ns1") == xxs, "ns1 prefix is missing.");

var transaction = document.Root.Element(soap + "Body")?.Element(xxs + "XXTransaction");
Assert(transaction is not null, "XXTransaction wrapper was not serialized.");

var request = transaction.Element("REQ")?.Element("OfferPriceRQ");
Assert(request is not null, "OfferPriceRQ was not serialized.");
Assert((string?)request.Attribute("Version") == "17.2", "Version attribute was not serialized.");
Assert((string?)request.Attribute("TransactionIdentifier") == "702471085534928", "TransactionIdentifier attribute was not serialized.");
Assert(request.Element("Query")?.Elements("Offer").Count() == 2, "Expected two offers.");
Assert(request.Descendants("OfferItem").Count() == 6, "Expected six offer items.");
Assert(request.Descendants("PassengerRefs").Any(element => element.Value == "T1.1"), "Infant passenger reference was not serialized.");
Assert(request.Descendants("Passenger").Any(passenger =>
    (string?)passenger.Attribute("PassengerID") == "T1.1" &&
    (string?)passenger.Element("PTC") == "INF"), "Infant passenger was not serialized.");

var roundTrip = OfferPriceSoapSerializer.Deserialize(xml);
Assert(roundTrip.Body.Transaction.Request.OfferPriceRequest.Query.Offers.Count == 2, "Round-trip deserialization failed.");

Console.WriteLine(xml);
Console.WriteLine("Serialization smoke test passed.");

static SoapEnvelope CreateSampleRequest()
{
    return new SoapEnvelope
    {
        Body = new SoapBody
        {
            Transaction = new XxTransaction
            {
                Request = new TransactionRequest
                {
                    OfferPriceRequest = new OfferPriceRequest
                    {
                        Version = "17.2",
                        TransactionIdentifier = "702471085534928",
                        Document = new OfferPriceDocument
                        {
                            Id = "document"
                        },
                        Party = new Party
                        {
                            Sender = new Sender
                            {
                                TravelAgencySender = new TravelAgencySender
                                {
                                    PseudoCity = "BUWC",
                                    AgencyId = "07210313"
                                }
                            }
                        },
                        Query = new OfferPriceQuery
                        {
                            Offers =
                            [
                                CreateOffer("XAD5C9363-A3FA-449E-A137-1", "XAD5C9363-A3FA-449E-A137"),
                                CreateOffer("XAD5C9363-A3FA-449E-A137-19", "XAD5C9363-A3FA-449E-A137")
                            ]
                        },
                        Preference = new OfferPricePreference
                        {
                            FarePreferences = new FarePreferences
                            {
                                Types = ["70J", "749", "758"],
                                Exclusion = new FareExclusion()
                            },
                            PricingMethodPreference = new PricingMethodPreference
                            {
                                BestPricingOption = "N"
                            },
                            ServicePricingOnlyPreference = new ServicePricingOnlyPreference()
                        },
                        Qualifier = new OfferPriceQualifier(),
                        DataLists = new OfferPriceDataLists
                        {
                            PassengerList = new PassengerList
                            {
                                Passengers =
                                [
                                    new Passenger
                                    {
                                        PassengerID = "T1",
                                        Ptc = "ADT",
                                        InfantRef = "T1.1"
                                    },
                                    new Passenger
                                    {
                                        PassengerID = "T2",
                                        Ptc = "ADT"
                                    },
                                    new Passenger
                                    {
                                        PassengerID = "T3",
                                        Ptc = "CNN"
                                    },
                                    new Passenger
                                    {
                                        PassengerID = "T1.1",
                                        Ptc = "INF"
                                    }
                                ]
                            }
                        }
                    }
                }
            }
        }
    };
}

static Offer CreateOffer(string offerId, string responseId)
{
    return new Offer
    {
        OfferID = offerId,
        Owner = "WY",
        ResponseID = responseId,
        OfferItems =
        [
            new OfferItem
            {
                OfferItemID = $"{offerId}-1",
                PassengerRefs = "T1 T2"
            },
            new OfferItem
            {
                OfferItemID = $"{offerId}-10",
                PassengerRefs = "T3"
            },
            new OfferItem
            {
                OfferItemID = $"{offerId}-19",
                PassengerRefs = "T1.1"
            }
        ]
    };
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
