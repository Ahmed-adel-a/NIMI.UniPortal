import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../api/client';
import { useAuth } from '../auth/useAuth';
import { navigationFor } from './navigation';
import { applyTheme, savedTheme, type Theme } from './theme';

/** The signed-in frame: a sidebar on the reading side (right in Arabic), a top bar, and the page. */
export function AppShell() {
  const { t, i18n } = useTranslation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [theme, setTheme] = useState<Theme>(savedTheme);

  const unread = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: () => call(api.GET('/api/notifications/mine/unread-count')),
    refetchInterval: 60_000,
    enabled: !!user,
  });

  const items = navigationFor(user?.role ?? '', user?.academicSystem);
  const nextTheme: Record<Theme, Theme> = { system: 'light', light: 'dark', dark: 'system' };
  const themeLabel = { system: t('nav.themeSystem'), light: t('nav.themeLight'), dark: t('nav.themeDark') }[theme];

  const sidebar = (
    <nav aria-label={t('nav.menu')} className="grid gap-1 p-3">
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/student' || item.to === '/home'}
          onClick={() => setOpen(false)}
          className={({ isActive }) =>
            `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition ${isActive ? 'bg-primary-soft text-primary' : 'text-fg hover:bg-sunk'}`
          }
        >
          <span aria-hidden="true">{item.icon}</span>
          <span className="flex-1">{t(item.label)}</span>
          {item.to === '/notifications' && !!unread.data?.count && (
            <span className="rounded-full bg-danger px-2 text-xs font-bold text-white ltr-nums">{unread.data.count}</span>
          )}
        </NavLink>
      ))}
    </nav>
  );

  return (
    <div className="min-h-dvh">
      <header className="sticky top-0 z-20 border-b border-line bg-surface/95 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-7xl items-center gap-3 px-4">
          <button className="rounded-lg p-2 hover:bg-sunk lg:hidden" aria-label={t('nav.menu')} aria-expanded={open} onClick={() => setOpen(!open)}>
            ☰
          </button>
          <span className="font-bold text-primary">{t('app.name')}</span>
          <div className="ms-auto flex items-center gap-1 sm:gap-2">
            <button
              className="rounded-lg px-2 py-1.5 text-sm hover:bg-sunk"
              onClick={() => void i18n.changeLanguage(i18n.language === 'ar' ? 'en' : 'ar')}
            >
              {t('nav.language')}
            </button>
            <button
              className="rounded-lg px-2 py-1.5 text-sm hover:bg-sunk"
              title={t('nav.theme')}
              onClick={() => {
                const value = nextTheme[theme];
                setTheme(value);
                applyTheme(value);
              }}
            >
              {theme === 'dark' ? '🌙' : theme === 'light' ? '☀️' : '🖥'} <span className="hidden sm:inline">{themeLabel}</span>
            </button>
            <div className="hidden text-end sm:block">
              <p className="text-sm font-semibold leading-tight">{user?.fullName}</p>
              <p className="text-xs text-muted">{user ? t(`roles.${user.role}`) : ''}</p>
            </div>
            <button
              className="rounded-lg px-3 py-1.5 text-sm font-medium text-danger hover:bg-danger-soft"
              onClick={async () => {
                await logout();
                navigate('/login', { replace: true });
              }}
            >
              {t('auth.logout')}
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto flex max-w-7xl gap-6 px-4">
        <aside className="sticky top-14 hidden h-[calc(100dvh-3.5rem)] w-60 shrink-0 overflow-y-auto border-e border-line lg:block">{sidebar}</aside>
        {open && (
          <div className="fixed inset-0 z-30 lg:hidden" onClick={() => setOpen(false)}>
            <div className="absolute inset-0 bg-black/40" />
            <aside className="absolute inset-y-0 start-0 w-72 max-w-[85vw] overflow-y-auto bg-surface shadow-xl" onClick={(e) => e.stopPropagation()}>
              {sidebar}
            </aside>
          </div>
        )}
        <main className="min-w-0 flex-1 py-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
