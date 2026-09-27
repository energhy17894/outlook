import { Badge } from '@fluentui/react-components';
import { useTranslation } from 'react-i18next';
import type { ProjectPhase } from '../../shared/api-client';

export function PhaseBadge({ phase }: { phase: ProjectPhase }) {
  const { t } = useTranslation();
  return (
    <Badge appearance="outline" shape="rounded" color="informative">
      {t(`phase.${phase}`)}
    </Badge>
  );
}
