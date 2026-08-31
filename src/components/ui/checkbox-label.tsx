import * as React from "react"

import { Checkbox } from "@/components/ui/checkbox"
import { Field, FieldErrorSlot } from "@/components/ui/field"
import { Label } from "@/components/ui/label"
import { cn } from "@/lib/utils"

type CheckboxLabelProps = {
  id?: string
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  children: React.ReactNode
  error?: string
  disabled?: boolean
  className?: string
  labelClassName?: string
  reserveSpace?: boolean
  reserveClassName?: string
  /** `ghost` — low-contrast tick/border for dark guest-feedback surfaces. */
  variant?: "default" | "ghost"
}

function CheckboxLabel({
  id,
  checked,
  onCheckedChange,
  children,
  error,
  disabled,
  className,
  labelClassName,
  reserveSpace = false,
  reserveClassName,
  variant = "default",
}: CheckboxLabelProps) {
  const generatedId = React.useId()
  const checkboxId = id ?? generatedId
  const errorId = `${checkboxId}-error`

  return (
    <Field
      data-invalid={error ? true : undefined}
      className={cn("gap-1.5", className)}
    >
      <div className="flex items-center gap-2">
        <Checkbox
          id={checkboxId}
          checked={checked}
          onCheckedChange={(value) => onCheckedChange(value === true)}
          disabled={disabled}
          variant={variant}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className={cn(
            variant === "default"
            && "size-[18px] rounded-[2px] border-[rgba(74,74,76,0.3)] bg-white data-checked:border-[#14a247] data-checked:bg-[#14a247] dark:border-op-checkbox-border dark:bg-transparent dark:data-checked:border-[#14a247] dark:data-checked:bg-[#14a247]"
          )}
        />
        <Label
          htmlFor={checkboxId}
          className={cn(
            "text-sm font-medium leading-[normal] text-[#141414] dark:text-op-text-primary",
            labelClassName
          )}
        >
          {children}
        </Label>
      </div>
      <FieldErrorSlot
        id={errorId}
        error={error}
        reserveSpace={reserveSpace}
        reserveClassName={reserveClassName}
      />
    </Field>
  )
}

export { CheckboxLabel }
export type { CheckboxLabelProps }
