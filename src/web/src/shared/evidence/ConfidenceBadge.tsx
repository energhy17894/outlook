import { Badge } from '@fluentui/react-components';
import { useTranslation } from 'react-i18next';
import type { ConfidenceLevel } from '../api-client';

const colorByLevel: Record<ConfidenceLevel, 'success' | 'warning' | 'danger'> = {
  high: 'success',
  medium: 'warning',
  low: 'danger',
};

export function ConfidenceBadge({ level }: { level: ConfidenceLevel }) {
  const { t } = useTranslation();
  return (
    <Badge color={colorByLevel[level]} appearance="tint">
      {t(`confidence.${level}`)}
    </Badge>
  );
}
