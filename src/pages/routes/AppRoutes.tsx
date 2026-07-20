import { BrowserRouter, Navigate, Routes, Route } from "react-router-dom";

import { ErrorBoundary } from "@/components/common/ErrorBoundary";
import { CookieConsentBanner } from "@/components/common/CookieConsentBanner";
import { CookieSettingsDialog } from "@/components/common/CookieSettingsDialog";
import { GoogleAnalytics } from "@/components/common/GoogleAnalytics";
import { ScrollToTop } from "@/components/common/ScrollToTop";
import { OperatorAppearanceProvider } from "@/components/theme/OperatorAppearanceProvider";
import MainLayout from "../../layouts/MainLayout";
import ProtectedRoute from "./ProtectedRoute";
import PublicOnlyRoute from "./PublicOnlyRoute";
import RoleRoute from "./RoleRoute";
import SetupAccountPage from "../auth/SetupAccountPage";

import HomePage from "../public/HomePage";
import LoginPage from "../auth/LoginPage";
import RegisterSinglePage from "../auth/RegisterSinglePage";
import RegisterMultiPage from "../auth/RegisterMultiPage";
import ResetPasswordPage from "../auth/ResetPasswordPage";
import ForgotPasswordPage from "../auth/ForgotPasswordPage";
import GuestFeedbackPage from "../public/GuestFeedbackPage";
import PrivacyPage from "../public/PrivacyPage";
import TermsPage from "../public/TermsPage";
import CookiePolicyPage from "../public/CookiePolicyPage";
import NotFoundPage from "../public/NotFoundPage";
import OperatorHomeResponsivePrototype from "../prototype/OperatorHomeResponsivePrototype";
import OperatorHomeResponsivePrototypeFrame from "../prototype/OperatorHomeResponsivePrototypeFrame";

import Dashboard from "../../components/dashboard/multi/Dashboard";
import AdminDashboard from "../../components/dashboard/admin/Dashboard";
import SupportDashboard from "../../components/dashboard/support/Dashboard";
import SupportQueryDetailPage from "../../components/dashboard/support/SupportQueryDetailPage";
import SingleDashboard from "../../components/dashboard/single/Dashboard";
import HelpCentreHubPage from "../public/HelpCentreHubPage";
import HelpCentreArticlePage from "../public/HelpCentreArticlePage";
import HelpCentreContactPage from "../public/HelpCentreContactPage";
import HelpCentreContactSuccessPage from "../public/HelpCentreContactSuccessPage";
import MyQueriesPage from "../public/MyQueriesPage";
import MyQueryThreadPage from "../public/MyQueryThreadPage";
import { HELP_CENTRE_ROUTES, SUPPORT_DASHBOARD_ROUTES } from "@/config/support";

function AppRoutes() {
  return (
    <BrowserRouter>
      <OperatorAppearanceProvider>
        <ScrollToTop />
        <GoogleAnalytics />
        <CookieConsentBanner />
        <CookieSettingsDialog />
        <Routes>
        {/* Full-viewport auth flows — no site navbar, wrapped in ErrorBoundary */}
        <Route
          path="login"
          element={
            <ErrorBoundary>
              <LoginPage />
            </ErrorBoundary>
          }
        />
        <Route
          path="forgot-password"
          element={
            <ErrorBoundary>
              <ForgotPasswordPage />
            </ErrorBoundary>
          }
        />
        <Route
          path="reset-password"
          element={
            <ErrorBoundary>
              <ResetPasswordPage />
            </ErrorBoundary>
          }
        />
        <Route
          path="setup-account"
          element={
            <ErrorBoundary>
              <SetupAccountPage />
            </ErrorBoundary>
          }
        />
        <Route
          path="setup-account-multi"
          element={
            <ErrorBoundary>
              <RegisterMultiPage />
            </ErrorBoundary>
          }
        />
        <Route
          path="setup-account-single"
          element={
            <ErrorBoundary>
              <RegisterSinglePage />
            </ErrorBoundary>
          }
        />
        <Route
          path="scan/:token"
          element={
            <ErrorBoundary>
              <GuestFeedbackPage />
            </ErrorBoundary>
          }
        />
        <Route
          path="prototype/operator-home-responsive"
          element={
            <ErrorBoundary>
              <OperatorHomeResponsivePrototype />
            </ErrorBoundary>
          }
        />
        <Route
          path="prototype/operator-home-responsive/frame"
          element={
            <ErrorBoundary>
              <OperatorHomeResponsivePrototypeFrame />
            </ErrorBoundary>
          }
        />
        <Route path="/" element={<MainLayout />}>
          <Route element={<PublicOnlyRoute />}>
            <Route index element={<HomePage />} />
            <Route
              path="request-trial"
              element={<Navigate to="/#request-trial" replace />}
            />

            <Route path="register/single" element={<RegisterSinglePage />} />
            <Route path="register/multi" element={<RegisterMultiPage />} />
          </Route>

          <Route element={<ProtectedRoute />}>
            <Route path="single-dashboard" element={<SingleDashboard />} />
            <Route path="multi-dashboard" element={<Dashboard />} />
            <Route
              path="admin-dashboard"
              element={
                <RoleRoute role="ADMIN">
                  <AdminDashboard />
                </RoleRoute>
              }
            />
            <Route
              path={SUPPORT_DASHBOARD_ROUTES.inbox}
              element={
                <RoleRoute role="SUPPORT">
                  <SupportDashboard />
                </RoleRoute>
              }
            />
            <Route
              path={SUPPORT_DASHBOARD_ROUTES.query}
              element={
                <RoleRoute role="SUPPORT">
                  <SupportQueryDetailPage />
                </RoleRoute>
              }
            />
            <Route
              path={HELP_CENTRE_ROUTES.myQueries}
              element={
                <RoleRoute role="USER">
                  <MyQueriesPage />
                </RoleRoute>
              }
            />
            <Route
              path={HELP_CENTRE_ROUTES.myQuery}
              element={
                <RoleRoute role="USER">
                  <MyQueryThreadPage />
                </RoleRoute>
              }
            />
          </Route>

          <Route path="privacy" element={<PrivacyPage />} />
          <Route path="terms" element={<TermsPage />} />
          <Route path="cookie-policy" element={<CookiePolicyPage />} />

          <Route path={HELP_CENTRE_ROUTES.hub} element={<HelpCentreHubPage />} />
          <Route
            path={HELP_CENTRE_ROUTES.article}
            element={<HelpCentreArticlePage />}
          />
          <Route path={HELP_CENTRE_ROUTES.contact} element={<HelpCentreContactPage />} />
          <Route
            path={HELP_CENTRE_ROUTES.contactSuccess}
            element={<HelpCentreContactSuccessPage />}
          />

          <Route path="*" element={<NotFoundPage />} />
        </Route>

        <Route element={<MainLayout />}>
          <Route path="*" element={<NotFoundPage />} />
        </Route>
        </Routes>
      </OperatorAppearanceProvider>
    </BrowserRouter>
  );
}

export default AppRoutes;
