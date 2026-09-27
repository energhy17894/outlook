import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import tr from './locales/tr';
import en from './locales/en';

// Turkish-first per ADR context (§1, §4): the product targets Turkish
// enterprises and KVKK compliance; English is a secondary, fully-supported
// locale. No CDN font/script loading — resources are bundled, compatible
// with a strict `default-src 'self'` CSP.
void i18n.use(initReactI18next).init({
  resources: { tr, en },
  lng: 'tr',
  fallbackLng: 'tr',
  interpolation: { escapeValue: false },
  returnEmptyString: false,
});

export default i18n;
