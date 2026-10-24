import { lazy } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/useAuth';
import { RequireAuth } from './auth/RequireAuth';
import { homeOf } from './auth/roles';
import { FullPageSpinner } from './components/ui/Spinner';
import { AppShell } from './layout/AppShell';
import { ChangePasswordPage } from './pages/auth/ChangePasswordPage';
import { ForgotPasswordPage } from './pages/auth/ForgotPasswordPage';
import { LoginPage } from './pages/auth/LoginPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
import { HomePage } from './pages/common/HomePage';
import { NotificationsPage } from './pages/common/NotificationsPage';
import { ProfilePage } from './pages/common/ProfilePage';
import { AssignmentsPage } from './pages/student/AssignmentsPage';
import { AttendancePage } from './pages/student/AttendancePage';
import { CoursesPage } from './pages/student/CoursesPage';
import { DashboardPage } from './pages/student/DashboardPage';
import { FeesPage } from './pages/student/FeesPage';
import { GradesPage } from './pages/student/GradesPage';
import { RegistrationPage } from './pages/student/RegistrationPage';
import { ReportsPage } from './pages/student/ReportsPage';
import { SchedulePage } from './pages/student/SchedulePage';
import { StandingPage } from './pages/student/StandingPage';

// Staff pages load on first use, so students never download them.
const AdminDashboardPage = lazy(() => import('./pages/admin/AdminDashboardPage').then((m) => ({ default: m.AdminDashboardPage })));
const FacilitiesPage = lazy(() => import('./pages/admin/FacilitiesPage').then((m) => ({ default: m.FacilitiesPage })));
const GradeApprovalPage = lazy(() => import('./pages/admin/GradeApprovalPage').then((m) => ({ default: m.GradeApprovalPage })));
const OrganizationPage = lazy(() => import('./pages/admin/OrganizationPage').then((m) => ({ default: m.OrganizationPage })));
const ActivityPage = lazy(() => import('./pages/admin/OversightPages').then((m) => ({ default: m.ActivityPage })));
const AdminReportsPage = lazy(() => import('./pages/admin/OversightPages').then((m) => ({ default: m.AdminReportsPage })));
const DevicesPage = lazy(() => import('./pages/admin/OversightPages').then((m) => ({ default: m.DevicesPage })));
const PaymentsPage = lazy(() => import('./pages/admin/PaymentsPage').then((m) => ({ default: m.PaymentsPage })));
const SettingsPage = lazy(() => import('./pages/admin/SettingsPage').then((m) => ({ default: m.SettingsPage })));
const SubjectsPage = lazy(() => import('./pages/admin/SubjectsPage').then((m) => ({ default: m.SubjectsPage })));
const TermsPage = lazy(() => import('./pages/admin/TermsPage').then((m) => ({ default: m.TermsPage })));
const TimetablePage = lazy(() => import('./pages/admin/TimetablePage').then((m) => ({ default: m.TimetablePage })));
const UsersPage = lazy(() => import('./pages/admin/UsersPage').then((m) => ({ default: m.UsersPage })));
const HeadReportsPage = lazy(() => import('./pages/head/HeadReportsPage').then((m) => ({ default: m.HeadReportsPage })));
const ClassPage = lazy(() => import('./pages/teaching/ClassPage').then((m) => ({ default: m.ClassPage })));
const TeachingHomePage = lazy(() => import('./pages/teaching/TeachingHomePage').then((m) => ({ default: m.TeachingHomePage })));

/** Sends a signed-in user to their role's home. */
function RoleHome() {
  const { user, loadingUser } = useAuth();
  if (loadingUser || !user) return <FullPageSpinner />;
  return <Navigate to={homeOf(user.role)} replace />;
}

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/change-password" element={<RequireAuth><ChangePasswordPage /></RequireAuth>} />

      <Route element={<RequireAuth><AppShell /></RequireAuth>}>
        <Route index element={<RoleHome />} />
        <Route path="home" element={<HomePage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="notifications" element={<NotificationsPage />} />

        <Route path="student" element={<RequireAuth roles={['student']}><DashboardPage /></RequireAuth>} />
        <Route path="student/schedule" element={<RequireAuth roles={['student']}><SchedulePage /></RequireAuth>} />
        <Route path="student/registration" element={<RequireAuth roles={['student']}><RegistrationPage /></RequireAuth>} />
        <Route path="student/standing" element={<RequireAuth roles={['student']}><StandingPage /></RequireAuth>} />
        <Route path="student/courses" element={<RequireAuth roles={['student']}><CoursesPage /></RequireAuth>} />
        <Route path="student/assignments" element={<RequireAuth roles={['student']}><AssignmentsPage /></RequireAuth>} />
        <Route path="student/grades" element={<RequireAuth roles={['student']}><GradesPage /></RequireAuth>} />
        <Route path="student/attendance" element={<RequireAuth roles={['student']}><AttendancePage /></RequireAuth>} />
        <Route path="student/fees" element={<RequireAuth roles={['student']}><FeesPage /></RequireAuth>} />
        <Route path="student/reports" element={<RequireAuth roles={['student']}><ReportsPage /></RequireAuth>} />

        <Route path="teach" element={<RequireAuth roles={['professor', 'department_head']}><TeachingHomePage /></RequireAuth>} />
        <Route path="teach/classes/:groupId" element={<RequireAuth roles={['professor', 'department_head']}><ClassPage /></RequireAuth>} />
        <Route path="head/reports" element={<RequireAuth roles={['department_head']}><HeadReportsPage /></RequireAuth>} />

        <Route path="admin" element={<RequireAuth roles={['admin']}><AdminDashboardPage /></RequireAuth>} />
        <Route path="admin/users" element={<RequireAuth roles={['admin']}><UsersPage /></RequireAuth>} />
        <Route path="admin/organization" element={<RequireAuth roles={['admin']}><OrganizationPage /></RequireAuth>} />
        <Route path="admin/terms" element={<RequireAuth roles={['admin']}><TermsPage /></RequireAuth>} />
        <Route path="admin/subjects" element={<RequireAuth roles={['admin']}><SubjectsPage /></RequireAuth>} />
        <Route path="admin/facilities" element={<RequireAuth roles={['admin']}><FacilitiesPage /></RequireAuth>} />
        <Route path="admin/timetable" element={<RequireAuth roles={['admin']}><TimetablePage /></RequireAuth>} />
        <Route path="admin/grades" element={<RequireAuth roles={['admin']}><GradeApprovalPage /></RequireAuth>} />
        <Route path="admin/payments" element={<RequireAuth roles={['admin']}><PaymentsPage /></RequireAuth>} />
        <Route path="admin/reports" element={<RequireAuth roles={['admin']}><AdminReportsPage /></RequireAuth>} />
        <Route path="admin/devices" element={<RequireAuth roles={['admin']}><DevicesPage /></RequireAuth>} />
        <Route path="admin/activity" element={<RequireAuth roles={['admin']}><ActivityPage /></RequireAuth>} />
        <Route path="admin/settings" element={<RequireAuth roles={['admin']}><SettingsPage /></RequireAuth>} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
