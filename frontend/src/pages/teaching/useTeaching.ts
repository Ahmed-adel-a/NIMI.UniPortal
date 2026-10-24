import { useQuery } from '@tanstack/react-query';
import { api, call } from '../../api/client';
import { addDays, isoDate, startOfWeek } from '../../lib/format';

/** The groups the caller teaches this term. */
export function useMyClasses() {
  return useQuery({ queryKey: ['teaching', 'classes'], queryFn: () => call(api.GET('/api/teaching/classes')) });
}

/** The caller's lectures in one Saturday-based week. */
export function useTeachingWeek(weekStart: Date = startOfWeek(new Date())) {
  const from = isoDate(weekStart);
  const to = isoDate(addDays(weekStart, 6));
  return useQuery({
    queryKey: ['teaching', 'schedule', from, to],
    queryFn: () => call(api.GET('/api/teaching/schedule', { params: { query: { from, to } } })),
  });
}
