import { Body1, MessageBar, MessageBarBody, MessageBarTitle, Title2 } from '@fluentui/react-components';
import { useTranslation } from 'react-i18next';

/**
 * Placeholder. Planned content (§3 PolicyRule, §4): excluded
 * mailboxes/folders/sites/domains/subject terms, label rules, special-category
 * data classifier thresholds, provider-tier permissions (Tier 1/2/3), model
 * catalogue, audit event viewer, user/role management.
 */
export function AdminPage() {
  const { t } = useTranslation();
  return (
    <div>
      <Title2>{t('admin.title')}</Title2>
      <Body1 as="p" />
      <MessageBar intent="info">
        <MessageBarBody>
          <MessageBarTitle>TODO</MessageBarTitle>
          {t('admin.todo')}
        </MessageBarBody>
      </MessageBar>
    </div>
  );
}
