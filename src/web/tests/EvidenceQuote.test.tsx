import { describe, expect, it } from 'vitest';
import { screen } from '@testing-library/react';
import { EvidenceQuote } from '../src/shared/evidence';
import type { Evidence } from '../src/shared/api-client';
import { renderWithProviders } from './testUtils';

const evidence: Evidence = {
  id: 'ev-test',
  sourceType: 'mail',
  exactQuote: "Cuma'ya kadar revize teklifi göndereceğim.",
  author: 'Ahmet Yılmaz',
  sourceTimestampUtc: '2026-09-15T08:12:00Z',
  outlookWebLink: 'https://outlook.office.com/mail/deeplink/read/AAMk...',
  verified: true,
};

describe('EvidenceQuote', () => {
  it('renders the exact quote, sender, date and an Outlook deep link', () => {
    renderWithProviders(<EvidenceQuote evidence={evidence} />);

    expect(screen.getByText(/Cuma'ya kadar revize teklifi göndereceğim\./)).toBeInTheDocument();
    expect(screen.getByText('Ahmet Yılmaz')).toBeInTheDocument();

    const link = screen.getByRole('link', { name: "Outlook'ta aç" });
    expect(link).toHaveAttribute('href', evidence.outlookWebLink);
  });

  it('flags unverified evidence instead of showing it as fact', () => {
    renderWithProviders(<EvidenceQuote evidence={{ ...evidence, verified: false }} />);
    expect(screen.getByText(/doğrulanamadı/)).toBeInTheDocument();
  });
});
