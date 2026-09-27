import { Body1, MessageBar, MessageBarBody, MessageBarTitle, Title2 } from '@fluentui/react-components';
import { useTranslation } from 'react-i18next';

/**
 * Placeholder. Planned content (§5 ADR-0020, ozellikler_ux.md "KPI catalogue"):
 * an Apache ECharts portfolio dashboard covering commitment reliability,
 * decision velocity, open-question aging, risk exposure trend and process
 * bottlenecks — all drilling down to evidence. Kept out of Faz 0 to keep the
 * dependency footprint modest; wired in Faz 1 S7 / Faz 3 S16.
 */
export function DashboardsPage() {
  const { t } = useTranslation();
  return (
    <div>
      <Title2>{t('dashboards.title')}</Title2>
      <Body1 as="p" />
      <MessageBar intent="info">
        <MessageBarBody>
          <MessageBarTitle>TODO</MessageBarTitle>
          {t('dashboards.todo')}
        </MessageBarBody>
      </MessageBar>
    </div>
  );
}
