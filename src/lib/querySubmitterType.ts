import type { SupportQuerySubmitterType } from "@/types/support"

/** Derive glossary **query submitter type** from the linked-operator wire flag. */
export function toQuerySubmitterType(
  linkedOperator: boolean
): SupportQuerySubmitterType {
  return linkedOperator ? "operator" : "contact"
}

export function querySubmitterTypeLabel(
  linkedOperator: boolean
): "Operator" | "Contact" {
  return linkedOperator ? "Operator" : "Contact"
}
