/** Locale-aware formatting. Arabic uses Western digits (as Egyptian universities print them) inside RTL text. */
function locale(language: string) {
  return language === 'ar' ? 'ar-EG-u-nu-latn' : 'en-GB';
}

export function formatDate(value: string | Date | null | undefined, language: string): string {
  if (!value) return '';
  const date = typeof value === 'string' ? new Date(value.length === 10 ? `${value}T00:00:00` : value) : value;
  return new Intl.DateTimeFormat(locale(language), { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

export function formatDateTime(value: string | Date | null | undefined, language: string): string {
  if (!value) return '';
  const date = typeof value === 'string' ? new Date(value) : value;
  return new Intl.DateTimeFormat(locale(language), { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date);
}

/** "09:00:00" → "09:00". */
export function formatTime(value: string | null | undefined): string {
  return value ? value.slice(0, 5) : '';
}

export function formatMoney(value: number | string | null | undefined, language: string): string {
  const amount = Number(value ?? 0);
  return new Intl.NumberFormat(locale(language), { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}

export function formatNumber(value: number | string | null | undefined, language: string, digits = 2): string {
  if (value === null || value === undefined || value === '') return '—';
  return new Intl.NumberFormat(locale(language), { maximumFractionDigits: digits }).format(Number(value));
}

/** yyyy-MM-dd of a local date (the API takes dates without time). */
export function isoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Saturday-based weeks, as Egyptian universities run them. */
export function startOfWeek(date: Date, weekStartDay = 6): Date {
  const result = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const diff = (result.getDay() - weekStartDay + 7) % 7;
  result.setDate(result.getDate() - diff);
  return result;
}

export function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/** The same weekday `weeks` weeks later, as yyyy-MM-dd in and out: last year's term dates, moved to this year. */
export function plusWeeks(value: string, weeks: number): string {
  return isoDate(addDays(new Date(`${value}T00:00:00`), weeks * 7));
}

/**
 * The API numbers weekdays from Saturday (0 = Saturday … 6 = Friday, ScheduleSlot.ProductDay); JavaScript and the
 * `days` texts start from Sunday. Converts the API's number to the JavaScript index.
 */
export function jsDayOf(productDay: number): number {
  return (productDay + 6) % 7;
}
