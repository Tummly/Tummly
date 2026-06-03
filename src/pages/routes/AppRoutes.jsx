import { BrowserRouter, Routes, Route } from "react-router-dom";

import MainLayout from "../../layouts/MainLayout";
import SetupAccountPage from "../auth/SetupAccountPage";

import HomePage from "../public/HomePage";
import LoginPage from "../auth/LoginPage";
import RegisterSinglePage from "../auth/RegisterSinglePage";
import RegisterMultiPage from "../auth/RegisterMultiPage";

import Dashboard from "../../components/dashboard/multi/Dashboard";
import AdminDashboard
from "../../components/dashboard/admin/Dashboard";
import Hero from "../../components/home/Hero";
import SingleDashboard from "../../components/dashboard/single/Dashboard";


function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<MainLayout />}>
          <Route index element={<HomePage />} />

          <Route
  path="request-trial"
  element={<Hero />}
/>
<Route
  path="single-dashboard"
  element={<SingleDashboard />}
/>

          {/* Auth Routes */}
          <Route
            path="register/single"
            element={<RegisterSinglePage />}
          />

          <Route
            path="register/multi"
            element={<RegisterMultiPage />}
          />

          <Route
            path="login"
            element={<LoginPage />}
          />

          {/* Multi Dashboard */}
          <Route
  path="multi-dashboard"
  element={<Dashboard />}
/>

<Route
  path="setup-account"
  element={<SetupAccountPage />}
/>  

<Route
  path="setup-account-multi"
  element={<RegisterMultiPage />}
/>

<Route
  path="setup-account-single"
  element={<RegisterSinglePage />}
/>

<Route
  path="admin-dashboard"
  element={<AdminDashboard />}
/>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default AppRoutes;