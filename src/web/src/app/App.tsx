import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { HashRouter } from 'react-router-dom';
import { AppShell } from './AppShell';

// HashRouter avoids relying on server-side history-API fallback config,
// which keeps deployment under Kestrel's static file server (wwwroot)
// simple and matches the strict same-origin posture in §1 (ADR-0003).
const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
});

export default function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <QueryClientProvider client={queryClient}>
        <HashRouter>
          <AppShell />
        </HashRouter>
      </QueryClientProvider>
    </FluentProvider>
  );
}
