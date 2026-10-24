import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

/**
 * The signed-out frame: a centered card with the university name. It uses the language last chosen on this device
 * (Arabic at first); the language is changed in Profile & settings.
 */
export function AuthLayout({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-4 py-10">
      <div className="w-full max-w-md">
        <div className="mb-6 flex items-center">
          <span className="flex items-center gap-2 font-bold text-primary">
            <img src="/favicon.svg" alt="" className="size-8" />
            {t('app.name')}
          </span>
        </div>
        <div className="rounded-2xl border border-line bg-surface p-6 shadow-sm sm:p-8">
          <h1 className="text-xl font-bold">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-muted">{subtitle}</p>}
          <div className="mt-6">{children}</div>
        </div>
      </div>
    </div>
  );
}
