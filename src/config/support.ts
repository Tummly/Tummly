export const SUPPORT_EMAIL = "support@tummly.com"

const HELP_CENTRE_ROUTE = "help-center"

/** Public Help Centre hub — matches email footer links (`{baseUrl}/help-center`). */
export const HELP_CENTRE_URL = `/${HELP_CENTRE_ROUTE}`

/** Contact us intake form. */
export const HELP_CENTRE_CONTACT_URL = `/${HELP_CENTRE_ROUTE}/contact`

/** Contact form confirmation screen. */
export const HELP_CENTRE_CONTACT_SUCCESS_URL =
  `/${HELP_CENTRE_ROUTE}/contact/success`

/** Signed-in operator query list. */
export const HELP_CENTRE_MY_QUERIES_URL = `/${HELP_CENTRE_ROUTE}/my-queries`

/** React Router path segments (no leading slash). */
export const HELP_CENTRE_ROUTES = {
  hub: HELP_CENTRE_ROUTE,
  article: `${HELP_CENTRE_ROUTE}/articles/:slug`,
  contact: `${HELP_CENTRE_ROUTE}/contact`,
  contactSuccess: `${HELP_CENTRE_ROUTE}/contact/success`,
  myQueries: `${HELP_CENTRE_ROUTE}/my-queries`,
  myQuery: `${HELP_CENTRE_ROUTE}/my-queries/:id`,
} as const

export function helpCentreArticleUrl(slug: string) {
  return `/help-center/articles/${slug}`
}

export function helpCentreMyQueryUrl(id: number | string) {
  return `${HELP_CENTRE_MY_QUERIES_URL}/${id}`
}
