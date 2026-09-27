import { Body1, Caption1, Title2, makeStyles, tokens } from '@fluentui/react-components';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { apiClient } from '../../shared/api-client';
import { ProposalCard } from './ProposalCard';

const useStyles = makeStyles({
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
  },
});

export function ReviewQueuePage() {
  const styles = useStyles();
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data: proposals } = useQuery({
    queryKey: ['review-queue'],
    queryFn: () => apiClient.reviewQueue.list(),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['review-queue'] });

  const approveMutation = useMutation({
    mutationFn: (id: string) => apiClient.reviewQueue.approve(id),
    onSuccess: invalidate,
  });
  const rejectMutation = useMutation({
    mutationFn: ({ id, reasonCode }: { id: string; reasonCode: string }) =>
      apiClient.reviewQueue.reject(id, reasonCode),
    onSuccess: invalidate,
  });
  const editMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: string }) => apiClient.reviewQueue.edit(id, payload),
    onSuccess: invalidate,
  });

  const pending = (proposals ?? []).filter((p) => p.status === 'pending_approval' || p.status === 'suggested');

  return (
    <div>
      <Title2>{t('reviewQueue.title')}</Title2>
      <Body1 as="p">{t('reviewQueue.subtitle')}</Body1>

      <div className={styles.list}>
        {pending.length === 0 && <Caption1>{t('reviewQueue.empty')}</Caption1>}
        {pending.map((proposal) => (
          <ProposalCard
            key={proposal.id}
            proposal={proposal}
            onApprove={() => approveMutation.mutate(proposal.id)}
            onReject={(reasonCode) => rejectMutation.mutate({ id: proposal.id, reasonCode })}
            onEditSave={(payload) => editMutation.mutate({ id: proposal.id, payload })}
          />
        ))}
      </div>
    </div>
  );
}
