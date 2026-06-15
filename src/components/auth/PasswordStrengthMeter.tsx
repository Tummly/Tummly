import {
  getPasswordStrengthBarColor,
  getPasswordStrengthLabel,
  getPasswordStrengthLabelColor,
  getPasswordStrengthScore,
} from "@/lib/passwordStrength"
import { cn } from "@/lib/utils"

type PasswordStrengthMeterProps = {
  password: string
  className?: string
}

export function PasswordStrengthMeter({
  password,
  className,
}: PasswordStrengthMeterProps) {
  const score = getPasswordStrengthScore(password)
  const label = getPasswordStrengthLabel(score)

  if (!password) {
    return null
  }

  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <div className="flex gap-3" aria-hidden>
        {Array.from({ length: 4 }, (_, index) => (
          <div
            key={index}
            className="h-[5px] min-w-0 flex-1 rounded-full transition-colors duration-300"
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
