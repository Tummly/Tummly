import type { SignupOnboardingFormValues } from "@/schemas/signupOnboarding"

const DRAFT_KEY_PREFIX = "tummly.signupOnboardingDraft."

/** Fields never written to sessionStorage. */
const SECRET_FIELDS = ["password", "confirmPassword", "token"] as const

export type SignupOnboardingDraft = {
  step: number
  values: Partial<SignupOnboardingFormValues>
}

function draftKey(sessionToken: string): string {
  return `${DRAFT_KEY_PREFIX}${sessionToken}`
}

/** Keep wizard values + step across reload (secrets excluded). */
export function saveSignupOnboardingDraft(
  sessionToken: string,
  step: number,
  values: Partial<SignupOnboardingFormValues>,
): void {
  const safe: Record<string, unknown> = { ...values }
  for (const field of SECRET_FIELDS) {
    delete safe[field]
  }
  try {
    sessionStorage.setItem(
      draftKey(sessionToken),
      JSON.stringify({ step, values: safe }),
    )
  } catch {
    // Storage full or blocked: the wizard still works without a draft.
  }
}

export function readSignupOnboardingDraft(
  sessionToken: string,
): SignupOnboardingDraft | null {
  const raw = sessionStorage.getItem(draftKey(sessionToken))
  if (raw == null || raw.trim() === "") {
    return null
  }
  try {
    const parsed = JSON.parse(raw) as SignupOnboardingDraft
    if (typeof parsed.step !== "number" || parsed.values == null) {
      return null
    }
    const values: Record<string, unknown> = { ...parsed.values }
    for (const field of SECRET_FIELDS) {
      delete values[field]
    }
    return { step: parsed.step, values }
  } catch {
    return null
  }
}

export function clearSignupOnboardingDraft(sessionToken: string): void {
  sessionStorage.removeItem(draftKey(sessionToken))
}
