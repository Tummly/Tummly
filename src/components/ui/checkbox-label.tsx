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
}: CheckboxLabelProps) {
  const generatedId = React.useId()
  const checkboxId = id ?? generatedId
  const errorId = `${checkboxId}-error`

  return (
    <Field
      data-invalid={error ? true : undefined}
      className={cn("gap-1.5", className)}
    >
      <div className="flex items-start gap-2">
        <Checkbox
          id={checkboxId}
          checked={checked}
          onCheckedChange={(value) => onCheckedChange(value === true)}
          disabled={disabled}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className="mt-0.5 size-[18px] rounded-[2px] border-[rgba(74,74,76,0.3)] bg-white data-checked:border-[#14a247] data-checked:bg-[#14a247] dark:bg-white dark:data-checked:bg-[#14a247]"
        />
        <Label
          htmlFor={checkboxId}
          className={cn(
            "items-start text-sm font-medium leading-[normal] text-[#141414]",
            labelClassName
          )}
        >
          {children}
        </Label>
      </div>
      <FieldErrorSlot
        id={errorId}
        error={error}
        reserveClassName="min-h-10"
      />
    </Field>
  )
}

export { CheckboxLabel }
export type { CheckboxLabelProps }
