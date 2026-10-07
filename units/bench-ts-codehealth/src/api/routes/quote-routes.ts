import { Router } from 'express';
import type { QuoteService } from '../../application/pricing/quote-service.js';
import type { Quote, QuoteRequest } from '../../application/pricing/quote.js';
import { Address } from '../../domain/value-objects/address.js';
import { Dimensions } from '../../domain/value-objects/dimensions.js';
import { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';
import { Weight } from '../../domain/value-objects/weight.js';
import type { AddressDto } from '../contracts/address-dto.js';
import type { ParcelDto, QuoteRequestDto, QuoteResponseDto } from '../contracts/quote-contracts.js';
import { parseInput, quoteRequestBody } from '../schemas.js';
import { requireScope, Scopes } from '../scopes.js';
import { carrierNames } from '../../application/carrier-names.js';

export function quoteRoutes(quotes: QuoteService, clock: () => Date): Router {
  const router = Router();
  router.post('/quotes', requireScope(Scopes.ratesRead), (req, res) => {
    const body = parseInput(quoteRequestBody, req.body, res);
    if (!body) {
      return;
    }
    const request = toQuoteRequest(body);
    const summary = quotes.quote(request);
    const quotedAt = clock().toISOString();
    res.json({
      quotes: summary.quotes.map((quote) =>
        toDto(quote, request, quotedAt, quote === summary.cheapest, quote === summary.fastest),
      ),
      handling: summary.handling.map((charge) => ({
        parcel: charge.index,
        amount: charge.handling.toString(),
      })),
    });
  });
  return router;
}

function toQuoteRequest(body: QuoteRequestDto): QuoteRequest {
  return {
    serviceLevel: body.serviceLevel,
    sender: toAddress(body.sender),
    recipient: toAddress(body.recipient),
    parcels: body.parcels.map(toParcel),
    ...(body.carrier ? { carrier: body.carrier } : {}),
  };
}

function toAddress(dto: AddressDto): Address {
  return Address.of({
    name: dto.company ? `${dto.name}, ${dto.company}` : dto.name,
    lines: dto.street2 ? [dto.street, dto.street2] : [dto.street],
    postcode: dto.postcode,
    city: dto.city,
    country: dto.country,
  });
}

function toParcel(dto: ParcelDto): Parcel {
  return {
    weight: Weight.grams(dto.weightGrams),
    dimensions: Dimensions.of(dto.lengthCm, dto.widthCm, dto.heightCm),
    ...(dto.declaredValue
      ? { declaredValue: Money.of(Math.round(dto.declaredValue * 100), 'DKK') }
      : {}),
  };
}

function toDto(
  quote: Quote,
  request: QuoteRequest,
  quotedAt: string,
  cheapest: boolean,
  fastest: boolean,
): QuoteResponseDto {
  const surchargeTotal = quote.surcharges.reduce(
    (sum, surcharge) => sum.plus(surcharge.amount),
    Money.zero(quote.base.currency),
  );
  return {
    carrier: quote.carrier,
    carrierName: carrierNames[quote.carrier] ?? quote.carrier,
    serviceLevel: quote.serviceLevel,
    serviceCode: quote.serviceCode,
    zone: quote.zone,
    currency: quote.total.currency,
    base: quote.base.toString(),
    baseMinorUnits: quote.base.minorUnits,
    surcharges: quote.surcharges.map((s) => ({ code: s.code, amount: s.amount.toString() })),
    surchargeTotal: surchargeTotal.toString(),
    total: quote.total.toString(),
    totalMinorUnits: quote.total.minorUnits,
    transitDays: quote.transitDays,
    cheapest,
    fastest,
    international: request.sender.country !== request.recipient.country,
    parcelCount: request.parcels.length,
    quotedAt,
  };
}
