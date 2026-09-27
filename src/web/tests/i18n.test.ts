import { describe, expect, it } from 'vitest';
import i18n from '../src/shared/i18n';

describe('i18n', () => {
  it('defaults to Turkish', () => {
    expect(i18n.language).toBe('tr');
    expect(i18n.t('nav.projects')).toBe('Projeler');
  });

  it('supports switching to English', async () => {
    await i18n.changeLanguage('en');
    expect(i18n.t('nav.projects')).toBe('Projects');
    await i18n.changeLanguage('tr');
  });
});
