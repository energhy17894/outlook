import { Body1, Caption1, Card, CardHeader, Subtitle1, Title2, Title3, makeStyles, tokens } from '@fluentui/react-components';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { apiClient } from '../../shared/api-client';
import type { WorkItem } from '../../shared/api-client';
import { ConfidenceBadge, EvidenceList } from '../../shared/evidence';

const useStyles = makeStyles({
  section: {
    marginTop: tokens.spacingVerticalL,
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  cardHeaderRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

function WorkItemList({ items, emptyLabel }: { items: WorkItem[]; emptyLabel: string }) {
  const styles = useStyles();
  const { t } = useTranslation();
  if (items.length === 0) {
    return <Caption1>{emptyLabel}</Caption1>;
  }
  return (
    <div className={styles.list}>
      {items.map((item) => (
        <Card key={item.id} data-testid={`my-work-item-${item.id}`}>
          <CardHeader
            header={
              <div className={styles.cardHeaderRow}>
                <Subtitle1>{item.title}</Subtitle1>
                <ConfidenceBadge level={item.confidence} />
              </div>
            }
            description={
              <Caption1>
                {t(`workItemKind.${item.kind}`)}
                {item.dueText ? ` · ${item.dueText}` : ''}
                {item.counterparty ? ` · ${item.counterparty}` : ''}
              </Caption1>
            }
          />
          <EvidenceList evidence={item.evidence} />
        </Card>
      ))}
    </div>
  );
}

export function MyWorkPage() {
  const styles = useStyles();
  const { t } = useTranslation();
  const { data } = useQuery({
    queryKey: ['my-work'],
    queryFn: () => apiClient.myWork.summary(),
  });

  return (
    <div>
      <Title2>{t('myWork.title')}</Title2>

      <section className={styles.section}>
        <Title3>{t('myWork.myTasks')}</Title3>
        <Body1 as="p" />
        <WorkItemList items={data?.myTasks ?? []} emptyLabel={t('myWork.noItems')} />
      </section>

      <section className={styles.section}>
        <Title3>{t('myWork.waitingOn')}</Title3>
        <WorkItemList items={data?.waitingOn ?? []} emptyLabel={t('myWork.noItems')} />
      </section>
    </div>
  );
}
