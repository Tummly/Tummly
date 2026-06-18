import {
  getPasswordStrengthBarColor,
  getPasswordStrengthLabel,
  getPasswordStrengthLabelColor,
  getPasswordStrengthScore,
  PASSWORD_STRENGTH_BAR_COUNT,
} from "@/lib/passwordStrength"
import { cn } from "@/lib/utils"

type PasswordStrengthMeterProps = {
  password: string
  className?: string
  /**
   * When true, the meter is hidden until the user types (matches the original
   * reset-password behavior). Defaults to false so the Account step keeps its
   * always-visible bars.
   */
  hideWhenEmpty?: boolean
}

export function PasswordStrengthMeter({
  password,
  className,
  hideWhenEmpty = false,
}: PasswordStrengthMeterProps) {
  if (hideWhenEmpty && !password) {
    return null
  }

  const score = getPasswordStrengthScore(password)
  const label = getPasswordStrengthLabel(score)

  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <div className="flex gap-3" aria-hidden>
        {Array.from({ length: PASSWORD_STRENGTH_BAR_COUNT }, (_, index) => (
          <div
            key={index}
            className="h-0.5 min-w-0 flex-1 rounded-full transition-colors duration-300"
            style={{
              backgroundColor: getPasswordStrengthBarColor(index, score),
            }}
          />
        ))}
      </div>

      {label ? (
        <p
          className="m-0 text-right text-sm font-normal leading-5"
          style={{ color: getPasswordStrengthLabelColor(score) }}
          aria-live="polite"
        >
          {label}
        </p>
      ) : null}
    </div>
  )
}
