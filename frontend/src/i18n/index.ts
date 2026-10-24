import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { ar } from './ar';
import { en } from './en';

export type Language = 'ar' | 'en';

const STORAGE_KEY = 'nimi.language';

function savedLanguage(): Language {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'en' ? 'en' : 'ar';
  } catch {
    return 'ar';
  }
}

/** Arabic is right-to-left; the whole layout follows <html dir>. */
export function directionOf(language: Language): 'rtl' | 'ltr' {
  return language === 'ar' ? 'rtl' : 'ltr';
}

export function applyLanguage(language: Language) {
  document.documentElement.lang = language;
  document.documentElement.dir = directionOf(language);
  try {
    localStorage.setItem(STORAGE_KEY, language);
  } catch {
    // Storage can be blocked (private mode); the choice then lasts for this visit only.
  }
}

void i18n.use(initReactI18next).init({
  resources: { ar: { translation: ar }, en: { translation: en } },
  lng: savedLanguage(),
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
  returnObjects: true,
});

applyLanguage(i18n.language as Language);
i18n.on('languageChanged', (language) => applyLanguage(language as Language));

/** The name in the user's language: the API sends English and Arabic side by side (name / nameAr). */
export function localName(item: { name?: string | null; nameAr?: string | null } | null | undefined, language: string): string {
  if (!item) return '';
  return (language === 'ar' ? item.nameAr || item.name : item.name || item.nameAr) ?? '';
}

export default i18n;
