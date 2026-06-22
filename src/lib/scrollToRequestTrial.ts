export const REQUEST_TRIAL_SECTION_ID = "request-trial"
export const REQUEST_TRIAL_HASH = `#${REQUEST_TRIAL_SECTION_ID}`

export function scrollToRequestTrial() {
  const element = document.getElementById(REQUEST_TRIAL_SECTION_ID)
  if (!element) {
    return
  }

  const behavior: ScrollBehavior = window.matchMedia(
    "(prefers-reduced-motion: reduce)"
  ).matches
    ? "auto"
    : "smooth"

  element.scrollIntoView({ behavior, block: "start" })
}
