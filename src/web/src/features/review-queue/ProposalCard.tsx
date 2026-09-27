import {
  Body1,
  Button,
  Card,
  CardFooter,
  CardHeader,
  Caption1,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Subtitle1,
  Textarea,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ActionProposal } from '../../shared/api-client';
import { EvidenceList } from '../../shared/evidence';

const useStyles = makeStyles({
  body: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: `0 ${tokens.spacingHorizontalL}`,
  },
  payload: {
    fontFamily: tokens.fontFamilyMonospace,
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    whiteSpace: 'pre-wrap',
  },
  flags: {
    color: tokens.colorPaletteDarkOrangeForeground1,
  },
});

const REJECT_REASONS = [
  { code: 'not_needed', labelKey: 'reject_reason.not_needed' },
  { code: 'wrong_owner', labelKey: 'reject_reason.wrong_owner' },
  { code: 'inaccurate', labelKey: 'reject_reason.inaccurate' },
];

export interface ProposalCardProps {
  proposal: ActionProposal;
  onApprove: () => void;
  onReject: (reasonCode: string) => void;
  onEditSave: (payload: string) => void;
}

/**
 * A single review-queue card: reply draft / task / meeting proposal, with
 * evidence, and Onayla / Düzenle / Reddet actions. Approving and rejecting
 * both go through a confirmation dialog — nothing here sends anything by
 * itself (see ADR-0008: Mail.Send is never requested; §4 execution always
 * needs a human-approved state transition).
 */
export function ProposalCard({ proposal, onApprove, onReject, onEditSave }: ProposalCardProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const [approveOpen, setApproveOpen] = useState(false);
  const [rejectOpen, setRejectOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [reasonCode, setReasonCode] = useState(REJECT_REASONS[0].code);
  const [draft, setDraft] = useState(proposal.payloadPreview);

  return (
    <Card data-testid={`proposal-card-${proposal.id}`}>
      <CardHeader
        header={<Subtitle1>{proposal.title}</Subtitle1>}
        description={<Caption1>{proposal.summary}</Caption1>}
      />
      <div className={styles.body}>
        <Body1 className={styles.payload}>{proposal.payloadPreview}</Body1>
        <Caption1>
          <strong>{t('reviewQueue.rationale')}:</strong> {proposal.rationale}
        </Caption1>
        {proposal.riskFlags.length > 0 && (
          <Caption1 className={styles.flags}>
            <strong>{t('reviewQueue.riskFlags')}:</strong> {proposal.riskFlags.join(', ')}
          </Caption1>
        )}
        <EvidenceList evidence={proposal.evidence} />
      </div>

      <CardFooter>
        <Button appearance="primary" onClick={() => setApproveOpen(true)} data-testid="approve-button">
          {t('reviewQueue.approve')}
        </Button>
        <Button onClick={() => setEditOpen(true)}>{t('reviewQueue.edit')}</Button>
        <Button appearance="secondary" onClick={() => setRejectOpen(true)}>
          {t('reviewQueue.reject')}
        </Button>
      </CardFooter>

      {/* Approve confirmation */}
      <Dialog open={approveOpen} onOpenChange={(_, data) => setApproveOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('reviewQueue.confirmApproveTitle')}</DialogTitle>
            <DialogContent>{t('reviewQueue.confirmApproveBody')}</DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">{t('reviewQueue.confirmApproveCancel')}</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                data-testid="confirm-approve-button"
                onClick={() => {
                  onApprove();
                  setApproveOpen(false);
                }}
              >
                {t('reviewQueue.confirmApproveOk')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {/* Reject confirmation with reason code */}
      <Dialog open={rejectOpen} onOpenChange={(_, data) => setRejectOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('reviewQueue.confirmRejectTitle')}</DialogTitle>
            <DialogContent>
              <Body1 as="p">{t('reviewQueue.confirmRejectBody')}</Body1>
              <select
                aria-label={t('reviewQueue.rationale')}
                value={reasonCode}
                onChange={(e) => setReasonCode(e.target.value)}
              >
                {REJECT_REASONS.map((r) => (
                  <option key={r.code} value={r.code}>
                    {r.code}
                  </option>
                ))}
              </select>
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">{t('reviewQueue.confirmApproveCancel')}</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                onClick={() => {
                  onReject(reasonCode);
                  setRejectOpen(false);
                }}
              >
                {t('reviewQueue.confirmRejectOk')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {/* Edit draft */}
      <Dialog open={editOpen} onOpenChange={(_, data) => setEditOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>{t('reviewQueue.editDialogTitle')}</DialogTitle>
            <DialogContent>
              <Body1 as="p">{t('reviewQueue.editDialogBody')}</Body1>
              <Textarea
                value={draft}
                onChange={(_, data) => setDraft(data.value)}
                rows={6}
                style={{ width: '100%' }}
              />
            </DialogContent>
            <DialogActions>
              <DialogTrigger disableButtonEnhancement>
                <Button appearance="secondary">{t('reviewQueue.confirmApproveCancel')}</Button>
              </DialogTrigger>
              <Button
                appearance="primary"
                onClick={() => {
                  onEditSave(draft);
                  setEditOpen(false);
                }}
              >
                {t('reviewQueue.editDialogSave')}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </Card>
  );
}
