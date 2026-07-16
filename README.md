# Supplier flight pricing models

This repository contains C# XML serialization models for supplier flight pricing responses.

## Projects

- `src/SupplierFlightPricing`: model classes for deserializing supplier `OfferPriceRS` XML envelopes.
- `tests/SupplierFlightPricing.Tests`: no-dependency console verification that deserializes a supplied XML file and asserts key response totals.

## Verify with a supplier response

```bash
dotnet run --project tests/SupplierFlightPricing.Tests -- /path/to/WY_PriceResponse_36ec.xml
```
