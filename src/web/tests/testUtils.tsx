import type { ReactElement } from 'react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import i18n from '../src/shared/i18n';

export function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <I18nextProvider i18n={i18n}>
      <FluentProvider theme={webLightTheme}>
        <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
      </FluentProvider>
    </I18nextProvider>,
  );
}
