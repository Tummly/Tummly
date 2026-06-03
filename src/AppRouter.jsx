import { BrowserRouter, Routes, Route } from "react-router-dom";

// Public Pages
import HomePage from "./public/HomePage";
import AboutPage from "./public/AboutPage";
import ServicesPage from "./public/ServicesPage";
import FeaturesPage from "./public/FeaturesPage";
import SetupPage from "./public/SetupPage";
import GuidedTrialPage from "./public/GuidedTrialPage";
import AccessPage from "./public/AccessPage";
import FaqPage from "./public/FaqPage";
import ContactPage from "./public/ContactPage";

// Auth Pages
import LoginPage from "./auth/LoginPage";
import RegisterChoicePage from "./auth/RegisterChoicePage";
import RegisterSinglePage from "./auth/RegisterSinglePage";
import RegisterMultiPage from "./auth/RegisterMultiPage";
import VerifyEmailPage from "./auth/VerifyEmailPage";
import ForgotPasswordPage from "./auth/ForgotPasswordPage";

// Layouts
import MainLayout from "../../layouts/MainLayout";

// Routes Protection
import ProtectedRoute from "./ProtectedRoute";
import AdminRoute from "./AdminRoute";

// Dashboard Pages
// SINGLE
// import SingleDashboardHome from "../dashboard/single/SingleDashboardHome";

// MULTI
// import MultiDashboardHome from "../dashboard/multi/MultiDashboardHome";

// ADMIN
// import AdminDashboardHome from "../dashboard/admin/AdminDashboardHome";

const AppRoutes = () => {
    return (
        <BrowserRouter>
            <Routes>

                {/* ================= PUBLIC ROUTES ================= */}
                <Route path="/" element={<MainLayout />}>

                    <Route index element={<HomePage />} />

                    <Route path="about" element={<AboutPage />} />

                    <Route path="services" element={<ServicesPage />} />

                    <Route path="features" element={<FeaturesPage />} />

                    <Route path="setup" element={<SetupPage />} />

                    <Route path="guided-trial" element={<GuidedTrialPage />} />

                    <Route path="access" element={<AccessPage />} />

                    <Route path="faqs" element={<FaqPage />} />

                    <Route path="contact" element={<ContactPage />} />

            
                </Route>


                {/* ================= AUTH ROUTES ================= */}

                <Route path="/login" element={<LoginPage />} />

                <Route
                    path="/register"
                    element={<RegisterChoicePage />}
                />

                <Route
                    path="/register/single"
                    element={<RegisterSinglePage />}
                />

                <Route
                    path="/register/multi"
                    element={<RegisterMultiPage />}
                />

                <Route
                    path="/verify-email"
                    element={<VerifyEmailPage />}
                />

                <Route
                    path="/forgot-password"
                    element={<ForgotPasswordPage />}
                />


                {/* ================= SINGLE DASHBOARD ================= */}

                <Route
                    path="/single-dashboard"
                    element={
                        <ProtectedRoute role="single">
                            {/* <SingleDashboardHome /> */}
                            <h1>Single Dashboard</h1>
                        </ProtectedRoute>
                    }
                />


                {/* ================= MULTI DASHBOARD ================= */}

                <Route
                    path="/multi-dashboard"
                    element={
                        <ProtectedRoute role="multi">
                            {/* <MultiDashboardHome /> */}
                            <h1>Multi Dashboard</h1>
                        </ProtectedRoute>
                    }
                />


                {/* ================= ADMIN DASHBOARD ================= */}

                <Route
                    path="/admin-dashboard"
                    element={
                        <AdminRoute>
                            {/* <AdminDashboardHome /> */}
                            <h1>Admin Dashboard</h1>
                        </AdminRoute>
                    }
                />


                {/* ================= 404 PAGE ================= */}

                <Route
                    path="*"
                    element={
                        <h1 style={{ textAlign: "center", marginTop: "100px" }}>
                            404 Page Not Found
                        </h1>
                    }
                />

            </Routes>
        </BrowserRouter>
    );
};

export default AppRoutes;