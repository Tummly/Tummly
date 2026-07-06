import type { HelpCentreQueryDetail, HelpCentreQueryListItem } from "./helpCentre"

export type SupportQueryListItem = HelpCentreQueryListItem & {
  submitterName: string
  submitterEmail: string
  businessName: string
  queryLocationLabel?: string | null
  linkedOperator: boolean
  linkedOperatorEmail?: string | null
}

export type SupportQueryDetail = HelpCentreQueryDetail
