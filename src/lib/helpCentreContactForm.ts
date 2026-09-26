import type { HelpCentreQueryTopicSlug } from "@/content/helpCentre/queryTopics"
import {
  HELP_CENTRE_ALREADY_USING_OPTIONS,
  HELP_CENTRE_LOCATION_COUNT_OPTIONS,
} from "@/content/helpCentre/queryTopics"
import type { HelpCentreContactFormValues } from "@/schemas/helpCentreContact"

export type ContactTopicFieldFlags = {
  showBusinessName: boolean
  businessNameLabel: string
  showLocationCount: boolean
  showAlreadyUsing: boolean
  showAlternateEmail: boolean
  alternateEmailLabel: string
  showQrOrderHint: boolean
}

const SALES_STYLE: ContactTopicFieldFlags = {
  showBusinessName: true,
  businessNameLabel: "Restaurant or business name",
  showLocationCount: true,
  showAlreadyUsing: false,
  showAlternateEmail: false,
  alternateEmailLabel: "",
  showQrOrderHint: false,
}

const TOPIC_FIELDS: Record<HelpCentreQueryTopicSlug, ContactTopicFieldFlags> = {
  "starting-with-tummly": SALES_STYLE,
  "plans-and-pricing": SALES_STYLE,
  "multi-location-setup": {
    ...SALES_STYLE,
    businessNameLabel: "Restaurant or group name",
  },
  "qr-materials-or-starter-kit": {
    ...SALES_STYLE,
    showAlreadyUsing: true,
    showQrOrderHint: true,
  },
  "existing-account": {
    showBusinessName: false,
    businessNameLabel: "",
    showLocationCount: false,
    showAlreadyUsing: false,
    showAlternateEmail: true,
    alternateEmailLabel: "Account Email if different from above — Optional",
    showQrOrderHint: false,
  },
  "billing-or-subscription": {
    showBusinessName: true,
    businessNameLabel: "Restaurant or business name",
    showLocationCount: false,
    showAlreadyUsing: false,
    showAlternateEmail: true,
    alternateEmailLabel: "Billing Email — Optional",
    showQrOrderHint: false,
  },
  "privacy-or-data-request": {
    showBusinessName: false,
    businessNameLabel: "",
    showLocationCount: false,
    showAlreadyUsing: false,
    showAlternateEmail: false,
    alternateEmailLabel: "",
    showQrOrderHint: false,
  },
  "partnership-media-or-company": {
    showBusinessName: true,
    businessNameLabel: "Company or organisation",
    showLocationCount: false,
    showAlreadyUsing: false,
    showAlternateEmail: false,
    alternateEmailLabel: "",
    showQrOrderHint: false,
  },
  "something-else": {
    showBusinessName: false,
    businessNameLabel: "",
    showLocationCount: false,
    showAlreadyUsing: false,
    showAlternateEmail: false,
    alternateEmailLabel: "",
    showQrOrderHint: false,
  },
}

export function getContactTopicFieldFlags(
  topic: string
): ContactTopicFieldFlags {
  if (topic in TOPIC_FIELDS) {
    return TOPIC_FIELDS[topic as HelpCentreQueryTopicSlug]
  }

  return TOPIC_FIELDS["something-else"]
}

function locationCountLabel(value: string): string | null {
  const match = HELP_CENTRE_LOCATION_COUNT_OPTIONS.find(
    (option) => option.value === value
  )
  return match?.label ?? null
}

function alreadyUsingLabel(value: string): string | null {
  const match = HELP_CENTRE_ALREADY_USING_OPTIONS.find(
    (option) => option.value === value
  )
  return match?.label ?? null
}

/** Build API message with optional extras prepended for Support. */
export function buildHelpCentreContactMessage(
  values: HelpCentreContactFormValues
): string {
  const flags = getContactTopicFieldFlags(values.topic)
  const lines: string[] = []

  if (flags.showAlreadyUsing && values.alreadyUsingTummly.trim()) {
    const label = alreadyUsingLabel(values.alreadyUsingTummly)
    if (label) {
      lines.push(`Already using Tummly: ${label}`)
    }
  }

  if (flags.showLocationCount && values.locationCount.trim()) {
    const label = locationCountLabel(values.locationCount)
    if (label) {
      lines.push(`Locations operated: ${label}`)
    }
  }

  if (flags.showAlternateEmail && values.alternateEmail.trim()) {
    const label =
      values.topic === "billing-or-subscription"
        ? "Billing email"
        : "Account email"
    lines.push(`${label}: ${values.alternateEmail.trim()}`)
  }

  const body = values.message.trim()

  if (lines.length === 0) {
    return body
  }

  return `${lines.join("\n")}\n\n${body}`
}

export function resolveHelpCentreContactBusinessName(
  values: HelpCentreContactFormValues
): string {
  const flags = getContactTopicFieldFlags(values.topic)
  if (!flags.showBusinessName) {
    return ""
  }

  return values.businessName.trim()
}
