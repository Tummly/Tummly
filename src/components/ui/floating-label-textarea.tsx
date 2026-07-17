import * as React from "react"
import { motion, useReducedMotion } from "framer-motion"

import { cn } from "@/lib/utils"
import { Field, FieldErrorSlot } from "@/components/ui/field"

const TEXTAREA_MIN_HEIGHT = 183
const LABEL_TOP = 15
const LABEL_REST_Y = 0
const INPUT_TEXT_TOP = 22

const labelVariants = {
  rest: {
    y: LABEL_REST_Y,
    scale: 1,
  },
  active: {
    y: -7,
    scale: 12 / 14,
  },
}

const labelTransition = {
  type: "spring" as const,
  stiffness: 560,
  damping: 36,
  mass: 0.45,
}

type FloatingLabelTextareaProps = Omit<
  React.ComponentProps<"textarea">,
  "placeholder" | "size"
> & {
  label: string
  optional?: boolean
  error?: string
  disableFocusRing?: boolean
  errorClassName?: string
  reserveSpace?: boolean
  reserveClassName?: string
  variant?: "light" | "dark"
  /** Bottom-right chrome inside the field (e.g. mic / Tick / X). */
  actions?: React.ReactNode
  notice?: string | null
}

const FloatingLabelTextarea = React.forwardRef<
  HTMLTextAreaElement,
  FloatingLabelTextareaProps
>(function FloatingLabelTextarea(
  {
    label,
    optional = false,
    error,
    disableFocusRing = false,
    className,
    errorClassName,
    reserveSpace = false,
    reserveClassName,
    variant = "light",
    disabled,
    readOnly,
    actions,
    notice,
    id,
    value,
    defaultValue,
    onFocus,
    onBlur,
    onChange,
    rows = 6,
    ...props
  },
  ref
) {
  const shouldReduceMotion = useReducedMotion()
  const generatedId = React.useId()
  const inputId = id ?? generatedId
  const errorId = `${inputId}-error`
  const noticeId = `${inputId}-notice`
  const hasActions = actions != null

  const [focused, setFocused] = React.useState(false)
  const [uncontrolledValue, setUncontrolledValue] = React.useState(
    () => (defaultValue != null ? String(defaultValue) : "")
  )

  const isControlled = value !== undefined
  const currentValue = isControlled ? String(value) : uncontrolledValue
  const hasValue = currentValue.length > 0
  const isActive = focused || hasValue || readOnly
  const isDark = variant === "dark"

  const handleFocus = (event: React.FocusEvent<HTMLTextAreaElement>) => {
    setFocused(true)
    onFocus?.(event)
  }

  const handleBlur = (event: React.FocusEvent<HTMLTextAreaElement>) => {
    setFocused(false)
    onBlur?.(event)
  }

  const handleChange = (event: React.ChangeEvent<HTMLTextAreaElement>) => {
    if (!isControlled) {
      setUncontrolledValue(event.target.value)
    }
    onChange?.(event)
  }

  return (
    <Field
      data-slot="floating-label-textarea"
      data-disabled={disabled ? true : undefined}
      data-invalid={error ? true : undefined}
      className={cn("gap-1.5", className)}
    >
      <div
        className={cn(
          "relative box-border flex w-full shrink-0 flex-col rounded-[4px] border border-[rgba(74,74,76,0.4)] px-[13px] py-[15px]",
          readOnly && (isDark ? "bg-[rgba(255,255,255,0.04)]" : "bg-[rgba(54,54,56,0.07)]"),
          disabled && "cursor-not-allowed opacity-50",
          error && "border-destructive",
          !disableFocusRing &&
            "focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50",
          error && !disableFocusRing && "focus-within:ring-destructive/20"
        )}
        style={{
          minHeight: TEXTAREA_MIN_HEIGHT,
        }}
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
          className="pointer-events-none absolute left-[13px] z-10 inline-flex origin-top-left items-center gap-1.5 text-sm leading-5 text-guest-feedback-placeholder"
        >
          <span>{label}</span>
          {optional ? (
            <span className="text-[10px] font-medium leading-[normal] text-[rgba(125,125,125,0.6)]">
              Optional
            </span>
          ) : null}
        </motion.label>

        <textarea
          ref={ref}
          {...props}
          id={inputId}
          rows={rows}
          value={isControlled ? value : uncontrolledValue}
          disabled={disabled}
          readOnly={readOnly}
          aria-invalid={error ? true : undefined}
          aria-describedby={
            [error ? errorId : null, notice ? noticeId : null]
              .filter(Boolean)
              .join(" ") || undefined
          }
          onFocus={handleFocus}
          onBlur={handleBlur}
          onChange={handleChange}
          className={cn(
            "min-h-[140px] w-full resize-none border-0 bg-transparent p-0 text-sm leading-5 outline-none",
            isDark ? "text-guest-feedback-text" : "text-[#141414]",
            readOnly && "cursor-default",
            hasActions && "pb-10"
          )}
          style={{
            paddingTop: INPUT_TEXT_TOP,
          }}
        />

        {hasActions ? (
          <div className="pointer-events-auto absolute bottom-[10px] right-[10px] z-10">
            {actions}
          </div>
        ) : null}
      </div>

      {notice ? (
        <p
          id={noticeId}
          className="text-xs font-medium leading-normal text-guest-feedback-muted"
        >
          {notice}
        </p>
      ) : null}

      <FieldErrorSlot
        id={errorId}
        error={error}
        reserveSpace={reserveSpace}
        reserveClassName={reserveClassName}
        className={error ? errorClassName : undefined}
      />
    </Field>
  )
})

FloatingLabelTextarea.displayName = "FloatingLabelTextarea"

export { FloatingLabelTextarea }
export type { FloatingLabelTextareaProps }
