/**
 * PROTOTYPE — persist wizard drafts across refresh for QA walkthroughs.
 * Delete with the prototype route.
 */
import type { AccountSetupMultiFormValues } from "@/schemas/accountSetupMulti"
import type { AccountSetupSingleFormValues } from "@/schemas/accountSetupSingle"

export type PrototypeFormDraftAccountType = "single" | "multi"

const STORAGE_KEYS = {
  single: "tummly-prototype-account-setup-single",
  multi: "tummly-prototype-account-setup-multi",
} as const satisfies Record<PrototypeFormDraftAccountType, string>

function storageKey(accountType: PrototypeFormDraftAccountType) {
  return STORAGE_KEYS[accountType]
}

export function loadPrototypeFormDraft<T extends PrototypeFormDraftAccountType>(
  accountType: T
): T extends "multi"
  ? Partial<AccountSetupMultiFormValues> | null
  : Partial<AccountSetupSingleFormValues> | null {
  if (typeof sessionStorage === "undefined") {
    return null
  }

  const raw = sessionStorage.getItem(storageKey(accountType))
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as Partial<
      AccountSetupMultiFormValues | AccountSetupSingleFormValues
    >
  } catch {
    sessionStorage.removeItem(storageKey(accountType))
    return null
  }
}

export function savePrototypeFormDraft(
  accountType: PrototypeFormDraftAccountType,
  values: AccountSetupSingleFormValues | AccountSetupMultiFormValues
) {
  if (typeof sessionStorage === "undefined") {
    return
  }

  sessionStorage.setItem(storageKey(accountType), JSON.stringify(values))
}

export function clearPrototypeFormDraft(accountType: PrototypeFormDraftAccountType) {
  if (typeof sessionStorage === "undefined") {
    return
  }

  sessionStorage.removeItem(storageKey(accountType))
}
