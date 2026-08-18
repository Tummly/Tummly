import { parseApiInstantMs } from "@/lib/operatorHome/relativeTime"
import type {
  GuestProfileContactEligibilityRow,
  LocationGuestMarketingPreference,
} from "@/types/dashboard"

export const MANAGE_MARKETING_PREFERENCES_EMPTY_EVIDENCE = "—"

export const MANAGE_MARKETING_PREFERENCES_COPY = {
  dialogTitle: "Manage marketing preferences",
  subtitle: (guestName: string) =>
    `Choose how ${guestName} may receive marketing. Transactional messages are not affected.`,
  saveLabel: "Save preferences",
  cancelLabel: "Cancel",
  emailChannelLabel: "Email marketing",
  smsChannelLabel: "SMS marketing",
  availableBadge: "Available",
  unavailableBadge: "Unavailable",
  permissionSourceLabel: "Permission source",
  recordedOnLabel: "Recorded on",
  noteLabel: "Internal note (optional)",
  notePlaceholder: "Why was this preference changed?",
  permissionSourceWithTimestamp: "Guest feedback form",
  emptyEvidence: MANAGE_MARKETING_PREFERENCES_EMPTY_EVIDENCE,
  optedOutConsequence:
    "This guest will be added to the marketing suppression list.",
  notRecordedConsequence:
    "Without recorded permission, this guest is treated as ineligible.",
  saveError: "Could not save marketing preferences.",
  noteSaveError: "Could not save the note.",
  loadError: "Could not load marketing preferences.",
} as const

export const MARKETING_PREFERENCE_STATUS_CARDS = [
  {
    id: "allowed" as const,
    label: "Allowed",
    helper: "May receive campaigns",
  },
  {
    id: "opted_out" as const,
    label: "Opted out",
    helper: "Suppress all marketing",
  },
  {
    id: "not_recorded" as const,
    label: "Not recorded",
    helper: "Treat as ineligible",
  },
]

export function operatorMaySelectMarketingPreference(
  current: LocationGuestMarketingPreference,
  next: LocationGuestMarketingPreference
): boolean {
  if (next === "allowed") {
    return current === "allowed"
  }
  return true
}

export function channelHasContact(value: string | null | undefined): boolean {
  return value != null && value.trim() !== ""
}

export function marketingPreferencePermissionSource(
  consentAt: string | null
): string {
  if (consentAt == null || consentAt.trim() === "") {
    return MANAGE_MARKETING_PREFERENCES_COPY.emptyEvidence
  }
  return MANAGE_MARKETING_PREFERENCES_COPY.permissionSourceWithTimestamp
}

export function formatMarketingPreferenceRecordedOn(
  consentAt: string | null
): string {
  if (consentAt == null || consentAt.trim() === "") {
    return MANAGE_MARKETING_PREFERENCES_COPY.emptyEvidence
  }

  const ms = parseApiInstantMs(consentAt)
  if (Number.isNaN(ms)) {
    return MANAGE_MARKETING_PREFERENCES_COPY.emptyEvidence
  }

  return new Date(ms).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    timeZone: "Europe/London",
  })
}

export function resolveMarketingPreferenceConsentAt(
  rows: readonly GuestProfileContactEligibilityRow[]
): string | null {
  for (const row of rows) {
    if (row.detailAt != null && row.detailAt.trim() !== "") {
      return row.detailAt
    }
  }
  return null
}

export function marketingPreferenceConsequenceHelper(
  preference: LocationGuestMarketingPreference
): string | null {
  if (preference === "opted_out") {
    return MANAGE_MARKETING_PREFERENCES_COPY.optedOutConsequence
  }
  if (preference === "not_recorded") {
    return MANAGE_MARKETING_PREFERENCES_COPY.notRecordedConsequence
  }
  return null
}

export function isMarketingPreferenceSaveDirty(
  current: LocationGuestMarketingPreference,
  draft: LocationGuestMarketingPreference,
  note: string
): boolean {
  return current !== draft || note.trim().length > 0
}

/** Figma 5014:49795 — 945px so three status cards sit in one row. */
export const MANAGE_MARKETING_PREFERENCES_DIALOG_CLASS =
  "flex max-h-[90vh] flex-col gap-[60px] overflow-y-auto border-0 bg-op-surface-secondary p-8 text-op-text-primary shadow-lg sm:max-w-[945px] dark:bg-[var(--op-color-gray-1000)]"

export const MANAGE_MARKETING_PREFERENCES_SECTION_CLASS =
  "flex w-full flex-col gap-5"

export const MANAGE_MARKETING_PREFERENCES_CARDS_CLASS =
  "flex w-full flex-col gap-[18px] sm:flex-row"

export const MANAGE_MARKETING_PREFERENCES_DIVIDER_CLASS =
  "h-px w-full shrink-0 bg-op-border-default"

export const MANAGE_MARKETING_PREFERENCES_CHANNEL_ICON_CLASS =
  "flex size-9 shrink-0 items-center justify-center rounded-[2px] bg-op-background-secondary p-2.5"

export const MANAGE_MARKETING_PREFERENCES_EVIDENCE_FIELD_CLASS =
  "h-[50px] shadow-none"
