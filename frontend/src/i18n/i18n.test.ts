import { describe, expect, it } from 'vitest';
import i18n, { directionOf, localName } from './index';
import { ar } from './ar';
import { en } from './en';

function keys(value: object, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) =>
    typeof child === 'object' && child !== null && !Array.isArray(child) ? keys(child, `${prefix}${key}.`) : [`${prefix}${key}`],
  );
}

describe('i18n', () => {
  it('starts in Arabic, right to left', () => {
    expect(i18n.language).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.documentElement.lang).toBe('ar');
  });

  it('switches the whole page to English, left to right', async () => {
    await i18n.changeLanguage('en');
    expect(document.documentElement.dir).toBe('ltr');
    expect(document.title).toBe('University Portal');
    expect(localStorage.getItem('nimi.language')).toBe('en');
    await i18n.changeLanguage('ar');
    expect(document.documentElement.dir).toBe('rtl');
    expect(document.title).toBe('البوابة الجامعية');
  });

  it('has the same keys in both languages', () => {
    expect(keys(en).sort()).toEqual(keys(ar).sort());
  });

  it('maps languages to directions and picks the right name', () => {
    expect(directionOf('ar')).toBe('rtl');
    expect(directionOf('en')).toBe('ltr');
    expect(localName({ name: 'Accounting', nameAr: 'المحاسبة' }, 'ar')).toBe('المحاسبة');
    expect(localName({ name: 'Accounting', nameAr: 'المحاسبة' }, 'en')).toBe('Accounting');
  });
});
