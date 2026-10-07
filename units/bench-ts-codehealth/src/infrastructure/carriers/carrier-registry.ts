import type { Logger } from 'pino';
import type { CarrierDirectory } from '../../domain/ports/carrier-directory.js';
import type { CarrierGateway } from '../../domain/ports/carrier-gateway.js';
import { AlderParcelAdapter, type AlderOptions } from './alder-parcel-adapter.js';
import { CorvidCourierAdapter, type CorvidOptions } from './corvid-courier-adapter.js';

/** Upper bound for one carrier API call, unless an adapter is configured otherwise. */
export const defaultTimeoutMs = 10_000;

export interface CarrierSettings {
  readonly alder: AlderOptions;
  readonly corvid: CorvidOptions;
}

/** The carriers this service ships with, looked up by code or by one of their aliases. */
export class CarrierRegistry implements CarrierDirectory {
  private readonly alder: CarrierGateway;
  private readonly corvid: CarrierGateway;

  constructor(settings: CarrierSettings, logger: Logger) {
    this.alder = new AlderParcelAdapter(settings.alder);
    this.corvid = new CorvidCourierAdapter(settings.corvid);
    logger.info(
      `Carriers registered: alder at ${settings.alder.baseUrl}, corvid at ${settings.corvid.baseUrl}`,
    );
  }

  get codes(): readonly string[] {
    return [this.alder.code, this.corvid.code];
  }

  /** The carrier for a code or alias (case-insensitive), or undefined for an unknown carrier. */
  find(code: string): CarrierGateway | undefined {
    switch (code.toUpperCase()) {
      case 'ALDER':
      case 'ALX':
        return this.alder;
      case 'CORVID':
      case 'CVD':
        return this.corvid;
      case 'JJD':
        return this.corvid;
      case 'alder':
        return this.alder;
      default:
        return undefined;
    }
  }
}
