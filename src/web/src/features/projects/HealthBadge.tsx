import { Badge } from '@fluentui/react-components';
import { useTranslation } from 'react-i18next';
import type { Project } from '../../shared/api-client';

const colorByBand: Record<Project['health']['band'], 'success' | 'warning' | 'danger'> = {
  good: 'success',
  watch: 'warning',
  at_risk: 'danger',
};

export function HealthBadge({ band, score }: { band: Project['health']['band']; score: number }) {
  const { t } = useTranslation();
  return (
    <Badge color={colorByBand[band]} appearance="filled">
      {t(`projects.health.${band}`)} · {score}
    </Badge>
  );
}
