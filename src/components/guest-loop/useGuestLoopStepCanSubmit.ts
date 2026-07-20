import { useEffect, useMemo, useRef } from "react"
import {
  get,
  useWatch,
  type FieldPath,
  type FieldValues,
  type UseFormReturn,
} from "react-hook-form"
import type { z } from "zod"

export type ValidateWizardStepOptions<TFieldValues extends FieldValues> = {
  selectStepValues?: (values: TFieldValues) => unknown
}

function pickStepValues<TFieldValues extends FieldValues>(
  values: TFieldValues,
  fields: readonly FieldPath<TFieldValues>[]
) {
  return Object.fromEntries(
    fields.map((field) => [field, get(values, field)])
  )
}

function issuePathToFieldPath(path: PropertyKey[]) {
  return path.map(String).join(".")
}

function fieldHasContent(value: unknown) {
  return typeof value === "string" && value.trim().length > 0
}

type UseGuestLoopStepOptions<TFieldValues extends FieldValues> =
  ValidateWizardStepOptions<TFieldValues> & {
    /** Fields that validate on blur only — skip auto-trigger while typing. */
    shouldSkipValidationFeedback?: (fieldPath: string) => boolean
  }

/**
 * Validates only the current wizard step schema and maps Zod issues onto RHF
 * fields. Use this instead of `form.trigger` for step navigation — the form
 * resolver runs the full schema and blocks advance when later steps are empty.
 */
export function validateWizardStep<TFieldValues extends FieldValues>(
  form: UseFormReturn<TFieldValues>,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  options?: ValidateWizardStepOptions<TFieldValues>
): boolean {
  const values = form.getValues()
  const stepValues = options?.selectStepValues
    ? options.selectStepValues(values)
    : pickStepValues(values, fields)
  const result = stepSchema.safeParse(stepValues)

  for (const field of fields) {
    form.clearErrors(field)
  }

  if (result.success) {
    return true
  }

  for (const issue of result.error.issues) {
    const fieldPath = issuePathToFieldPath(
      issue.path
    ) as FieldPath<TFieldValues>

    form.setError(fieldPath, {
      type: "custom",
      message: issue.message,
    })
  }

  return false
}

export function useGuestLoopStepCanSubmit<TFieldValues extends FieldValues>(
  form: UseFormReturn<TFieldValues>,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  options?: UseGuestLoopStepOptions<TFieldValues>
) {
  const { selectStepValues } = options ?? {}
  const watchedValues = useWatch({ control: form.control }) as
    | TFieldValues
    | undefined

  return useMemo(() => {
    const values = watchedValues ?? form.getValues()
    const stepValues = selectStepValues
      ? selectStepValues(values)
      : pickStepValues(values, fields)

    return stepSchema.safeParse(stepValues).success
  }, [watchedValues, fields, form, selectStepValues, stepSchema])
}

/**
 * Surfaces step-schema failures on fields that already have input, so the CTA
 * gate and inline errors stay in sync when liveValidate has not fired yet.
 * Also clears errors on fields that have recovered (e.g. prefilled name edited
 * through a short intermediate value) so sticky "required" messages do not linger.
 */
export function applyWizardStepValidationFeedback<
  TFieldValues extends FieldValues,
>(
  form: UseFormReturn<TFieldValues>,
  values: TFieldValues,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  isStepComplete: boolean,
  options?: UseGuestLoopStepOptions<TFieldValues>
) {
  const { selectStepValues, shouldSkipValidationFeedback } = options ?? {}

  if (isStepComplete) {
    for (const field of fields) {
      form.clearErrors(field)
    }
    return
  }

  const stepValues = selectStepValues
    ? selectStepValues(values)
    : pickStepValues(values, fields)
  const result = stepSchema.safeParse(stepValues)

  const issueMessagesByField = new Map<string, string>()
  if (!result.success) {
    for (const issue of result.error.issues) {
      const fieldPath = issuePathToFieldPath(issue.path)
      if (!issueMessagesByField.has(fieldPath)) {
        issueMessagesByField.set(fieldPath, issue.message)
      }
    }
  }

  for (const field of fields) {
    const fieldPath = String(field)

    if (shouldSkipValidationFeedback?.(fieldPath)) {
      continue
    }

    const message = issueMessagesByField.get(fieldPath)
    const fieldValue = get(values, field)

    if (message && fieldHasContent(fieldValue)) {
      form.setError(field, {
        type: "custom",
        message,
      })
    } else if (!message) {
      form.clearErrors(field)
    }
  }
}

export function useGuestLoopStepValidationFeedback<
  TFieldValues extends FieldValues,
>(
  form: UseFormReturn<TFieldValues>,
  fields: readonly FieldPath<TFieldValues>[],
  stepSchema: z.ZodType,
  isStepComplete: boolean,
  options?: UseGuestLoopStepOptions<TFieldValues>
) {
  const { selectStepValues, shouldSkipValidationFeedback } = options ?? {}
  const shouldSkipValidationFeedbackRef = useRef(shouldSkipValidationFeedback)
  shouldSkipValidationFeedbackRef.current = shouldSkipValidationFeedback

  const watchedValues = useWatch({ control: form.control }) as
    | TFieldValues
    | undefined

  useEffect(() => {
    const values = watchedValues ?? form.getValues()

    applyWizardStepValidationFeedback(
      form,
      values,
      fields,
      stepSchema,
      isStepComplete,
      {
        selectStepValues,
        shouldSkipValidationFeedback: shouldSkipValidationFeedbackRef.current,
      }
    )
  }, [
    watchedValues,
    isStepComplete,
    form,
    fields,
    stepSchema,
    selectStepValues,
  ])
}
