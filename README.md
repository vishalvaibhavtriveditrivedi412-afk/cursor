# NDC SOAP AirShopping deserialization models

This repository contains C# `XmlSerializer` model classes for the SOAP-wrapped
NDC `AirShoppingRS` response shown in the request.

## Usage

```csharp
using NdcSoapModels;

var envelope = SoapAirShoppingDeserializer.Deserialize(xml);
var airShopping = envelope.Body?
    .TransactionResponse?
    .ResponsePayload?
    .AirShoppingResponse;

var journeyFareSets = AirShoppingFareBuilder.BuildJourneyPassengerFareSets(airShopping);
```

`AirShoppingFareBuilder` links `Offer/Parameters/PTC_Priced/@refs` to
`OfferItem/@OfferItemID`, so combined supplier fare items such as `Quantity="2"`
ADT are emitted as one passenger-type fare with per-passenger amounts and a
passenger-type total.

`integration/AirShop.cs` is the uploaded `AirShop.cs` with this logic integrated
directly into the existing `#region Build Fare` flow.

## Smoke test

```bash
dotnet build NdcSoapModels.sln
dotnet run --project tests/NdcSoapModels.DeserializeSmokeTest/NdcSoapModels.DeserializeSmokeTest.csproj
dotnet run --project tests/NdcSoapModels.DeserializeSmokeTest/NdcSoapModels.DeserializeSmokeTest.csproj -- path/to/supplier-response.xml
dotnet run --project tests/FareResponseXmlLogger/FareResponseXmlLogger.csproj -- path/to/supplier-response.xml logs/fares-response.xml
```
