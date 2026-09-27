import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ProposalCard } from '../src/features/review-queue/ProposalCard';
import type { ActionProposal } from '../src/shared/api-client';
import { renderWithProviders } from './testUtils';

const proposal: ActionProposal = {
  id: 'ap-test',
  kind: 'reply_draft',
  title: 'Test öneri',
  summary: 'Özet',
  payloadPreview: 'Merhaba, ...',
  rationale: 'Çünkü...',
  evidence: [
    {
      id: 'ev-test',
      sourceType: 'mail',
      exactQuote: 'Test alıntı',
      author: 'Test Kullanıcı',
      sourceTimestampUtc: '2026-09-15T08:12:00Z',
      verified: true,
    },
  ],
  riskFlags: [],
  status: 'pending_approval',
};

describe('ProposalCard', () => {
  it('does not approve until the confirmation dialog is accepted', async () => {
    const user = userEvent.setup();
    const onApprove = vi.fn();

    renderWithProviders(
      <ProposalCard proposal={proposal} onApprove={onApprove} onReject={vi.fn()} onEditSave={vi.fn()} />,
    );

    // Clicking Onayla only opens the confirmation dialog; approve is not
    // called yet (no auto-send / no silent approval).
    await user.click(screen.getByTestId('approve-button'));
    expect(onApprove).not.toHaveBeenCalled();
    expect(screen.getByText('Onayı onayla')).toBeInTheDocument();

    await user.click(screen.getByTestId('confirm-approve-button'));

    await waitFor(() => expect(onApprove).toHaveBeenCalledTimes(1));
  });
});
