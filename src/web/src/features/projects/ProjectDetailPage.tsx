import {
  Body1,
  Caption1,
  Card,
  CardHeader,
  Link,
  Subtitle1,
  Tab,
  TabList,
  Title2,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { apiClient } from '../../shared/api-client';
import type { WorkItemKind } from '../../shared/api-client';
import { EvidenceList } from '../../shared/evidence';
import { PhaseBadge } from './PhaseBadge';
import { HealthBadge } from './HealthBadge';

const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
});

const tabToKinds: Record<string, WorkItemKind[]> = {
  risks: ['risk'],
  decisions: ['decision'],
  openQuestions: ['open_question'],
  tasks: ['task'],
};

export function ProjectDetailPage() {
  const styles = useStyles();
  const { t } = useTranslation();
  const { projectId } = useParams<{ projectId: string }>();
  const [tab, setTab] = useState<keyof typeof tabToKinds>('risks');

  const { data: project } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => apiClient.projects.get(projectId!),
    enabled: !!projectId,
  });

  const { data: workItems } = useQuery({
    queryKey: ['project-work-items', projectId],
    queryFn: () => apiClient.projects.workItems(projectId!),
    enabled: !!projectId,
  });

  const filtered = useMemo(
    () => (workItems ?? []).filter((w) => tabToKinds[tab].includes(w.kind)),
    [workItems, tab],
  );

  if (!project) {
    return <Caption1>{t('common.loading')}</Caption1>;
  }

  return (
    <div>
      <Link href="#/projects">{t('projects.backToList')}</Link>

      <div className={styles.header}>
        <Title2>{project.name}</Title2>
        <PhaseBadge phase={project.currentPhase} />
        <HealthBadge band={project.health.band} score={project.health.score} />
      </div>
      <Body1 as="p">{project.customerOrg}</Body1>

      <TabList
        selectedValue={tab}
        onTabSelect={(_, data) => setTab(data.value as keyof typeof tabToKinds)}
      >
        <Tab value="risks">{t('projects.raidTabs.risks')}</Tab>
        <Tab value="decisions">{t('projects.raidTabs.decisions')}</Tab>
        <Tab value="openQuestions">{t('projects.raidTabs.openQuestions')}</Tab>
        <Tab value="tasks">{t('projects.raidTabs.tasks')}</Tab>
      </TabList>

      <div className={styles.list}>
        {filtered.length === 0 && <Caption1>{t('projects.noItems')}</Caption1>}
        {filtered.map((item) => (
          <Card key={item.id} data-testid={`work-item-${item.id}`}>
            <CardHeader
              header={<Subtitle1>{item.title}</Subtitle1>}
              description={<Caption1>{t(`reviewState.${item.reviewState}`)}</Caption1>}
            />
            <EvidenceList evidence={item.evidence} />
          </Card>
        ))}
      </div>
    </div>
  );
}
