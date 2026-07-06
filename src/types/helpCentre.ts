export type HelpCentreQueryStatus =
  | "NEW"
  | "IN_PROGRESS"
  | "WAITING_ON_CUSTOMER"
  | "ESCALATED_TO_ADMIN"
  | "RESOLVED"
  | "CLOSED"

export type HelpCentreAuthorKind = "SUBMITTER" | "SUPPORT" | "OPERATOR"

export interface HelpCentreQueryMessage {
  id: number
  authorKind: HelpCentreAuthorKind
  body: string
  createdAt: string
}

export interface HelpCentreQueryListItem {
  id: number
  topic: string
  topicLabel: string
  status: HelpCentreQueryStatus
  statusLabel: string
  updatedAt: string
  preview?: string | null
}

export interface HelpCentreQueryDetail extends HelpCentreQueryListItem {
  submitterName: string
  submitterEmail: string
  phone?: string | null
  businessName: string
  queryLocation?: { id: number; label: string } | null
  linkedOperator?: boolean
  linkedOperatorEmail?: string | null
  escalationNote?: string | null
  canReply?: boolean
  createdAt: string
  messages: HelpCentreQueryMessage[]
}

export interface HelpCentreContactPrefill {
  businessName: string
  submitterName: string
  submitterEmail: string
  locations: Array<{ id: number; label: string }>
}

export interface HelpCentreArticle {
  slug: string
  title: string
  summary: string
  body: string
  relatedSlugs: string[]
}
