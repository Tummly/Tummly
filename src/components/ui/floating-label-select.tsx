import * as React from "react"
import { motion, useReducedMotion } from "framer-motion"

import { cn } from "@/lib/utils"
import { Field, FieldError } from "@/components/ui/field"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

const TRIGGER_HEIGHT = 50
const LABEL_TOP = 8
const LABEL_REST_Y = 7
const VALUE_TOP = 22
const VALUE_HEIGHT = 20

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

type SelectOption = {
  value: string
  label: string
}

type FloatingLabelSelectProps = {
  label: string
  options: SelectOption[]
  error?: string
  disableFocusRing?: boolean
  className?: string
  disabled?: boolean
  value?: string
  defaultValue?: string
  onValueChange?: (value: string) => void
  name?: string
  required?: boolean
  id?: string
}

const FloatingLabelSelect = React.forwardRef<
  HTMLButtonElement,
  FloatingLabelSelectProps
>(function FloatingLabelSelect(
  {
    label,
    options,
    error,
    disableFocusRing = false,
    className,
    disabled,
    value,
    defaultValue,
    onValueChange,
    name,
    required,
    id,
  },
  ref
) {
  const shouldReduceMotion = useReducedMotion()
  const generatedId = React.useId()
  const triggerId = id ?? generatedId
  const errorId = `${triggerId}-error`

  const [open, setOpen] = React.useState(false)
  const [uncontrolledValue, setUncontrolledValue] = React.useState(
    defaultValue ?? ""
  )

  const isControlled = value !== undefined
  const currentValue = isControlled ? value : uncontrolledValue
  const hasValue = currentValue.length > 0
  const isActive = open || hasValue

  const handleValueChange = (nextValue: string) => {
    if (!isControlled) {
      setUncontrolledValue(nextValue)
    }
    onValueChange?.(nextValue)
  }

  const showOpenRing = open && !disableFocusRing

  return (
    <Field
      data-slot="floating-label-select"
      data-disabled={disabled ? true : undefined}
      data-invalid={error ? true : undefined}
      className={cn("gap-1.5", className)}
    >
      {name ? (
        <input
          type="hidden"
          name={name}
          value={currentValue}
          required={required}
        />
      ) : null}

      <Select
        value={hasValue ? currentValue : undefined}
        onValueChange={handleValueChange}
        open={open}
        onOpenChange={setOpen}
        disabled={disabled}
        required={required}
      >
        <SelectTrigger
          ref={ref}
          id={triggerId}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className={cn(
            "box-border flex w-full min-w-0 items-center gap-0.5 rounded-[4px] border border-[rgba(74,74,76,0.4)] bg-transparent px-[13px] shadow-none outline-none",
            "h-auto min-h-0 whitespace-normal hover:bg-transparent dark:bg-transparent dark:hover:bg-transparent",
            "data-[size=default]:!h-[50px]",
            disabled && "cursor-not-allowed opacity-50",
            error && "border-destructive",
            !disableFocusRing &&
              "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
            error && !disableFocusRing && "focus-visible:ring-destructive/20",
            showOpenRing && "border-ring ring-3 ring-ring/50",
            showOpenRing && error && "ring-destructive/20",
            "ring-0 data-[state=open]:ring-0",
            "disabled:cursor-not-allowed",
            "[&_[data-slot=select-value]]:line-clamp-1",
            "[&_svg]:text-[#7d7d7d]"
          )}
          style={{
            height: TRIGGER_HEIGHT,
            minHeight: TRIGGER_HEIGHT,
            maxHeight: TRIGGER_HEIGHT,
          }}
        >
          <div
            className="relative min-w-0 flex-1 shrink-0"
            style={{ height: TRIGGER_HEIGHT }}
          >
            <motion.span
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
              className="pointer-events-none absolute left-0 z-10 block origin-top-left text-sm leading-5 text-[#7d7d7d]"
            >
              {label}
            </motion.span>

            <div
              className={cn(
                "absolute left-0 z-20 w-full min-w-0 overflow-hidden text-left",
                !hasValue && "pointer-events-none invisible"
              )}
              style={{
                top: VALUE_TOP,
                height: VALUE_HEIGHT,
              }}
            >
              <SelectValue className="block truncate text-sm leading-5 text-[#141414]" />
            </div>
          </div>
        </SelectTrigger>

        <SelectContent
          position="popper"
          side="bottom"
          align="start"
          className="w-(--radix-select-trigger-width) min-w-(--radix-select-trigger-width) max-w-(--radix-select-trigger-width)"
        >
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {error ? <FieldError id={errorId}>{error}</FieldError> : null}
    </Field>
  )
})

FloatingLabelSelect.displayName = "FloatingLabelSelect"

export { FloatingLabelSelect }
export type { FloatingLabelSelectProps, SelectOption }
