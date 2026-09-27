import {
  Body1,
  Button,
  Card,
  CardHeader,
  Checkbox,
  Subtitle1,
  Title2,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle24Filled, Circle24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

const useStyles = makeStyles({
  steps: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
    maxWidth: '640px',
  },
  stepHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  footer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
});

type StepKey = 'signIn' | 'folders' | 'modelTier' | 'consent';
const STEP_ORDER: StepKey[] = ['signIn', 'folders', 'modelTier', 'consent'];

/**
 * First-run wizard, mock implementation. Real steps run inside
 * OpsIntel.Host (§1: BFF PKCE sign-in, delta-sync folder/site selection,
 * Foundry Local as the default model tier, and the KVKK/consent gate before
 * setup can finish per §4).
 */
export function SetupPage() {
  const styles = useStyles();
  const { t } = useTranslation();
  const [completed, setCompleted] = useState<Record<StepKey, boolean>>({
    signIn: false,
    folders: false,
    modelTier: false,
    consent: false,
  });
  const [consentChecked, setConsentChecked] = useState(false);

  const currentIndex = STEP_ORDER.findIndex((key) => !completed[key]);
  const done = currentIndex === -1;

  const complete = (key: StepKey) => setCompleted((prev) => ({ ...prev, [key]: true }));

  return (
    <div>
      <Title2>{t('setup.title')}</Title2>
      <Body1 as="p">{t('setup.subtitle')}</Body1>

      <div className={styles.steps}>
        {STEP_ORDER.map((key, index) => {
          const isCurrent = index === currentIndex;
          const isDone = completed[key];
          const isLocked = !isDone && !isCurrent;
          return (
            <Card key={key} data-testid={`setup-step-${key}`}>
              <CardHeader
                header={
                  <div className={styles.stepHeader}>
                    {isDone ? <CheckmarkCircle24Filled /> : <Circle24Regular />}
                    <Subtitle1>{t(`setup.steps.${key}.title`)}</Subtitle1>
                  </div>
                }
                description={<Body1>{t(`setup.steps.${key}.body`)}</Body1>}
              />
              {key === 'consent' && isCurrent && (
                <Checkbox
                  label={t('setup.steps.consent.action')}
                  checked={consentChecked}
                  onChange={(_, data) => setConsentChecked(!!data.checked)}
                />
              )}
              <div className={styles.footer}>
                <Button
                  appearance="primary"
                  disabled={isLocked || isDone || (key === 'consent' && !consentChecked)}
                  onClick={() => complete(key)}
                >
                  {key === 'consent' ? t('setup.finish') : t(`setup.steps.${key}.action`)}
                </Button>
              </div>
            </Card>
          );
        })}

        {done && (
          <Card>
            <Body1 as="p">{t('setup.steps.done')}</Body1>
          </Card>
        )}
      </div>
    </div>
  );
}
