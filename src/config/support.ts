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

/** Support dashboard inbox. */
export const SUPPORT_DASHBOARD_URL = "/support-dashboard"

/** React Router path segments for Support dashboard (no leading slash). */
export const SUPPORT_DASHBOARD_ROUTES = {
  inbox: "support-dashboard",
  query: "support-dashboard/queries/:id",
} as const

export type SupportInboxParams = {
  q?: string
  status?: string
  topic?: string
  type?: "operator" | "contact"
  page?: number
  pageSize?: number
}

const DEFAULT_PAGE_SIZE = 20

/** Build inbox URL with list state; omits defaults (`page=1`, `pageSize=20`, empty filters). */
export function supportDashboardInboxUrl(params: SupportInboxParams = {}) {
  const search = new URLSearchParams()

  const q = params.q?.trim()
  if (q) {
    search.set("q", q)
  }
  if (params.status) {
    search.set("status", params.status)
  }
  if (params.topic) {
    search.set("topic", params.topic)
  }
  if (params.type) {
    search.set("type", params.type)
  }
  if (params.page && params.page > 1) {
    search.set("page", String(params.page))
  }
  if (params.pageSize && params.pageSize !== DEFAULT_PAGE_SIZE) {
    search.set("pageSize", String(params.pageSize))
  }

  const query = search.toString()
  return query ? `${SUPPORT_DASHBOARD_URL}?${query}` : SUPPORT_DASHBOARD_URL
}

export function supportDashboardQueryUrl(id: number | string) {
  return `${SUPPORT_DASHBOARD_URL}/queries/${id}`
}
