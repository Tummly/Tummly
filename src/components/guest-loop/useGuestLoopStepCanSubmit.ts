import { useEffect, useState } from "react"
import type { FieldPath, UseFormReturn } from "react-hook-form"
import type { z } from "zod"

import type { AccountSetupSingleFormValues } from "@/schemas/accountSetupSingle"

function pickStepValues(
  values: AccountSetupSingleFormValues,
  fields: readonly FieldPath<AccountSetupSingleFormValues>[]
) {
  return Object.fromEntries(
    fields.map((field) => [field, values[field]])
  ) as Record<string, unknown>
}

export function useGuestLoopStepCanSubmit(
  form: UseFormReturn<AccountSetupSingleFormValues>,
  fields: readonly FieldPath<AccountSetupSingleFormValues>[],
  stepSchema: z.ZodType
) {
  const [canSubmit, setCanSubmit] = useState(false)

  useEffect(() => {
    const fieldSet = new Set(fields as readonly string[])

    const validate = () => {
      const stepValues = pickStepValues(form.getValues(), fields)
      setCanSubmit(stepSchema.safeParse(stepValues).success)
    }

    validate()

    const subscription = form.watch((_value, { name }) => {
      if (name && !fieldSet.has(name)) {
        return
      }

      validate()
    })

    return () => subscription.unsubscribe()
  }, [form, fields, stepSchema])

  return canSubmit
}
