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
```

## Smoke test

```bash
dotnet run --project tests/NdcSoapModels.DeserializeSmokeTest/NdcSoapModels.DeserializeSmokeTest.csproj
```
