import type {
  OperatorSidebarActiveId,
  OperatorSidebarNavModel,
} from "@/lib/operatorHome/sidebarNav";
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod";

export type { OperatorSidebarActiveId, OperatorSidebarNavModel };
export type { OperatorSidebarNavId } from "@/lib/operatorHome/sidebarNav";

/**
 * Client-facing Operator Home body contract for the selected Owned location.
 * Shell chrome (locations, profile, nav) comes from Operator workspace session /
 * shell presentation — not this view-model.
 */
export interface OperatorHomeLocationOption {
  id: number;
  name: string;
  /** Freeform address for the Owned location; empty when unset. */
  address: string;
}

export type OperatorHomeSetupStepId =
  | "account-ready"
  | "upload-logo"
  | "guest-form"
  | "first-response"
  | "qr-placement"
  | "first-offer"
  | "first-campaign";

export type OperatorHomeSetupStepStatus = "complete" | "partial" | "incomplete";

export type OperatorHomeSetupAction = {
  id: string;
  label: string;
  available: boolean;
};

export type OperatorHomeSetupStep = {
  id: OperatorHomeSetupStepId;
  stepNumber: 1 | 2 | 3 | 4 | 5 | 6 | 7;
  title: string;
  description: string;
  status: OperatorHomeSetupStepStatus;
  actions: OperatorHomeSetupAction[];
};

export type OperatorHomeKpiId =
  | "qr-scans"
  | "feedback"
  | "guests-joined"
  | "offer-redemptions";

export type OperatorHomeKpi = {
  id: OperatorHomeKpiId;
  label: string;
  /** Honest zero when empty; real total when available. */
  value: number;
  /** Period-over-period change; null shows placeholder until API provides a value. */
  trendPercent: number | null;
  /** False when the metric has no backing service yet (still shows honest zero). */
  hasRealData: boolean;
};

export type OperatorHomeActivityTabId =
  | "all"
  | "feedback"
  | "guests"
  | "offers"
  | "campaigns";

export type OperatorHomeActivityItem =
  | {
      id: string;
      kind: "feedback";
      /** Backend Feedback id for Latest activity Feedback details. */
      feedbackId: number;
      /** Location Guest to open from View guest; null when unlinked. */
      locationGuestId: number | null;
      comment: string;
      guestName: string;
      createdAt: string;
      /** Omitted in UI when null — no invented sentiment. */
      sentiment: "positive" | "neutral" | "negative" | null;
      canViewFeedback: boolean;
      canViewGuest: boolean;
    }
  | {
      id: string;
      kind: "guest-joined";
      locationGuestId: number;
      guestName: string;
      initials: string;
      headline: string;
      joinSourceLabel: "From QR scan";
      consentLabel: "Opted in" | "Opted out";
      createdAt: string;
      canViewGuest: boolean;
      canSendOffer: false;
    };

export type OperatorHomeActivityEmpty = {
  emptyCopy: string;
  emptyHelper: string;
};

export type OperatorHomeChecklistAcks = {
  guestFormPreviewed: boolean;
  qrPlacementGuideViewed: boolean;
  logoUploaded: boolean;
};

export interface OperatorHomeViewModel {
  selectedLocationId: number;
  selectedLocationName: string;
  /** Smart Guest Link for the selected Owned location; null when unavailable. */
  smartGuestLink: string | null;
  /** Copy Smart Guest Link when a Smart Guest Link exists. */
  canCopySmartGuestLink: boolean;
  /** Preview guest form when a Smart Guest Link exists. */
  canPreviewGuestForm: boolean;
  /** Display-only reporting window; does not imply false filtering. */
  dateRangeLabel: string;
  setupSteps: OperatorHomeSetupStep[];
  kpis: OperatorHomeKpi[];
  activityByTab: Record<OperatorHomeActivityTabId, OperatorHomeActivityItem[]>;
  activityEmpty: OperatorHomeActivityEmpty;
}

export interface OperatorShellPresentation {
  /** Activation period badge copy; null hides the badge. Home hero renders it. */
  activationPeriodBadge: ActivationPeriodBadgeCopy | null;
  profileDisplayName: string;
  profileFirstName: string;
  profileInitials: string;
  /** Normalized Self role under the account trigger; null omits subtitle. */
  profileSelfRoleSubtitle: string | null;
  /** Deferred Figma chrome that must not be rendered. */
  omittedNavbarControls: ReadonlyArray<
    "search" | "ai-copilot" | "help" | "notifications"
  >;
  sidebarNav: OperatorSidebarNavModel;
  locationSwitcher: {
    interactive: boolean;
    selectedLocationId: number;
    selectedLocationName: string;
    options: OperatorHomeLocationOption[];
  };
}
