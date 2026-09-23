import type { LegalPageContent, LegalSection } from "./types"

const PLACEHOLDER_BODY =
  "Full legal copy for this section will be published when the review is complete."

function buildPlaceholderSections(
  titles: readonly string[],
): LegalSection[] {
  return titles.map((title, index) => {
    const number = index + 1
    const id = title
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-|-$/g, "")

    return {
      id: `${number}-${id}`,
      title: `${number}. ${title}`,
      content: <p>{PLACEHOLDER_BODY}</p>,
    }
  })
}

/** Shared Figma TOC labels used as temporary section stubs. */
const FIGMA_PLACEHOLDER_TOC = [
  "Who we are",
  "Who this notice applies to",
  "Tummly and restaurants",
  "Information we handle",
  "How information is used",
  "Permissions and marketing",
  "AI features",
  "Who information may be shared with",
  "International processing",
  "How long information is kept",
  "Your rights",
  "Cookies",
  "Security",
  "Children",
  "Changes to this notice",
  "Contact us",
] as const

const placeholderSections = buildPlaceholderSections(FIGMA_PLACEHOLDER_TOC)

/** Figma Accessibility (`4974:27034`). Body copy will be replaced later. */
export const accessibilityContent: LegalPageContent = {
  title: "Accessibility at Tummly",
  description:
    "We want Tummly to be practical and usable for as many people as possible — whether someone is visiting our website, giving Feedback to a restaurant or using Guest Loop to run their business.",
  sections: placeholderSections,
}

/** Figma Acceptable Use Policy (`4974:27074`). Body copy will be replaced later. */
export const acceptableUseContent: LegalPageContent = {
  title: "Acceptable Use Policy",
  lastUpdated: "[DD Month YYYY]",
  description: [
    "Tummly is built to help restaurants create permission-aware guest relationships.",
    "This policy explains the kinds of activity that are not permitted when using Tummly.",
    "It forms part of the applicable agreement for use of the service.",
  ],
  sections: placeholderSections,
}

/** Figma Shop & Print Terms (`4974:27156`). Body copy will be replaced later. */
export const shopPrintTermsContent: LegalPageContent = {
  title: "Shop & Print Terms",
  lastUpdated: "[DD Month YYYY]",
  description: [
    "Tummly is built to help restaurants create permission-aware guest relationships.",
    "This policy explains the kinds of activity that are not permitted when using Tummly.",
    "It forms part of the applicable agreement for use of the service.",
  ],
  sections: placeholderSections,
}
