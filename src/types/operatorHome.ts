import type {
  OperatorSidebarActiveId,
  OperatorSidebarNavModel,
} from "@/lib/operatorHome/sidebarNav";
import type { ActivationPeriodBadgeCopy } from "@/lib/operatorHome/activationPeriod";
import type {
  HomeRecommendationCampaignType,
  HomeRecommendationDomainActionKind,
  HomeRecommendationNativeType,
  HomeRecommendationOverviewDatePreset,
  HomeRecommendationType,
} from "@/lib/operatorHome/homeRecommendationContract";
import type {
  CampaignRecommendationDraftPrefill,
  CampaignRecommendationEchoedCounts,
} from "@/types/operatorCampaigns";

export type { OperatorSidebarActiveId, OperatorSidebarNavModel };
export type { OperatorSidebarNavId } from "@/lib/operatorHome/sidebarNav";
export type {
  HomeRecommendationCampaignType,
  HomeRecommendationDomainActionKind,
  HomeRecommendationNativeType,
  HomeRecommendationOverviewDatePreset,
  HomeRecommendationType,
} from "@/lib/operatorHome/homeRecommendationContract";

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
  /**
   * Location switcher status line (`Active` / later `Inactive`).
   * All Owned locations are active until deactivation ships.
   */
  isActive: boolean;
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
      consentLabel: "Opted in" | "Opted out" | "Not recorded";
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
    brandLogoPublicUrl: string | null;
    options: OperatorHomeLocationOption[];
  };
}

/** POST /home/recommendation — Home performance window (ticket 01). */
export type HomeRecommendationRequest = {
  locationId: number;
  /** Home performance preset id, or `custom`. */
  overviewDatePreset: HomeRecommendationOverviewDatePreset;
  /** Resolved ISO UTC bounds — always present (Home has no all-time). */
  from: string;
  to: string;
  refresh?: boolean;
};

/**
 * Domain primary CTA payload for Home-native types.
 * Null entity id means the domain list / create destination.
 */
export type HomeRecommendationDomainAction =
  | { kind: Extract<HomeRecommendationDomainActionKind, "open-feedback">; feedbackId: number | null }
  | { kind: Extract<HomeRecommendationDomainActionKind, "open-guest">; locationGuestId: number | null }
  | { kind: Extract<HomeRecommendationDomainActionKind, "open-offer">; offerId: number | null };

/**
 * Home Recommended next step payload.
 * Not typed as CampaignRecommendation — Home-native types use domain actions;
 * campaign types may carry the same draftPrefill + audience fields as Campaigns.
 */
export type HomeRecommendation = {
  type: HomeRecommendationType;
  title?: string | null;
  opportunity?: string | null;
  whyBullets?: string[] | null;
  /** Present for Home-native types when a primary CTA is available. */
  action?: HomeRecommendationDomainAction | null;
  /** Server metrics only; shape varies by type. Campaign types reuse Campaigns counts. */
  echoedCounts?: CampaignRecommendationEchoedCounts | null;
  /** Campaign types only — same as Campaigns recommendation. */
  eligibleAudience?: string | null;
  suggestedChannel?: "email" | "sms" | null;
  estimatedUsage?: string | null;
  draftPrefill?: CampaignRecommendationDraftPrefill | null;
  locationName?: string | null;
};

export type HomeRecommendationResponse = {
  success: boolean;
  recommendation?: HomeRecommendation;
  message?: string;
  retryable?: boolean;
};

/** Narrow helpers for Home-native vs campaign allow-list members. */
export type HomeRecommendationNative = HomeRecommendation & {
  type: HomeRecommendationNativeType;
};

export type HomeRecommendationCampaign = HomeRecommendation & {
  type: HomeRecommendationCampaignType;
};

/** One domain section in a Weekly brief body (capture / feedback / offers / campaigns). */
export type WeeklyBriefSection = {
  hasData: boolean;
  summary: string;
  echoedCounts?: Record<string, number> | null;
};

/** Structured Weekly brief body (schema v1). */
export type WeeklyBriefBody = {
  headline: string;
  capture: WeeklyBriefSection;
  feedback: WeeklyBriefSection;
  offers: WeeklyBriefSection;
  campaigns: WeeklyBriefSection;
  watchNext: string[];
};

export type WeeklyBriefMetrics = {
  guestsJoined: number;
  qrScanEvents: number;
  feedbackCount: number;
  positiveFeedbackCount: number;
  neutralFeedbackCount: number;
  negativeFeedbackCount: number;
  needsAttentionCount: number;
  detectedTagCounts: Record<string, number>;
  activeOffers: number;
  claimsInWeek: number;
  redemptionsInWeek: number;
  campaignsSentInWeek: number;
  campaignRecipientsReached: number;
};

export type WeeklyBriefNotReadyResponse = {
  success: true;
  ready: false;
  locationId: number;
  week: string;
};

export type WeeklyBriefReadyResponse = {
  success: true;
  ready: true;
  locationId: number;
  week: string;
  status: string;
  generatedAtUtc: string;
  body: WeeklyBriefBody;
  metrics: WeeklyBriefMetrics;
};

export type WeeklyBriefGetResponse =
  | WeeklyBriefNotReadyResponse
  | WeeklyBriefReadyResponse;

export type WeeklyBriefGenerateFailureResponse = {
  success: false;
  message: string;
  retryable?: boolean;
};

export type WeeklyBriefGenerateResponse =
  | WeeklyBriefReadyResponse
  | WeeklyBriefGenerateFailureResponse;
