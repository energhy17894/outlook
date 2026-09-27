import {
  Hamburger,
  NavDrawer,
  NavDrawerBody,
  NavDrawerHeader,
  NavItem,
  NavSectionHeader,
  Title3,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import {
  AppsListDetail24Regular,
  BoardRegular,
  BriefcaseSearch24Regular,
  ChatBubblesQuestion24Regular,
  Clock24Regular,
  PersonBoard24Regular,
  Settings24Regular,
  Timeline24Regular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Route, Routes, useLocation } from 'react-router-dom';
import { ProjectsPage } from '../features/projects/ProjectsPage';
import { ProjectDetailPage } from '../features/projects/ProjectDetailPage';
import { MyWorkPage } from '../features/my-work/MyWorkPage';
import { ReviewQueuePage } from '../features/review-queue/ReviewQueuePage';
import { TimelinePage } from '../features/timeline/TimelinePage';
import { DashboardsPage } from '../features/dashboards/DashboardsPage';
import { AskPage } from '../features/ask/AskPage';
import { AdminPage } from '../features/admin/AdminPage';
import { SetupPage } from '../features/setup/SetupPage';

const useStyles = makeStyles({
  layout: {
    display: 'flex',
    minHeight: '100vh',
    backgroundColor: tokens.colorNeutralBackground3,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
  },
  content: {
    flexGrow: 1,
    padding: tokens.spacingVerticalL,
    minWidth: 0,
  },
});

const navItems = [
  { to: '/projects', labelKey: 'nav.projects', icon: <AppsListDetail24Regular /> },
  { to: '/my-work', labelKey: 'nav.myWork', icon: <BriefcaseSearch24Regular /> },
  { to: '/review-queue', labelKey: 'nav.reviewQueue', icon: <Clock24Regular /> },
  { to: '/timeline', labelKey: 'nav.timeline', icon: <Timeline24Regular /> },
  { to: '/dashboards', labelKey: 'nav.dashboards', icon: <BoardRegular /> },
  { to: '/ask', labelKey: 'nav.ask', icon: <ChatBubblesQuestion24Regular /> },
  { to: '/admin', labelKey: 'nav.admin', icon: <PersonBoard24Regular /> },
  { to: '/setup', labelKey: 'nav.setup', icon: <Settings24Regular /> },
] as const;

export function AppShell() {
  const styles = useStyles();
  const { t } = useTranslation();
  const location = useLocation();
  const [open, setOpen] = useState(true);

  return (
    <div className={styles.layout}>
      <NavDrawer open={open} type="inline" selectedValue={location.pathname}>
        <NavDrawerHeader>
          <div className={styles.header}>
            <Hamburger onClick={() => setOpen((v) => !v)} aria-label="Menüyü daralt/genişlet" />
            <Title3>{t('appName')}</Title3>
          </div>
        </NavDrawerHeader>
        <NavDrawerBody>
          <NavSectionHeader>OpsIntel</NavSectionHeader>
          {navItems.map((item) => (
            <NavItem key={item.to} value={item.to} icon={item.icon} href={`#${item.to}`}>
              {t(item.labelKey)}
            </NavItem>
          ))}
        </NavDrawerBody>
      </NavDrawer>

      <main className={styles.content}>
        <Routes>
          <Route path="/" element={<ProjectsPage />} />
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="/projects/:projectId" element={<ProjectDetailPage />} />
          <Route path="/my-work" element={<MyWorkPage />} />
          <Route path="/review-queue" element={<ReviewQueuePage />} />
          <Route path="/timeline" element={<TimelinePage />} />
          <Route path="/dashboards" element={<DashboardsPage />} />
          <Route path="/ask" element={<AskPage />} />
          <Route path="/admin" element={<AdminPage />} />
          <Route path="/setup" element={<SetupPage />} />
        </Routes>
      </main>
    </div>
  );
}
