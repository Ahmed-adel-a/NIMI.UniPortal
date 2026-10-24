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
import { HeadReportsPage } from './pages/head/HeadReportsPage';
import { ClassPage } from './pages/teaching/ClassPage';
import { TeachingHomePage } from './pages/teaching/TeachingHomePage';

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
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
