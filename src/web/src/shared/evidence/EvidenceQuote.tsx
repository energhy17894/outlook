import { Caption1, Link, makeStyles, tokens } from '@fluentui/react-components';
import { useTranslation } from 'react-i18next';
import type { Evidence } from '../api-client';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalS,
    borderLeftWidth: tokens.strokeWidthThick,
    borderLeftStyle: 'solid',
    borderLeftColor: tokens.colorBrandStroke1,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  quote: {
    fontStyle: 'italic',
    color: tokens.colorNeutralForeground1,
  },
  meta: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground3,
  },
});

export interface EvidenceQuoteProps {
  evidence: Evidence;
}

/**
 * Renders one evidence anchor: the exact quote, its sender/author and date,
 * and a deep link back to the source item ("Outlook'ta aç").
 *
 * Every item card in the app (WorkItem, ActionProposal, TimelineEvent, ...)
 * shows evidence through this component, so quotes are never paraphrased —
 * see docs/research/notes/ozellikler_ux.md on evidence-first item cards and
 * ADR-0015/0019 (deterministic quote verification, no unverified item is
 * ever shown as fact).
 */
export function EvidenceQuote({ evidence }: EvidenceQuoteProps) {
  const { t, i18n } = useTranslation();
  const styles = useStyles();
  const formattedDate = new Intl.DateTimeFormat(i18n.language, {
    dateStyle: 'medium',
  }).format(new Date(evidence.sourceTimestampUtc));

  return (
    <figure className={styles.root} data-testid="evidence-quote">
      <blockquote className={styles.quote}>&ldquo;{evidence.exactQuote}&rdquo;</blockquote>
      <figcaption className={styles.meta}>
        <Caption1>{evidence.author}</Caption1>
        <Caption1>·</Caption1>
        <Caption1>{formattedDate}</Caption1>
        {!evidence.verified && <Caption1>· {t('evidence.unverified')}</Caption1>}
        {evidence.outlookWebLink && (
          <>
            <Caption1>·</Caption1>
            <Link href={evidence.outlookWebLink} target="_blank" rel="noreferrer">
              {t('evidence.openInOutlook')}
            </Link>
          </>
        )}
      </figcaption>
    </figure>
  );
}
