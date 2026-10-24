import { useQuery } from '@tanstack/react-query';
import { api, call } from '../../api/client';

/** Lists the admin forms pick from; cached for a few minutes since they rarely change. */
const lookup = { staleTime: 5 * 60_000 };

export function useFaculties() {
  return useQuery({ queryKey: ['lookup', 'faculties'], queryFn: () => call(api.GET('/api/faculties', { params: { query: { includeInactive: true, pageSize: 100 } } })), ...lookup });
}

export function useDepartments() {
  return useQuery({ queryKey: ['lookup', 'departments'], queryFn: () => call(api.GET('/api/departments', { params: { query: { includeInactive: true, pageSize: 100 } } })), ...lookup });
}

export function useTerms() {
  return useQuery({ queryKey: ['lookup', 'terms'], queryFn: () => call(api.GET('/api/terms', { params: { query: { pageSize: 100 } } })), ...lookup });
}

/** The active term, or null when none is active (a 404 from the API). */
export function useCurrentTerm() {
  return useQuery({ queryKey: ['lookup', 'current-term'], queryFn: () => call(api.GET('/api/terms/current')).catch(() => null), ...lookup });
}

/** Professors and department heads (both can teach). */
export function useStaff() {
  return useQuery({
    queryKey: ['lookup', 'staff'],
    queryFn: async () => {
      const [professors, heads] = await Promise.all([
        call(api.GET('/api/admin/users', { params: { query: { role: 'professor', isActive: true, pageSize: 100 } } })),
        call(api.GET('/api/admin/users', { params: { query: { role: 'department_head', isActive: true, pageSize: 100 } } })),
      ]);
      return [...professors.items, ...heads.items].sort((a, b) => a.fullName.localeCompare(b.fullName));
    },
    ...lookup,
  });
}
