import { makeStyles, tokens } from '@fluentui/react-components';
import type { Evidence } from '../api-client';
import { EvidenceQuote } from './EvidenceQuote';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
});

export function EvidenceList({ evidence }: { evidence: Evidence[] }) {
  const styles = useStyles();
  if (evidence.length === 0) return null;
  return (
    <div className={styles.root}>
      {evidence.map((e) => (
        <EvidenceQuote key={e.id} evidence={e} />
      ))}
    </div>
  );
}
