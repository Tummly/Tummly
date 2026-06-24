import { BrowserRouter, Navigate, Routes, Route } from "react-router-dom";

import { ErrorBoundary } from "@/components/common/ErrorBoundary";
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

import Dashboard from "../../components/dashboard/multi/Dashboard";
import AdminDashboard from "../../components/dashboard/admin/Dashboard";
import SingleDashboard from "../../components/dashboard/single/Dashboard";

function AppRoutes() {
  return (
    <BrowserRouter>
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
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;
