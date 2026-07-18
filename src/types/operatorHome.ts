import type { OperatorSidebarNavId } from "@/lib/operatorHome/sidebarNav";
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod";

export type { OperatorSidebarNavId };

/**
 * Client-facing Operator Home body contract for the selected Owned location.
 * Shell chrome (locations, profile, nav) comes from Operator workspace session /
 * shell presentation — not this view-model.
 */
export interface OperatorHomeLocationOption {
  id: number;
  name: string;
}

export type OperatorHomeSetupStepId =
  | "account-ready"
  | "guest-form"
  | "qr-placement"
  | "first-response"
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
  stepNumber: 1 | 2 | 3 | 4 | 5 | 6;
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
  /** Only set when a real period-over-period signal exists. */
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

export type OperatorHomeActivityItem = {
  id: string;
  kind: "feedback";
  /** Backend Feedback id for Latest activity Feedback details. */
  feedbackId: number;
  comment: string;
  guestName: string;
  createdAt: string;
  /** Omitted in UI when null — no invented sentiment. */
  sentiment: "positive" | "neutral" | "negative" | null;
  canViewFeedback: boolean;
  canViewGuest: boolean;
};

export type OperatorHomeActivityEmpty = {
  emptyCopy: string;
  emptyHelper: string;
};

export type OperatorHomeChecklistAcks = {
  guestFormPreviewed: boolean;
  qrPlacementGuideViewed: boolean;
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
  /** Deferred Figma chrome that must not be rendered. */
  omittedNavbarControls: ReadonlyArray<
    "search" | "ai-copilot" | "help" | "notifications"
  >;
  sidebarNav: Array<{
    id: OperatorSidebarNavId;
    label: string;
    navigable: boolean;
    active: boolean;
  }>;
  locationSwitcher: {
    interactive: boolean;
    selectedLocationId: number;
    selectedLocationName: string;
    options: OperatorHomeLocationOption[];
  };
  pageTitle: string;
}
