export type PasswordStrengthScore = 0 | 1 | 2 | 3 | 4 | 5

export type PasswordStrengthLabel =
  | "Very weak"
  | "Weak"
  | "Good"
  | "Strong"
  | "Excellent"

export const PASSWORD_STRENGTH_BAR_COUNT = 5

const MIN_LENGTH = 8
const STRONG_MIN_LENGTH = 10
const EXCELLENT_MIN_LENGTH = 12

const STRENGTH_LABELS: Record<
  Exclude<PasswordStrengthScore, 0>,
  PasswordStrengthLabel
> = {
  1: "Very weak",
  2: "Weak",
  3: "Good",
  4: "Strong",
  5: "Excellent",
}

function hasUppercase(password: string) {
  return /[A-Z]/.test(password)
}

function hasNumber(password: string) {
  return /[0-9]/.test(password)
}

function hasSymbol(password: string) {
  return /[^A-Za-z0-9]/.test(password)
}

function hasNumberOrSymbol(password: string) {
  return hasNumber(password) || hasSymbol(password)
}

/**
 * Five-tier score used across account setup and reset password.
 *
 * 1 Very weak — fewer than 8 characters
 * 2 Weak — 8+ but missing uppercase or number/symbol
 * 3 Good — 8+ with uppercase and number or symbol (minimum accepted)
 * 4 Strong — 10+ with the same mix as Good
 * 5 Excellent — 12+ with uppercase, number, and symbol
 */
export function getPasswordStrengthScore(
  password: string
): PasswordStrengthScore {
  if (!password) {
    return 0
  }

  if (password.length < MIN_LENGTH) {
    return 1
  }

  const uppercase = hasUppercase(password)
  const numberOrSymbol = hasNumberOrSymbol(password)

  if (!uppercase || !numberOrSymbol) {
    return 2
  }

  if (
    password.length >= EXCELLENT_MIN_LENGTH &&
    hasNumber(password) &&
    hasSymbol(password)
  ) {
    return 5
  }

  if (password.length >= STRONG_MIN_LENGTH) {
    return 4
  }

  return 3
}

export function isPasswordAtLeastGood(password: string): boolean {
  return getPasswordStrengthScore(password) >= 3
}

export function getPasswordStrengthLabel(
  score: PasswordStrengthScore
): PasswordStrengthLabel | null {
  if (score === 0) {
    return null
  }

  return STRENGTH_LABELS[score]
}

export function getPasswordStrengthBarColor(
  barIndex: number,
  score: PasswordStrengthScore
): string {
  if (barIndex >= score) {
    return "#D2D2D2"
  }

  if (score === 1) {
    return "#EF4444"
  }

  if (score === 2) {
    return "#F59E0B"
  }

  return "#22C55E"
}

export function getPasswordStrengthLabelColor(
  score: PasswordStrengthScore
): string {
  if (score === 1) {
    return "#EF4444"
  }

  if (score === 2) {
    return "#F59E0B"
  }

  if (score >= 3) {
    return "#22C55E"
  }

  return "#232323"
}
