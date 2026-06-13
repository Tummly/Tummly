import * as React from "react"
import { motion, useReducedMotion } from "framer-motion"
import { Eye, EyeOff } from "lucide-react"

import { cn } from "@/lib/utils"
import { Field, FieldError } from "@/components/ui/field"
import { Button } from "@/components/ui/button"

const INPUT_HEIGHT = 50
const LABEL_TOP = 8
/** Label y-offset when resting (centered in the value row). */
const LABEL_REST_Y = 7
/** Value row — fixed so focus never shifts input text or container height. */
const INPUT_TEXT_TOP = 22
const INPUT_TEXT_HEIGHT = 20

const labelVariants = {
  rest: {
    y: LABEL_REST_Y,
    scale: 1,
  },
  active: {
    y: 0,
    scale: 12 / 14,
  },
}

const labelTransition = {
  type: "spring" as const,
  stiffness: 560,
  damping: 36,
  mass: 0.45,
}

type FloatingLabelInputProps = Omit<
  React.ComponentProps<"input">,
  "placeholder" | "size"
> & {
  label: string
  error?: string
  disableFocusRing?: boolean
}

const FloatingLabelInput = React.forwardRef<
  HTMLInputElement,
  FloatingLabelInputProps
>(function FloatingLabelInput(
  {
    label,
    error,
    disableFocusRing = false,
    className,
    disabled,
    readOnly,
    type = "text",
    id,
    value,
    defaultValue,
    onFocus,
    onBlur,
    onChange,
    ...props
  },
  ref
) {
  const shouldReduceMotion = useReducedMotion()
  const generatedId = React.useId()
  const inputId = id ?? generatedId
  const errorId = `${inputId}-error`
  const isPassword = type === "password"

  const [focused, setFocused] = React.useState(false)
  const [showPassword, setShowPassword] = React.useState(false)
  const [uncontrolledValue, setUncontrolledValue] = React.useState(
    () => (defaultValue != null ? String(defaultValue) : "")
  )

  const isControlled = value !== undefined
  const currentValue = isControlled ? String(value) : uncontrolledValue
  const hasValue = currentValue.length > 0
  const isActive = focused || hasValue || readOnly

  const handleFocus = (event: React.FocusEvent<HTMLInputElement>) => {
    setFocused(true)
    onFocus?.(event)
  }

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    setFocused(false)
    onBlur?.(event)
  }

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (!isControlled) {
      setUncontrolledValue(event.target.value)
    }
    onChange?.(event)
  }

  const inputType = isPassword && showPassword ? "text" : type

  return (
    <Field
      data-slot="floating-label-input"
      data-disabled={disabled ? true : undefined}
      data-invalid={error ? true : undefined}
      className={cn("gap-1.5", className)}
    >
      <div
        className={cn(
          "box-border flex w-full shrink-0 items-center gap-0.5 rounded-[4px] border border-[rgba(74,74,76,0.4)] px-[13px]",
          readOnly && "bg-[rgba(54,54,56,0.07)]",
          disabled && "cursor-not-allowed opacity-50",
          error && "border-destructive",
          !disableFocusRing &&
            "focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50",
          error && !disableFocusRing && "focus-within:ring-destructive/20"
        )}
        style={{
          height: INPUT_HEIGHT,
          minHeight: INPUT_HEIGHT,
          maxHeight: INPUT_HEIGHT,
        }}
      >
        <div
          className="relative min-w-0 flex-1 shrink-0"
          style={{ height: INPUT_HEIGHT }}
        >
          <motion.label
            htmlFor={inputId}
            initial={false}
            variants={labelVariants}
            animate={isActive ? "active" : "rest"}
            transition={
              shouldReduceMotion ? { duration: 0 } : labelTransition
            }
            style={{
              top: LABEL_TOP,
              transformOrigin: "0 0",
              willChange: "transform",
            }}
            className="pointer-events-none absolute left-0 z-10 origin-top-left text-sm leading-5 text-[#7d7d7d]"
          >
            {label}
          </motion.label>

          <input
            ref={ref}
            {...props}
            id={inputId}
            type={inputType}
            value={isControlled ? value : uncontrolledValue}
            disabled={disabled}
            readOnly={readOnly}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? errorId : undefined}
            onFocus={handleFocus}
            onBlur={handleBlur}
            onChange={handleChange}
            className={cn(
              "absolute left-0 w-full min-w-0 border-0 bg-transparent p-0 text-sm leading-5 text-[#141414] outline-none",
              readOnly && "cursor-default",
              isPassword && "pr-1"
            )}
            style={{
              top: INPUT_TEXT_TOP,
              height: INPUT_TEXT_HEIGHT,
            }}
          />
        </div>

        {isPassword && !readOnly && (
          <Button
            type="button"
            variant="ghost"
            size="icon-xs"
            disabled={disabled}
            aria-label={showPassword ? "Hide password" : "Show password"}
            onClick={() => setShowPassword((visible) => !visible)}
            className="size-[18px] shrink-0 text-[#7d7d7d] hover:bg-transparent hover:text-[#141414]"
          >
            {showPassword ? (
              <EyeOff data-icon="inline-end" />
            ) : (
              <Eye data-icon="inline-end" />
            )}
          </Button>
        )}
      </div>

      {error ? (
        <FieldError id={errorId}>{error}</FieldError>
      ) : null}
    </Field>
  )
})

FloatingLabelInput.displayName = "FloatingLabelInput"

export { FloatingLabelInput }
export type { FloatingLabelInputProps }
