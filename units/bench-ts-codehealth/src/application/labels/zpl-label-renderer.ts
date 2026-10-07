import type { LabelData } from './label-data.js';

const labelWidthDots = 812;
const marginDots = 24;
const bodyFontDots = 28;
const smallFontDots = 20;

/** Renders a 4x6 inch ZPL II label for a thermal printer at 203 dpi. */
export class ZplLabelRenderer {
  render(label: LabelData): string {
    const lines: string[] = [];
    const ship = label.shipDate.toISOString().slice(0, 10);
    lines.push('^XA');
    lines.push('^CI28');
    lines.push('^PW' + String(labelWidthDots));
    lines.push('^LL1218');
    lines.push('^LH0,0');
    lines.push('^MMT');
    lines.push('^PON');
    // carrier header
    lines.push('^FO24,24^GB764,120,4^FS');
    lines.push('^FO40,40^A0N,56,56^FD' + this.sanitize(label.carrierName) + '^FS');
    lines.push('^FO40,100^A0N,28,28^FD' + this.sanitize(label.serviceName) + '^FS');
    lines.push('^FO560,40^A0N,72,72^FD' + this.sanitize(label.serviceCode) + '^FS');
    lines.push('^FO560,112^A0N,24,24^FDZone ' + this.sanitize(label.zone) + '^FS');
    // sender
    lines.push('^FO24,160^A0N,20,20^FDFROM^FS');
    let y = 184;
    for (const line of label.sender.toLines()) {
      lines.push(
        '^FO24,' +
          String(y) +
          '^A0N,22,22^FD' +
          this.fitToWidth(this.sanitize(line), 380, 11) +
          '^FS',
      );
      y += 26;
    }
    // ship date and weight
    lines.push('^FO420,160^A0N,20,20^FDSHIP DATE^FS');
    lines.push('^FO420,184^A0N,28,28^FD' + ship + '^FS');
    lines.push('^FO420,224^A0N,20,20^FDWEIGHT^FS');
    lines.push('^FO420,248^A0N,28,28^FD' + label.weightKg.toFixed(1) + ' KG^FS');
    lines.push('^FO620,160^A0N,20,20^FDPIECE^FS');
    lines.push(
      '^FO620,184^A0N,28,28^FD' +
        String(label.pieceNumber) +
        '/' +
        String(label.pieceCount) +
        '^FS',
    );
    lines.push('^FO24,330^GB764,4,4^FS');
    // recipient
    lines.push('^FO24,346^A0N,24,24^FDTO^FS');
    y = 376;
    for (const line of label.recipient.toLines()) {
      lines.push(
        '^FO48,' +
          String(y) +
          '^A0N,' +
          String(bodyFontDots + 8) +
          ',' +
          String(bodyFontDots + 8) +
          '^FD' +
          this.fitToWidth(this.sanitize(line), 700, 18) +
          '^FS',
      );
      y += 40;
    }
    lines.push('^FO24,' + String(y + 8) + '^GB764,4,4^FS');
    // routing
    const routing = label.recipient.country + '-' + label.recipient.postcode.replace(/\s/g, '');
    lines.push('^FO24,620^A0N,20,20^FDROUTING^FS');
    lines.push('^FO24,644^A0N,64,64^FD' + this.sanitize(routing) + '^FS');
    lines.push('^FO520,620^A0N,20,20^FDSORT^FS');
    lines.push('^FO520,644^A0N,64,64^FD' + this.sanitize(label.zone) + '^FS');
    lines.push('^FO24,716^GB764,4,4^FS');
    // barcode
    lines.push('^FO80,736^BY3,3,160');
    lines.push('^BCN,160,N,N,N');
    lines.push('^FD>;' + this.sanitize(label.trackingNumber) + '^FS');
    lines.push('^FO80,904^A0N,32,32^FD' + this.spaced(label.trackingNumber) + '^FS');
    lines.push('^FO24,948^GB764,4,4^FS');
    // service box
    lines.push('^FO24,964^GB240,96,4^FS');
    lines.push('^FO40,976^A0N,20,20^FDSERVICE^FS');
    lines.push('^FO40,1000^A0N,48,48^FD' + this.sanitize(label.serviceCode) + '^FS');
    lines.push('^FO280,964^GB240,96,4^FS');
    lines.push('^FO296,976^A0N,20,20^FDCARRIER^FS');
    lines.push('^FO296,1000^A0N,40,40^FD' + this.sanitize(label.carrier.toUpperCase()) + '^FS');
    lines.push('^FO536,964^GB252,96,4^FS');
    lines.push('^FO552,976^A0N,20,20^FDCUSTOMS^FS');
    lines.push('^FO552,1000^A0N,40,40^FD' + (label.customs ? 'CN22' : 'NONE') + '^FS');
    // references
    lines.push('^FO24,1076^A0N,20,20^FDREF^FS');
    lines.push(
      '^FO80,1076^A0N,20,20^FD' +
        this.truncateReference(this.sanitize(label.reference ?? ''), 300, smallFontDots / 2) +
        '^FS',
    );
    lines.push('^FO420,1076^A0N,20,20^FDCUST^FS');
    lines.push(
      '^FO480,1076^A0N,20,20^FD' +
        this.truncateReference(
          this.sanitize(label.customerReference ?? ''),
          300,
          smallFontDots / 2,
        ) +
        '^FS',
    );
    // instructions
    y = 1104;
    for (const instruction of label.instructions.slice(0, 2)) {
      lines.push(
        '^FO24,' +
          String(y) +
          '^A0N,20,20^FD' +
          this.fitToWidth(this.sanitize(instruction), 760, 10) +
          '^FS',
      );
      y += 22;
    }
    // receipt stub, torn off by the sender
    lines.push('^FO24,1150^GB764,2,2^FS');
    lines.push('^FO24,1160^A0N,18,18^FDRECEIPT^FS');
    lines.push('^FO120,1160^A0N,18,18^FD' + this.sanitize(label.trackingNumber) + '^FS');
    lines.push('^FO420,1160^A0N,18,18^FD' + ship + '^FS');
    lines.push('^FO560,1160^A0N,18,18^FD' + this.sanitize(label.serviceCode) + '^FS');
    lines.push(
      '^FO24,1184^A0N,18,18^FD' +
        this.fitToWidth(this.sanitize(label.recipient.name), 380, 9) +
        '^FS',
    );
    lines.push(
      '^FO420,1184^A0N,18,18^FD' +
        this.sanitize(label.recipient.postcode) +
        ' ' +
        this.sanitize(label.recipient.country) +
        '^FS',
    );
    // print quantity and end
    lines.push('^PQ1,0,1,Y');
    lines.push('^XZ');
    return lines.join('\n');
  }

  /** Shortens `text` until it fits a field `fieldWidthDots` wide in a font `fontWidthDots` per character. */
  fitToWidth(text: string, fieldWidthDots: number, fontWidthDots: number): string {
    let fitted = text;
    while (fitted.length * fontWidthDots + 2 * marginDots > fieldWidthDots) {
      fitted = fitted.slice(0, -1);
    }
    return fitted;
  }

  /** Like fitToWidth for the reference fields, which may be left empty. */
  truncateReference(text: string, fieldWidthDots: number, fontWidthDots: number): string {
    let fitted = text;
    while (fitted.length > 0 && fitted.length * fontWidthDots > fieldWidthDots) {
      fitted = fitted.slice(0, -1);
    }
    return fitted;
  }

  /** Field data must not contain ZPL command characters or control characters. */
  private sanitize(value: string): string {
    // eslint-disable-next-line no-control-regex -- stripping control characters is the point here
    return value.replace(/[\u0000-\u001f\u007f]/g, '').replace(/[\^~]/g, ' ');
  }

  /** The tracking number in groups of four for the human-readable line. */
  private spaced(trackingNumber: string): string {
    return this.sanitize(trackingNumber).replace(/(.{4})(?=.)/g, '$1 ');
  }
}
