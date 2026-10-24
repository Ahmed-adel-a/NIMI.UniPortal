import { useQuery } from '@tanstack/react-query';
import { api, call } from '../../api/client';

/** The student's subjects and groups this term (either academic system). */
export function useMyCourses() {
  return useQuery({ queryKey: ['courses', 'mine'], queryFn: () => call(api.GET('/api/courses/mine')) });
}
