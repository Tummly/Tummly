/** Shared email design tokens — Figma metrics, typography roles from docs/agents/typography.md */

export const colors = {
  black: "#141414",
  white: "#ffffff",
  gray550: "#7d7d7d",
  gray555: "#555555",
  /** Guest-response ticket fill — Figma 6852:49926 */
  gray995: "#191919",
  /** Guest-response ticket border — Figma 6852:49926 */
  gray980: "#292929",
  footerBg: "#f9f9fa",
  divider: "#e8e8e8",
  buttonGreen: "#14a74a",
} as const

/** *headline* — Plus Jakarta Sans (Figma Season Mix → role table) */
export const fontHeadline =
  "'Plus Jakarta Sans', 'Helvetica Neue', Helvetica, Arial, sans-serif"

/** *functional* — Helvetica Neue */
export const fontFunctional =
  "'Helvetica Neue', Helvetica, Arial, sans-serif"

export const supportEmail = "support@tummly.com"

export const brandTagline =
  "Turn everyday orders and visits into guest relationships you can build on."
