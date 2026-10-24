import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import i18n from '../../i18n';
import { setSession } from '../../api/session';
import { TermsPage } from './TermsPage';

const terms = [
  { id: '0198a1b2-0000-7000-8000-000000000001', academicYear: 2026, number: 'first', startsOn: '2026-09-19', endsOn: '2027-01-15', weekCount: 17, status: 'closed' },
  { id: '0198a1b2-0000-7000-8000-000000000002', academicYear: 2026, number: 'second', startsOn: '2027-02-06', endsOn: '2027-06-04', weekCount: 17, status: 'active' },
];

function page(items: unknown[]) {
  return new Response(JSON.stringify({ items, page: 1, pageSize: 100, totalCount: items.length, totalPages: 1 }), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}

afterEach(async () => {
  vi.unstubAllGlobals();
  setSession(null);
  await i18n.changeLanguage('ar');
});

describe('Terms page', () => {
  it('copies a term of any status into a new draft, starting from that row', async () => {
    await i18n.changeLanguage('en');
    vi.stubGlobal('fetch', vi.fn(async (request: Request) =>
      page(new URL(request.url).pathname === '/api/terms' ? terms : [])));
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <TermsPage />
      </QueryClientProvider>,
    );

    const copyButtons = await screen.findAllByRole('button', { name: 'Copy to new term' });
    expect(copyButtons).toHaveLength(2);           // closed and active terms alike

    await userEvent.click(copyButtons[1]);          // the active Second 2026 term

    expect(screen.getByText('New term from Second 2026')).toBeInTheDocument();
    expect(screen.getByLabelText('Academic year')).toHaveValue(2027);
    expect(screen.getByLabelText('Term')).toHaveValue('second');
    expect(screen.getByLabelText('Starts')).toHaveValue('2028-02-05');
  });
});
