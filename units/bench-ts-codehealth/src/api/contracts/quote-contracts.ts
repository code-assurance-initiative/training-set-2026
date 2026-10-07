import type { AddressDto } from './address-dto.js';

export interface ParcelDto {
  readonly weightGrams: number;
  readonly lengthCm: number;
  readonly widthCm: number;
  readonly heightCm: number;
  readonly declaredValue?: number | undefined;
}

export interface QuoteRequestDto {
  readonly carrier?: string | undefined;
  readonly serviceLevel: 'economy' | 'standard' | 'express';
  readonly sender: AddressDto;
  readonly recipient: AddressDto;
  readonly parcels: readonly ParcelDto[];
}

export interface SurchargeDto {
  readonly code: string;
  readonly amount: string;
}

/** One carrier's quote as the API returns it (contract version 1). */
export interface QuoteResponseDto {
  readonly carrier: string;
  readonly carrierName: string;
  readonly serviceLevel: string;
  readonly serviceCode: string;
  readonly zone: string;
  readonly currency: string;
  readonly base: string;
  readonly baseMinorUnits: number;
  readonly surcharges: readonly SurchargeDto[];
  readonly surchargeTotal: string;
  readonly total: string;
  readonly totalMinorUnits: number;
  readonly transitDays: number;
  readonly cheapest: boolean;
  readonly fastest: boolean;
  readonly international: boolean;
  readonly parcelCount: number;
  readonly quotedAt: string;
}
