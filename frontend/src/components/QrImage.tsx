import { useEffect, useState } from 'react';
import QRCode from 'qrcode';

/** Draws a value as a QR code (an SVG data URL), large enough to scan from the back of a lecture hall. */
export function QrImage({ value, label, size = 320 }: { value: string; label: string; size?: number }) {
  const [src, setSrc] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    QRCode.toString(value, { type: 'svg', margin: 1, errorCorrectionLevel: 'M', color: { dark: '#000000', light: '#ffffff' } })
      .then((svg) => {
        if (!cancelled) setSrc(`data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`);
      })
      .catch(() => setSrc(null));
    return () => {
      cancelled = true;
    };
  }, [value]);

  // White background in both themes: phones scan dark-on-light codes most reliably.
  return src ? <img src={src} alt={label} width={size} height={size} className="max-w-full rounded-lg bg-white p-2" /> : null;
}
