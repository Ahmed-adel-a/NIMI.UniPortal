import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import i18n from '../../i18n';
import { setSession } from '../../api/session';
import type { Schema } from '../../api/types';
import { CopyTermForm } from './CopyTermForm';
import { CopyTermResult } from './CopyTermResult';

const source: Schema<'TermDto'> = {
  id: '0198a1b2-0000-7000-8000-000000000001',
  academicYear: 2026,
  number: 'first',
  startsOn: '2026-09-19',
  endsOn: '2027-01-15',
  weekCount: 17,
  status: 'active',
};

const result: Schema<'CopyTermResultDto'> = {
  term: { ...source, id: '0198a1b2-0000-7000-8000-000000000002', academicYear: 2027, startsOn: '2027-09-18', endsOn: '2028-01-14', status: 'planned' },
  copied: { groups: 42, teachingAssignments: 38, slots: 120 },
  skipped: [
    { kind: 'slot', subjectCode: 'CS302', groupNumber: 1, reason: 'facility_inactive' },
    { kind: 'group', subjectCode: 'CS401', groupNumber: 2, reason: 'subject_inactive' },
  ],
};

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function renderForm(onDone = vi.fn()) {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { mutations: { retry: false } } })}>
      <CopyTermForm source={source} onDone={onDone} onCancel={() => {}} />
    </QueryClientProvider>,
  );
  return onDone;
}

afterEach(async () => {
  vi.unstubAllGlobals();
  setSession(null);
  await i18n.changeLanguage('ar');
});

describe('Copy a term', () => {
  it('starts from the next year, the same term and the same weekdays', async () => {
    await i18n.changeLanguage('en');
    renderForm();

    expect(screen.getByLabelText('Academic year')).toHaveValue(2027);
    expect(screen.getByLabelText('Term')).toHaveValue('first');
    expect(screen.getByLabelText('Starts')).toHaveValue('2027-09-18'); // still a Saturday
    expect(screen.getByLabelText('Ends')).toHaveValue('2028-01-14');
    expect(screen.getByLabelText('Weeks')).toHaveValue(17);
  });

  it('sends the dates to the copy endpoint and hands back the result', async () => {
    await i18n.changeLanguage('en');
    const fetchMock = vi.fn().mockResolvedValue(json(201, result));
    vi.stubGlobal('fetch', fetchMock);
    const onDone = renderForm();

    await userEvent.click(screen.getByRole('button', { name: 'Create draft' }));

    await waitFor(() => expect(onDone).toHaveBeenCalled());
    const request = fetchMock.mock.calls[0][0] as Request;
    expect(new URL(request.url).pathname).toBe(`/api/admin/terms/${source.id}/copy`);
    expect(await request.json()).toEqual({ academicYear: 2027, number: 'first', startsOn: '2027-09-18', endsOn: '2028-01-14', weekCount: 17 });
  });

  it('shows the API message when the term already exists', async () => {
    await i18n.changeLanguage('en');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(409, { code: 'conflict', message: 'A first term for 2027 already exists.', details: [] })));
    renderForm();

    await userEvent.click(screen.getByRole('button', { name: 'Create draft' }));

    expect(await screen.findByText('A first term for 2027 already exists.')).toBeInTheDocument();
  });

  it('reports what was copied and what was left out, each in one language', async () => {
    await i18n.changeLanguage('en');
    render(<CopyTermResult result={result} onClose={() => {}} />);

    expect(screen.getByText('First 2027 created as a draft.')).toBeInTheDocument();
    expect(screen.getByText('Copied 42 groups, 38 teaching assignments and 120 timetable slots.')).toBeInTheDocument();
    expect(screen.getByText('Not copied (2)')).toBeInTheDocument();
    expect(screen.getByText('The hall is inactive.')).toBeInTheDocument();
    expect(screen.getByText('The subject is inactive.')).toBeInTheDocument();
    expect(screen.queryByText(/[؀-ۿ]/)).toBeNull();
  });

  it('says so when nothing was left out', async () => {
    await i18n.changeLanguage('en');
    render(<CopyTermResult result={{ ...result, skipped: [] }} onClose={() => {}} />);

    expect(screen.getByText('Everything was copied.')).toBeInTheDocument();
  });

  it('reports in Arabic with no English words', async () => {
    await i18n.changeLanguage('ar');
    render(<CopyTermResult result={result} onClose={() => {}} />);

    expect(screen.getByText('تم نسخ 42 مجموعة و38 تكليف تدريس و120 محاضرة في الجدول.')).toBeInTheDocument();
    expect(screen.getByText('القاعة موقوفة.')).toBeInTheDocument();
    expect(screen.queryByText(/[A-Za-z]{3,}/, { ignore: '.ltr-nums' })).toBeNull();
  });
});
