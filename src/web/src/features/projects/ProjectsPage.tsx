import {
  Body1,
  Caption1,
  Card,
  CardHeader,
  Subtitle1,
  Title2,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../../shared/api-client';
import { PhaseBadge } from './PhaseBadge';
import { HealthBadge } from './HealthBadge';

const useStyles = makeStyles({
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalL,
  },
  card: {
    cursor: 'pointer',
  },
  headerRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
});

export function ProjectsPage() {
  const styles = useStyles();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { data: projects, isLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: () => apiClient.projects.list(),
  });

  return (
    <div>
      <Title2>{t('projects.title')}</Title2>
      <Body1 as="p">{t('projects.subtitle')}</Body1>

      {isLoading && <Caption1>{t('common.loading')}</Caption1>}

      <div className={styles.grid}>
        {projects?.map((project) => (
          <Card
            key={project.id}
            className={styles.card}
            onClick={() => navigate(`/projects/${project.id}`)}
            data-testid={`project-card-${project.id}`}
          >
            <CardHeader
              header={<Subtitle1>{project.name}</Subtitle1>}
              description={<Caption1>{project.customerOrg}</Caption1>}
            />
            <div className={styles.headerRow}>
              <PhaseBadge phase={project.currentPhase} />
              <HealthBadge band={project.health.band} score={project.health.score} />
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
