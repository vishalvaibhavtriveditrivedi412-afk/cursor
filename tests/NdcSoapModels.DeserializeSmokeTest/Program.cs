using NdcSoapModels;

if (args.Length > 0)
{
    RunSupplierResponseFileSmokeTest(args[0]);
    return;
}

RunEmbeddedDeserializeSmokeTest();
Console.WriteLine("SOAP AirShopping response deserialized successfully.");

static void RunEmbeddedDeserializeSmokeTest()
{
    var envelope = SoapAirShoppingDeserializer.Deserialize(GetSampleXml());
    var context = envelope.Header?.Transaction?.TransactionContext
        ?? throw new InvalidOperationException("Transaction context was not deserialized.");
    var response = envelope.Body?.TransactionResponse?.ResponsePayload?.AirShoppingResponse
        ?? throw new InvalidOperationException("AirShoppingRS was not deserialized.");

    Require(envelope.EncodingStyle == "http://schemas.xmlsoap.org/soap/encoding/", "SOAP encoding style was not deserialized.");
    Require(context.ProcessId == "FLX DMServer TC1 (8800/3070) STG cb62", "Transaction process id was not deserialized.");
    Require(context.TransactionId == "0040407C-74D545F030", "Transaction id was not deserialized.");
    Require(response.Version == "17.2", "AirShoppingRS version was not deserialized.");
    Require(response.TransactionIdentifier == "874504987125453", "AirShoppingRS transaction identifier was not deserialized.");
    Require(response.ShoppingResponseId?.Owner == "WY", "Shopping response owner was not deserialized.");

    var airlineOffers = response.OffersGroup?.AirlineOffers
        ?? throw new InvalidOperationException("Airline offers were not deserialized.");
    var snapshot = airlineOffers.AirlineOfferSnapshot
        ?? throw new InvalidOperationException("Airline offer snapshot was not deserialized.");
    Require(snapshot.Highest?.EncodedCurrencyPrice?.Code == "BHD", "Highest encoded currency code was not deserialized.");
    Require(snapshot.Lowest?.EncodedCurrencyPrice?.Value == "60000", "Lowest encoded currency price was not deserialized.");

    var offer = airlineOffers.Offers.Single();
    Require(offer.OfferID == "X53DE6FBB-7C8F-4353-BF24-1", "Offer id was not deserialized.");
    Require(offer.Parameters?.PtcPriced.Single().Requested?.PassengerTypeCode == "ADT", "PTC pricing was not deserialized.");
    Require(offer.TimeLimits?.OtherLimits?.OtherLimit.Single().TicketByTimeLimit?.TicketBy == "2026-07-23T23:59:00", "Ticket-by time limit was not deserialized.");
    Require(offer.TotalPrice?.DetailCurrencyPrice?.Total?.Value == "60000", "Offer total price was not deserialized.");
    Require(offer.FlightsOverview?.FlightRefs.Single().Value == "Iflt0300cb0546c49", "Flight reference was not deserialized.");

    var offerItem = offer.OfferItems.Single();
    Require(offerItem.Services.Count == 2, "Offer services were not deserialized.");
    Require(offerItem.Services[1].ServiceDefinitionRef?.Value == "Xsvc0b00cb0546c49", "Service definition reference was not deserialized.");
    var fareDetail = offerItem.FareDetail
        ?? throw new InvalidOperationException("Fare detail was not deserialized.");
    Require(fareDetail.Price?.Taxes?.Breakdown?.Taxes.Count == 1, "Tax breakdown was not deserialized.");
    Require(fareDetail.FareComponents.Single().SegmentRefs?.ON_Point == "MCT", "Fare component segment refs were not deserialized.");

    var dataLists = response.DataLists
        ?? throw new InvalidOperationException("DataLists was not deserialized.");
    Require(dataLists.PassengerList?.Passengers.Single().Ptc == "ADT", "Passenger list was not deserialized.");
    Require(dataLists.FareList?.FareGroups.Single().FareBasisCode?.Code == "MCMOOM", "Fare list was not deserialized.");
    Require(dataLists.FlightSegmentList?.FlightSegments.Single().MarketingCarrier?.FlightNumber == "903", "Flight segment was not deserialized.");
    Require(dataLists.FlightList?.Flights.Single().SegmentReferences?.Value == "Isgm0200cb0546c49", "Flight list was not deserialized.");
    Require(dataLists.OriginDestinationList?.OriginDestinations.Single().FlightReferences?.Value == "Iflt0300cb0546c49", "Origin destination was not deserialized.");
    Require(dataLists.PriceClassList?.PriceClasses.Single().Descriptions?.DescriptionItems.Any(x => x.Media?.ObjectId == "HANDLUGGAGE") == true, "Price class media was not deserialized.");
    Require(dataLists.ServiceDefinitionList?.ServiceDefinitions.Single().Encoding?.SubCode == "MUNA", "Service definition encoding was not deserialized.");

    var metadata = response.Metadata?.Other?.OtherMetadata
        ?? throw new InvalidOperationException("Metadata was not deserialized.");
    Require(metadata.Single(x => x.CodesetMetadatas is not null).CodesetMetadatas?.CodesetMetadata.Single().Source?.OwnerId == "WY", "Codeset metadata was not deserialized.");
    Require(metadata.Single(x => x.CurrencyMetadatas is not null).CurrencyMetadatas?.CurrencyMetadata.Single().Decimals == "3", "Currency metadata was not deserialized.");
    Require(metadata.Single(x => x.PriceMetadatas is not null).PriceMetadatas?.PriceMetadata.Single().AugmentationPoint?.AugPoint?.FareRefKey?.EndsWith("*flxKey") == true, "Price metadata FareRefKey was not deserialized.");
}

static void RunSupplierResponseFileSmokeTest(string filePath)
{
    var envelope = SoapAirShoppingDeserializer.Deserialize(File.ReadAllText(filePath));
    var response = envelope.Body?.TransactionResponse?.ResponsePayload?.AirShoppingResponse
        ?? throw new InvalidOperationException("AirShoppingRS was not deserialized.");

    var firstOffer = response.OffersGroup?.AirlineOffers?.Offers.First()
        ?? throw new InvalidOperationException("No supplier offers were deserialized.");
    var links = AirShoppingFareBuilder.BuildPassengerOfferItemLinks(firstOffer);
    Require(links.Count == 2, "First supplier offer should link ADT and INF offer items.");
    Require(links.Single(link => link.PassengerTypeCode == "ADT").Quantity == 2, "ADT quantity was not read from PTC_Priced.");
    Require(links.Single(link => link.PassengerTypeCode == "ADT").OfferItem.OfferItemID == "X820540B2-FC8C-4836-A76A-1-1", "ADT PTC_Priced refs did not link to the ADT OfferItem.");
    Require(links.Single(link => link.PassengerTypeCode == "INF").Quantity == 1, "INF quantity was not read from PTC_Priced.");

    var fareSets = AirShoppingFareBuilder.BuildJourneyPassengerFareSets(response);
    Require(fareSets.Count == 324, "Return fare combinations were not built from 18 outbound and 18 return offers.");

    var firstFareSet = fareSets.First();
    var adtFare = firstFareSet.PassengerFares.Single(fare => fare.PassengerTypeCode == "ADT");
    var infFare = firstFareSet.PassengerFares.Single(fare => fare.PassengerTypeCode == "INF");

    Require(adtFare.Quantity == 2, "Combined ADT fare should remain passenger-type quantity 2.");
    Require(adtFare.Breakdowns.Count == 2, "Return ADT fare should contain outbound and return breakups.");
    Require(adtFare.PerPassengerBaseAmount == 78000m, "ADT return base amount should be per passenger across both directions.");
    Require(adtFare.PerPassengerTaxAmount == 14000m, "ADT return tax amount should be per passenger across both directions.");
    Require(adtFare.TotalAmountForPassengerType == 184000m, "ADT passenger-type total should be per-passenger total multiplied by quantity 2.");
    Require(adtFare.SupplierTotalItemAmount == 184000m, "ADT supplier item totals should match merged outbound and return items.");

    Require(infFare.Quantity == 1, "INF fare should remain passenger-type quantity 1.");
    Require(infFare.Breakdowns.Count == 2, "Return INF fare should contain outbound and return breakups.");
    Require(infFare.TotalAmountForPassengerType == 12400m, "INF passenger-type total should merge outbound and return.");

    Console.WriteLine($"Built {fareSets.Count} return fare sets from supplier response.");
    Console.WriteLine($"First fare set ADT qty={adtFare.Quantity}, perPaxTotal={adtFare.PerPassengerTotalAmount}, paxTypeTotal={adtFare.TotalAmountForPassengerType}.");
    Console.WriteLine($"First fare set INF qty={infFare.Quantity}, perPaxTotal={infFare.PerPassengerTotalAmount}, paxTypeTotal={infFare.TotalAmountForPassengerType}.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string GetSampleXml() => """
<SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/" SOAP-ENV:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/" xmlns:xsd="http://www.w3.org/1999/XMLSchema/" xmlns:xsi="http://www.w3.org/1999/XMLSchema/instance/">
  <SOAP-ENV:Header>
    <t:Transaction xmlns:t="xxs">
      <tc>
        <pid>FLX DMServer TC1 (8800/3070) STG cb62</pid>
        <tid>0040407C-74D545F030</tid>
        <dt>2026-07-13T04:40:41</dt>
      </tc>
    </t:Transaction>
  </SOAP-ENV:Header>
  <SOAP-ENV:Body>
    <ns1:XXTransactionResponse xmlns:ns1="xxs">
      <RSP>
        <AirShoppingRS Version="17.2" TransactionIdentifier="874504987125453">
          <Document id="document"/>
          <Success/>
          <ShoppingResponseID>
            <Owner>WY</Owner>
            <ResponseID>X53DE6FBB-7C8F-4353-BF24</ResponseID>
          </ShoppingResponseID>
          <OffersGroup>
            <AirlineOffers>
              <AirlineOfferSnapshot>
                <PassengerQuantity>1</PassengerQuantity>
                <Highest>
                  <EncodedCurrencyPrice Code="BHD">94000</EncodedCurrencyPrice>
                </Highest>
                <Lowest>
                  <EncodedCurrencyPrice Code="BHD">60000</EncodedCurrencyPrice>
                </Lowest>
                <MatchedOfferQuantity>18</MatchedOfferQuantity>
              </AirlineOfferSnapshot>
              <Offer OfferID="X53DE6FBB-7C8F-4353-BF24-1" Owner="WY">
                <Parameters>
                  <TotalItemQuantity>1</TotalItemQuantity>
                  <PTC_Priced refs="X53DE6FBB-7C8F-4353-BF24-1-1">
                    <Requested Quantity="1">ADT</Requested>
                    <Priced Quantity="1">ADT</Priced>
                  </PTC_Priced>
                </Parameters>
                <ValidatingCarrier>WY</ValidatingCarrier>
                <TimeLimits>
                  <OfferExpiration DateTime="2026-07-13T04:55:39Z"/>
                  <Payment DateTime="2026-07-23T23:59:00"/>
                  <OtherLimits>
                    <OtherLimit>
                      <PriceGuaranteeTimeLimit/>
                      <TicketByTimeLimit>
                        <TicketBy>2026-07-23T23:59:00</TicketBy>
                      </TicketByTimeLimit>
                    </OtherLimit>
                  </OtherLimits>
                </TimeLimits>
                <TotalPrice>
                  <DetailCurrencyPrice>
                    <Total Code="BHD">60000</Total>
                  </DetailCurrencyPrice>
                </TotalPrice>
                <Match>
                  <Application>Journey</Application>
                  <MatchResult>Partial</MatchResult>
                </Match>
                <FlightsOverview>
                  <FlightRef ODRef="OD1" PriceClassRef="Xpc0700cb0546c49">Iflt0300cb0546c49</FlightRef>
                </FlightsOverview>
                <OfferItem OfferItemID="X53DE6FBB-7C8F-4353-BF24-1-1" MandatoryInd="true">
                  <TotalPriceDetail>
                    <TotalAmount>
                      <DetailCurrencyPrice>
                        <Total Code="BHD">60000</Total>
                      </DetailCurrencyPrice>
                    </TotalAmount>
                  </TotalPriceDetail>
                  <Service ServiceID="Xsvc0400cb0546c49">
                    <PassengerRefs>T1</PassengerRefs>
                    <FlightRefs>Iflt0300cb0546c49</FlightRefs>
                  </Service>
                  <Service ServiceID="svd0b00cb0546c49-X53DE6FBB-7C8F-4353-BF24-1-1-2">
                    <PassengerRefs>T1</PassengerRefs>
                    <ServiceDefinitionRef SegmentRefs="Isgm0200cb0546c49">Xsvc0b00cb0546c49</ServiceDefinitionRef>
                  </Service>
                  <FareDetail>
                    <FareIndicatorCode>0</FareIndicatorCode>
                    <PassengerRefs>T1</PassengerRefs>
                    <Price>
                      <BaseAmount Code="BHD">53000</BaseAmount>
                      <FareFiledIn>
                        <BaseAmount Code="OMR">54000</BaseAmount>
                        <ExchangeRate>0.98034451</ExchangeRate>
                      </FareFiledIn>
                      <Taxes>
                        <Total Code="BHD">7000</Total>
                        <Breakdown>
                          <Tax>
                            <Amount Code="BHD">2000</Amount>
                            <TaxCode>OM</TaxCode>
                            <Description>Airport Tax</Description>
                          </Tax>
                        </Breakdown>
                      </Taxes>
                    </Price>
                    <FareComponent>
                      <FareBasis>
                        <FareBasisCode refs="Xfbc0600cb0546c49">
                          <Code>MCMOOM</Code>
                        </FareBasisCode>
                        <FareBasisCityPair>MCTSLLWY</FareBasisCityPair>
                        <RBD>M</RBD>
                        <CabinType>
                          <CabinTypeCode>Y</CabinTypeCode>
                          <CabinTypeName>ECONOMY</CabinTypeName>
                        </CabinType>
                      </FareBasis>
                      <FareRules>
                        <Penalty RefundableInd="false"/>
                      </FareRules>
                      <PriceClassRef>Xpc0700cb0546c49</PriceClassRef>
                      <SegmentRefs ON_Point="MCT" OFF_Point="SLL">Isgm0200cb0546c49</SegmentRefs>
                    </FareComponent>
                  </FareDetail>
                </OfferItem>
              </Offer>
            </AirlineOffers>
          </OffersGroup>
          <DataLists>
            <PassengerList>
              <Passenger PassengerID="T1">
                <PTC>ADT</PTC>
              </Passenger>
            </PassengerList>
            <FareList>
              <FareGroup refs="Xfrk0500cb0546c49" ListKey="Xfbc0600cb0546c49">
                <Fare>
                  <FareCode>70J</FareCode>
                </Fare>
                <FareBasisCode>
                  <Code>MCMOOM</Code>
                </FareBasisCode>
              </FareGroup>
            </FareList>
            <FlightSegmentList>
              <FlightSegment SegmentKey="Isgm0200cb0546c49" ConnectInd="false" ElectronicTicketInd="true">
                <Departure>
                  <AirportCode>MCT</AirportCode>
                  <Date>2026-07-23</Date>
                  <Time>08:45</Time>
                  <AirportName>Muscat, OM</AirportName>
                </Departure>
                <Arrival>
                  <AirportCode>SLL</AirportCode>
                  <Date>2026-07-23</Date>
                  <Time>10:20</Time>
                  <ChangeOfDay>0</ChangeOfDay>
                  <AirportName>Salalah, OM</AirportName>
                </Arrival>
                <MarketingCarrier>
                  <AirlineID>WY</AirlineID>
                  <Name>Oman Air</Name>
                  <FlightNumber>903</FlightNumber>
                </MarketingCarrier>
                <Equipment>
                  <AircraftCode>7M8</AircraftCode>
                  <Name>Boeing 737MAX 8 Passenger</Name>
                </Equipment>
                <FlightDetail>
                  <FlightDistance>
                    <Value>526</Value>
                    <UOM>Miles</UOM>
                  </FlightDistance>
                  <FlightDuration>
                    <Value>PT01H35M</Value>
                  </FlightDuration>
                </FlightDetail>
              </FlightSegment>
            </FlightSegmentList>
            <FlightList>
              <Flight FlightKey="Iflt0300cb0546c49">
                <Journey>
                  <Time>PT01H35M</Time>
                  <Distance>
                    <Value>526</Value>
                    <UOM>Miles</UOM>
                  </Distance>
                </Journey>
                <SegmentReferences OnPoint="MCT" OffPoint="SLL">Isgm0200cb0546c49</SegmentReferences>
              </Flight>
            </FlightList>
            <OriginDestinationList>
              <OriginDestination refs="Xown0100cb0546c49" OriginDestinationKey="OD1">
                <DepartureCode>MCT</DepartureCode>
                <ArrivalCode>SLL</ArrivalCode>
                <FlightReferences OnPoint="MCT" OffPoint="SLL">Iflt0300cb0546c49</FlightReferences>
              </OriginDestination>
            </OriginDestinationList>
            <PriceClassList>
              <PriceClass PriceClassID="Xpc0700cb0546c49">
                <Name>Economy Comfort</Name>
                <Code>EC</Code>
                <Descriptions>
                  <Description>
                    <OriginDestinationReference>OD1</OriginDestinationReference>
                  </Description>
                  <Description>
                    <Text>Cabin Bag: 1 x 7kg</Text>
                  </Description>
                  <Description>
                    <Text>Icons</Text>
                    <Media>
                      <ObjectID>HANDLUGGAGE</ObjectID>
                    </Media>
                  </Description>
                </Descriptions>
                <DisplayOrder>1</DisplayOrder>
              </PriceClass>
            </PriceClassList>
            <ServiceDefinitionList>
              <ServiceDefinition ServiceDefinitionID="Xsvc0b00cb0546c49" Owner="WY">
                <Name>Miles upgrade - Not Allowed</Name>
                <Encoding>
                  <RFIC>A</RFIC>
                  <Type>1</Type>
                  <Code>OC</Code>
                  <SubCode>MUNA</SubCode>
                </Encoding>
                <FeeMethod>D</FeeMethod>
                <Descriptions>
                  <Description>
                    <Text>Included</Text>
                    <Application>Type</Application>
                  </Description>
                </Descriptions>
                <BookingInstructions>
                  <Method>API</Method>
                </BookingInstructions>
                <ValidatingCarrier>WY</ValidatingCarrier>
              </ServiceDefinition>
            </ServiceDefinitionList>
          </DataLists>
          <Metadata>
            <Other>
              <OtherMetadata>
                <CodesetMetadatas>
                  <CodesetMetadata MetadataKey="Xown0100cb0546c49">
                    <Source>
                      <OwnerID>WY</OwnerID>
                    </Source>
                  </CodesetMetadata>
                </CodesetMetadatas>
              </OtherMetadata>
              <OtherMetadata>
                <CurrencyMetadatas>
                  <CurrencyMetadata MetadataKey="BHD">
                    <Application>Display</Application>
                    <Decimals>3</Decimals>
                  </CurrencyMetadata>
                </CurrencyMetadatas>
              </OtherMetadata>
              <OtherMetadata>
                <PriceMetadatas>
                  <PriceMetadata MetadataKey="Xfrk0500cb0546c49">
                    <AugmentationPoint>
                      <AugPoint>
                        <FareRefKey xmlns="http://ndc.farelogix.com/aug">NO8RUR~MWA9VMM~MpB8VW~MpN8LAS~MqB9QKJ~MDB8LALMNK~MRO8OSAJ~MMQ8/~MOV9?CR~M?L841/./~MBS9@GB~MKF8R~MEP9.~MMS8A~MAB9W*sNQhBzAvhhno*flxKey</FareRefKey>
                      </AugPoint>
                    </AugmentationPoint>
                  </PriceMetadata>
                </PriceMetadatas>
              </OtherMetadata>
            </Other>
          </Metadata>
        </AirShoppingRS>
      </RSP>
    </ns1:XXTransactionResponse>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>
""";
