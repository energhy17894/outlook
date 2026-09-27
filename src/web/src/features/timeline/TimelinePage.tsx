import { Body1, Caption1, Card, CardHeader, Subtitle2, Title2, makeStyles, tokens } from '@fluentui/react-components';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { apiClient } from '../../shared/api-client';
import { EvidenceQuote } from '../../shared/evidence';

const useStyles = makeStyles({
  rail: {
    display: 'flex',
    flexDirection: 'column',
    gap: 0,
    marginTop: tokens.spacingVerticalL,
    borderLeftWidth: tokens.strokeWidthThin,
    borderLeftStyle: 'solid',
    borderLeftColor: tokens.colorNeutralStroke2,
    paddingLeft: tokens.spacingHorizontalL,
  },
  item: {
    position: 'relative',
    marginBottom: tokens.spacingVerticalL,
  },
  dot: {
    position: 'absolute',
    left: `calc(-1 * ${tokens.spacingHorizontalL} - 5px)`,
    top: '6px',
    width: '10px',
    height: '10px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
  },
});

/**
 * Simple, lightweight event list rendered as a vertical timeline.
 *
 * TODO(Faz 2+): swap this list for the planned vis-timeline / React Flow
 * visualisation once swimlanes-per-organization and phase bands are needed
 * (see §5 ADR-0020 and ozellikler_ux.md "Project timeline with swimlanes").
 * Kept dependency-light for Faz 0.
 */
export function TimelinePage() {
  const styles = useStyles();
  const { t, i18n } = useTranslation();
  const { data: events } = useQuery({
    queryKey: ['timeline'],
    queryFn: () => apiClient.timeline.list(),
  });

  return (
    <div>
      <Title2>{t('timeline.title')}</Title2>
      <Body1 as="p">{t('timeline.subtitle')}</Body1>

      <div className={styles.rail}>
        {(events ?? []).length === 0 && <Caption1>{t('timeline.empty')}</Caption1>}
        {events?.map((event) => (
          <div className={styles.item} key={event.id} data-testid={`timeline-event-${event.id}`}>
            <span className={styles.dot} />
            <Card>
              <CardHeader
                header={<Subtitle2>{event.activity}</Subtitle2>}
                description={
                  <Caption1>
                    {new Intl.DateTimeFormat(i18n.language, { dateStyle: 'medium', timeStyle: 'short' }).format(
                      new Date(event.timestampUtc),
                    )}{' '}
                    · {event.actor}
                  </Caption1>
                }
              />
              {event.evidence && <EvidenceQuote evidence={event.evidence} />}
            </Card>
          </div>
        ))}
      </div>
    </div>
  );
}
