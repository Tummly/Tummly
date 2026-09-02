import { BrowserRouter, Navigate, Routes, Route } from "react-router-dom";

import { ErrorBoundary } from "@/components/common/ErrorBoundary";
import { CookieConsentBanner } from "@/components/common/CookieConsentBanner";
import { CookieSettingsDialog } from "@/components/common/CookieSettingsDialog";
import { GoogleAnalytics } from "@/components/common/GoogleAnalytics";
import { ScrollToTop } from "@/components/common/ScrollToTop";
import { OperatorAppearanceProvider } from "@/components/theme/OperatorAppearanceProvider";
import MainLayout from "../../layouts/MainLayout";
import OperatorDashboardRoute from "./OperatorDashboardRoute";
import ProtectedRoute from "./ProtectedRoute";
import PublicOnlyRoute from "./PublicOnlyRoute";
import RoleRoute from "./RoleRoute";
import SetupAccountPage from "../auth/SetupAccountPage";
import TeamInvitationAcceptPage from "../auth/TeamInvitationAcceptPage";

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
import { Dashboard as OperatorDashboard } from "@/components/dashboard/operator/Dashboard";
import { CaptureNestedRoute } from "@/components/dashboard/operator/Capture/CaptureNestedRoute";
import { CaptureArchiveRoute } from "@/components/dashboard/operator/Capture/CaptureArchiveRoute";
import { CaptureMultiRootRoute } from "@/components/dashboard/operator/Capture/CaptureMultiRootRoute";
import { CaptureSingleRoute } from "@/components/dashboard/operator/Capture/CaptureSingleRoute";
import { GuestsRoute } from "@/components/dashboard/operator/Guests/GuestsRoute";
import { CampaignsRoute } from "@/components/dashboard/operator/Campaigns/CampaignsRoute";
import { FeedbackRoute } from "@/components/dashboard/operator/Feedback/FeedbackRoute";
import { OffersRoute } from "@/components/dashboard/operator/Offers/OffersRoute"
import { OfferDetailsPageModuleProvider } from "@/components/dashboard/operator/Offers/OfferDetailsPageModuleProvider"
import { OfferDetailsRoute } from "@/components/dashboard/operator/Offers/OfferDetailsRoute"
import { OfferGuestPreviewRoute } from "@/components/dashboard/operator/Offers/OfferGuestPreviewRoute"
import { CampaignDetailsRoute } from "@/components/dashboard/operator/Campaigns/CampaignDetailsRoute"
import { CampaignGuestPreviewRoute } from "@/components/dashboard/operator/Campaigns/CampaignGuestPreviewRoute"
import { OffersRedemptionLogPageModuleProvider } from "@/components/dashboard/operator/Offers/OffersRedemptionLogPageModuleProvider"
import { OffersRedemptionLogRoute } from "@/components/dashboard/operator/Offers/OffersRedemptionLogRoute";
import { GuestEditRoute } from "@/components/dashboard/operator/GuestProfile/GuestEditRoute";
import { GuestProfilePageModuleProvider } from "@/components/dashboard/operator/GuestProfile/GuestProfilePageModuleProvider";
import { GuestProfileRoute } from "@/components/dashboard/operator/GuestProfile/GuestProfileRoute";
import { HomeRoute } from "@/components/dashboard/operator/Home/HomeRoute";
import { AccountWorkspacePageModuleProvider } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspacePageModuleProvider";
import { AccountWorkspaceRoute } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceRoute";
import { TeamPermissionsPageModuleProvider } from "@/components/dashboard/operator/TeamPermissions/TeamPermissionsPageModuleProvider";
import { TeamPermissionsRoute } from "@/components/dashboard/operator/TeamPermissions/TeamPermissionsRoute";
import { ShopRoute } from "@/components/dashboard/operator/Shop/ShopRoute";
import { ReportsRoute } from "@/components/dashboard/operator/Reports/ReportsRoute";
import AdminDashboard from "../../components/dashboard/admin/Dashboard";
import SupportDashboard from "../../components/dashboard/support/Dashboard";
import SupportQueryDetailPage from "../../components/dashboard/support/SupportQueryDetailPage";
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
          path="start"
          element={
            <ErrorBoundary>
              <TeamInvitationAcceptPage />
            </ErrorBoundary>
          }
        />
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
            <Route element={<OperatorDashboardRoute mode="single" />}>
              <Route path="single-dashboard" element={<OperatorDashboard mode="single" />}>
                <Route index element={<HomeRoute />} />
                <Route path="guests" element={<GuestsRoute />} />
                <Route
                  path="guests/:guestId"
                  element={<GuestProfilePageModuleProvider />}
                >
                  <Route index element={<GuestProfileRoute />} />
                  <Route path="edit" element={<GuestEditRoute />} />
                </Route>
                <Route path="capture" element={<CaptureSingleRoute />} />
                <Route
                  path="capture/archive"
                  element={<CaptureArchiveRoute mode="single" />}
                />
                <Route path="feedback" element={<FeedbackRoute />} />
                <Route path="campaigns" element={<CampaignsRoute />} />
                <Route
                  path="campaigns/:campaignId"
                  element={<CampaignDetailsRoute />}
                />
                <Route
                  path="campaigns/:campaignId/preview"
                  element={<CampaignGuestPreviewRoute />}
                />
                <Route path="offers" element={<OffersRoute />} />
                <Route
                  path="offers/redemption-log"
                  element={
                    <OffersRedemptionLogPageModuleProvider>
                      <OffersRedemptionLogRoute />
                    </OffersRedemptionLogPageModuleProvider>
                  }
                />
                <Route
                  path="offers/:offerId/preview"
                  element={<OfferGuestPreviewRoute />}
                />
                <Route
                  path="offers/:offerId"
                  element={
                    <OfferDetailsPageModuleProvider>
                      <OfferDetailsRoute />
                    </OfferDetailsPageModuleProvider>
                  }
                />
                <Route
                  path="settings/account-workspace"
                  element={
                    <AccountWorkspacePageModuleProvider>
                      <AccountWorkspaceRoute />
                    </AccountWorkspacePageModuleProvider>
                  }
                />
                <Route
                  path="settings/team-permissions"
                  element={
                    <TeamPermissionsPageModuleProvider>
                      <TeamPermissionsRoute />
                    </TeamPermissionsPageModuleProvider>
                  }
                />
                <Route path="reports" element={<ReportsRoute />} />
                <Route path="shop" element={<ShopRoute />} />
              </Route>
            </Route>
            <Route element={<OperatorDashboardRoute mode="multi" />}>
              <Route path="multi-dashboard" element={<OperatorDashboard mode="multi" />}>
                <Route index element={<HomeRoute />} />
                <Route path="guests" element={<GuestsRoute />} />
                <Route
                  path="guests/:guestId"
                  element={<GuestProfilePageModuleProvider />}
                >
                  <Route index element={<GuestProfileRoute />} />
                  <Route path="edit" element={<GuestEditRoute />} />
                </Route>
                <Route path="capture" element={<CaptureMultiRootRoute />} />
                <Route
                  path="capture/archive"
                  element={<CaptureArchiveRoute mode="multi" />}
                />
                <Route
                  path="capture/locations/:locationId"
                  element={<CaptureNestedRoute />}
                />
                <Route path="feedback" element={<FeedbackRoute />} />
                <Route path="campaigns" element={<CampaignsRoute />} />
                <Route
                  path="campaigns/:campaignId"
                  element={<CampaignDetailsRoute />}
                />
                <Route
                  path="campaigns/:campaignId/preview"
                  element={<CampaignGuestPreviewRoute />}
                />
                <Route path="offers" element={<OffersRoute />} />
                <Route
                  path="offers/redemption-log"
                  element={
                    <OffersRedemptionLogPageModuleProvider>
                      <OffersRedemptionLogRoute />
                    </OffersRedemptionLogPageModuleProvider>
                  }
                />
                <Route
                  path="offers/:offerId/preview"
                  element={<OfferGuestPreviewRoute />}
                />
                <Route
                  path="offers/:offerId"
                  element={
                    <OfferDetailsPageModuleProvider>
                      <OfferDetailsRoute />
                    </OfferDetailsPageModuleProvider>
                  }
                />
                <Route
                  path="settings/account-workspace"
                  element={
                    <AccountWorkspacePageModuleProvider>
                      <AccountWorkspaceRoute />
                    </AccountWorkspacePageModuleProvider>
                  }
                />
                <Route
                  path="settings/team-permissions"
                  element={
                    <TeamPermissionsPageModuleProvider>
                      <TeamPermissionsRoute />
                    </TeamPermissionsPageModuleProvider>
                  }
                />
                <Route path="reports" element={<ReportsRoute />} />
                <Route path="shop" element={<ShopRoute />} />
              </Route>
            </Route>
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
