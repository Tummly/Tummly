import type { Control, FieldPath, FieldValues } from "react-hook-form"
import { useFormContext } from "react-hook-form"

import { useWizardLiveValidation } from "@/components/form/WizardLiveValidationContext"
import {
  FloatingLabelSelect,
  type FloatingLabelSelectProps,
} from "@/components/ui/floating-label-select"
import { FormControl, FormField, FormItem } from "@/components/ui/form"

type FormFloatingSelectProps<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
> = Omit<
  FloatingLabelSelectProps,
  "value" | "defaultValue" | "onValueChange" | "name" | "error"
> & {
  control: Control<TFieldValues>
  name: TName
  liveValidate?: boolean
}

function FormFloatingSelect<
  TFieldValues extends FieldValues,
  TName extends FieldPath<TFieldValues>,
>({
  control,
  name,
  liveValidate,
  ...selectProps
}: FormFloatingSelectProps<TFieldValues, TName>) {
  const { trigger } = useFormContext<TFieldValues>()
  const contextLiveValidate = useWizardLiveValidation(String(name))
  const shouldLiveValidate = liveValidate ?? contextLiveValidate

  return (
    <FormField
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <FormItem className="w-full gap-0">
          <FormControl>
            <FloatingLabelSelect
              {...selectProps}
              name={field.name}
              value={field.value ?? ""}
              onValueChange={(value) => {
                field.onChange(value)
                if (shouldLiveValidate) {
                  void trigger(name)
                }
              }}
              ref={field.ref}
              error={fieldState.error?.message}
            />
          </FormControl>
        </FormItem>
      )}
    />
  )
}

export { FormFloatingSelect }
export type { FormFloatingSelectProps }
