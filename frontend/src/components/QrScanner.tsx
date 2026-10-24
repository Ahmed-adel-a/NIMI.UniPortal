import { useEffect, useRef, useState } from 'react';
import jsQR from 'jsqr';
import { useTranslation } from 'react-i18next';

/**
 * Reads a QR code with the device camera (the back camera on phones) and reports the first value found. Decoding runs
 * on video frames with jsQR, so it works in every browser that grants camera access.
 */
export function QrScanner({ onResult }: { onResult: (value: string) => void }) {
  const { t } = useTranslation();
  const video = useRef<HTMLVideoElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let stream: MediaStream | null = null;
    let frame = 0;
    let done = false;

    async function start() {
      try {
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
      } catch {
        setError(t('attendance.cameraDenied'));
        return;
      }
      const v = video.current!;
      v.srcObject = stream;
      await v.play().catch(() => undefined);
      const tick = () => {
        if (done) return;
        const c = canvas.current;
        if (c && v.readyState === v.HAVE_ENOUGH_DATA) {
          c.width = v.videoWidth;
          c.height = v.videoHeight;
          const context = c.getContext('2d', { willReadFrequently: true });
          if (context) {
            context.drawImage(v, 0, 0, c.width, c.height);
            const code = jsQR(context.getImageData(0, 0, c.width, c.height).data, c.width, c.height);
            if (code?.data) {
              done = true;
              onResult(code.data);
              return;
            }
          }
        }
        frame = requestAnimationFrame(tick);
      };
      frame = requestAnimationFrame(tick);
    }

    void start();
    return () => {
      done = true;
      cancelAnimationFrame(frame);
      stream?.getTracks().forEach((track) => track.stop());
    };
  }, [onResult, t]);

  return (
    <div className="grid gap-2">
      {error ? (
        <p className="text-sm text-danger">{error}</p>
      ) : (
        <video ref={video} muted playsInline className="aspect-square w-full max-w-sm rounded-xl bg-black object-cover" />
      )}
      <canvas ref={canvas} className="hidden" />
    </div>
  );
}
