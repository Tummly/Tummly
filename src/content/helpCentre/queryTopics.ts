/** Contact form + Support inbox topic filter (marketing Contact redesign). */
export const HELP_CENTRE_QUERY_TOPICS = [
  { slug: "starting-with-tummly", label: "Starting with Tummly" },
  { slug: "plans-and-pricing", label: "Plans and pricing" },
  { slug: "multi-location-setup", label: "Multi-Location setup" },
  {
    slug: "qr-materials-or-starter-kit",
    label: "QR materials or Starter Kit",
  },
  {
    slug: "existing-account",
    label: "Help with an existing Tummly account",
  },
  { slug: "billing-or-subscription", label: "Billing or subscription" },
  { slug: "privacy-or-data-request", label: "Privacy or data request" },
  {
    slug: "partnership-media-or-company",
    label: "Partnership, media or company enquiry",
  },
  { slug: "something-else", label: "Something else" },
] as const

export type HelpCentreQueryTopicSlug =
  (typeof HELP_CENTRE_QUERY_TOPICS)[number]["slug"]

/** Optional location-count choices for sales-style topics. */
export const HELP_CENTRE_LOCATION_COUNT_OPTIONS = [
  { value: "1", label: "1 Location" },
  { value: "2-3", label: "2–3 Locations" },
  { value: "4-5", label: "4–5 Locations" },
  { value: "6-10", label: "6–10 Locations" },
  { value: "11-20", label: "11–20 Locations" },
  { value: "21-30", label: "21–30 Locations" },
  { value: "30+", label: "More than 30 Locations" },
  { value: "not-operating", label: "Not operating yet" },
] as const

export const HELP_CENTRE_ALREADY_USING_OPTIONS = [
  { value: "yes", label: "Yes" },
  { value: "no", label: "No" },
] as const
