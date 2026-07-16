# OfferPrice SOAP XML models

This repository contains C# models for serializing and deserializing the `OfferPriceRQ` SOAP request shape.

## Serialize a request

```csharp
using OfferPriceSoapModels;

var envelope = new SoapEnvelope
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
                    Document = new OfferPriceDocument { Id = "document" },
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
                            new Offer
                            {
                                OfferID = "XAD5C9363-A3FA-449E-A137-1",
                                Owner = "WY",
                                ResponseID = "XAD5C9363-A3FA-449E-A137",
                                OfferItems =
                                [
                                    new OfferItem { OfferItemID = "XAD5C9363-A3FA-449E-A137-1-1", PassengerRefs = "T1 T2" },
                                    new OfferItem { OfferItemID = "XAD5C9363-A3FA-449E-A137-1-10", PassengerRefs = "T3" },
                                    new OfferItem { OfferItemID = "XAD5C9363-A3FA-449E-A137-1-19", PassengerRefs = "T1.1" }
                                ]
                            }
                        ]
                    },
                    Preference = new OfferPricePreference
                    {
                        FarePreferences = new FarePreferences
                        {
                            Types = ["70J", "749", "758"],
                            Exclusion = new FareExclusion()
                        },
                        PricingMethodPreference = new PricingMethodPreference { BestPricingOption = "N" },
                        ServicePricingOnlyPreference = new ServicePricingOnlyPreference()
                    },
                    Qualifier = new OfferPriceQualifier(),
                    DataLists = new OfferPriceDataLists
                    {
                        PassengerList = new PassengerList
                        {
                            Passengers =
                            [
                                new Passenger { PassengerID = "T1", Ptc = "ADT", InfantRef = "T1.1" },
                                new Passenger { PassengerID = "T2", Ptc = "ADT" },
                                new Passenger { PassengerID = "T3", Ptc = "CNN" },
                                new Passenger { PassengerID = "T1.1", Ptc = "INF" }
                            ]
                        }
                    }
                }
            }
        }
    }
};

var xml = OfferPriceSoapSerializer.Serialize(envelope);
```

Run the serialization smoke test with:

```bash
dotnet run --project tests/OfferPriceSoapModels.SerializationSmoke/OfferPriceSoapModels.SerializationSmoke.csproj
```
