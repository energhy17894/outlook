import {
  Body1,
  Button,
  Input,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
  Title2,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

const useStyles = makeStyles({
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalL,
    maxWidth: '640px',
  },
});

/**
 * Placeholder for the read-only Q&A agent (MAF 1.x, no tools that write —
 * see §1 "AI katmanı"). Disabled input keeps the shape of the real feature
 * visible without wiring a backend in Faz 0.
 */
export function AskPage() {
  const styles = useStyles();
  const { t } = useTranslation();
  const [value, setValue] = useState('');

  return (
    <div>
      <Title2>{t('ask.title')}</Title2>
      <Body1 as="p" />
      <MessageBar intent="info">
        <MessageBarBody>
          <MessageBarTitle>TODO</MessageBarTitle>
          {t('ask.todo')}
        </MessageBarBody>
      </MessageBar>
      <div className={styles.row}>
        <Input
          placeholder={t('ask.placeholder')}
          value={value}
          onChange={(_, data) => setValue(data.value)}
          disabled
          style={{ flexGrow: 1 }}
        />
        <Button appearance="primary" disabled>
          {t('ask.title')}
        </Button>
      </div>
      <Body1 as="p">{t('ask.disabledHint')}</Body1>
    </div>
  );
}
