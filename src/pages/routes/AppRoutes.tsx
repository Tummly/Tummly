import { BrowserRouter, Routes, Route } from "react-router-dom";

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

import Dashboard from "../../components/dashboard/multi/Dashboard";
import AdminDashboard from "../../components/dashboard/admin/Dashboard";
import Hero from "../../components/home/Hero";
import SingleDashboard from "../../components/dashboard/single/Dashboard";

function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Full-viewport auth flows — no site navbar */}
        <Route path="login" element={<LoginPage />} />
        <Route path="forgot-password" element={<ForgotPasswordPage />} />
        <Route path="reset-password" element={<ResetPasswordPage />} />
        <Route path="setup-account" element={<SetupAccountPage />} />
        <Route path="setup-account-multi" element={<RegisterMultiPage />} />
        <Route path="setup-account-single" element={<RegisterSinglePage />} />

        <Route path="/" element={<MainLayout />}>
          <Route element={<PublicOnlyRoute />}>
            <Route index element={<HomePage />} />

            <Route path="request-trial" element={<Hero />} />

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
