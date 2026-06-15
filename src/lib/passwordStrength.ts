export type PasswordStrengthScore = 0 | 1 | 2 | 3 | 4

export type PasswordStrengthLabel =
  | "Very weak"
  | "Weak"
  | "Good"
  | "Excellent"

const STRENGTH_LABELS: Record<
  Exclude<PasswordStrengthScore, 0>,
  PasswordStrengthLabel
> = {
  1: "Very weak",
  2: "Weak",
  3: "Good",
  4: "Excellent",
}

/** Four-tier score used across account setup and reset password. */
export function getPasswordStrengthScore(
  password: string
): PasswordStrengthScore {
  if (!password) {
    return 0
  }

  let score = 0

  if (password.length >= 8) {
    score++
  }

  if (/[A-Z]/.test(password)) {
    score++
  }

  if (/[0-9!@#$%^&*]/.test(password)) {
    score++
  }

  if (password.length >= 12) {
    score++
  }

  return Math.min(score, 4) as PasswordStrengthScore
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
    return "#E5E7EB"
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
