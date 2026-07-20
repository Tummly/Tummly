import { describe, expect, it } from "vitest"
import type { UseFormReturn } from "react-hook-form"

import {
  applyWizardStepValidationFeedback,
  validateWizardStep,
} from "@/components/guest-loop/useGuestLoopStepCanSubmit"
import {
  accountSetupSingleDefaultValues,
  accountSetupSingleSchema,
  accountSetupSingleStep1Fields,
  accountSetupSingleStep1Schema,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"
import {
  accountSetupMultiDefaultValues,
  accountSetupMultiSchema,
  accountSetupMultiStep1Schema,
} from "@/schemas/accountSetupMulti"
import { validationMessages } from "@/schemas/messages"

function createMockForm<T extends Record<string, unknown>>(values: T) {
  const errors: Record<string, { message: string }> = {}

  return {
    getValues: () => values,
    clearErrors: (name: string) => {
      delete errors[name]
    },
    setError: (name: string, error: { message: string }) => {
      errors[name] = error
    },
    get formState() {
      return { errors }
    },
  } as unknown as UseFormReturn<T>
}

/** Values after invite prefill — step 1 complete, step 2 address fields still empty. */
const prefilledSingleStep1 = {
  ...accountSetupSingleDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  password: "Password1",
  confirmPassword: "Password1",
  agree: true,
  restaurantName: "The Golden Fork",
  locationName: "The Golden Fork",
  phone: "07911123456",
}

const prefilledMultiStep1 = {
  ...accountSetupMultiDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  password: "Password1",
  confirmPassword: "Password1",
  agree: true,
  groupName: "The Golden Fork Group",
  primaryPhone: "07911123456",
  numLocations: "2-5",
}

describe("step 1 vs full schema mismatch (setup account hang)", () => {
  it("single: step1 schema passes while full schema fails on empty step2 fields", () => {
    const step1 = accountSetupSingleStep1Schema.safeParse(prefilledSingleStep1)
    const full = accountSetupSingleSchema.safeParse(prefilledSingleStep1)

    expect(step1.success).toBe(true)
    expect(full.success).toBe(false)
    if (!full.success) {
      const paths = full.error.issues.map((i) => i.path[0])
      expect(paths).toContain("address")
    }
  })

  it("multi: step1 schema passes while full schema fails on empty step2/3 fields", () => {
    const step1 = accountSetupMultiStep1Schema.safeParse(prefilledMultiStep1)
    const full = accountSetupMultiSchema.safeParse(prefilledMultiStep1)

    expect(step1.success).toBe(true)
    expect(full.success).toBe(false)
  })
})

describe("validateWizardStep", () => {
  it("passes step 1 when only step-1 fields are validated", () => {
    const form = createMockForm<AccountSetupSingleFormValues>(prefilledSingleStep1)

    const valid = validateWizardStep(
      form,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema
    )

    expect(valid).toBe(true)
    expect(form.formState.errors).toEqual({})
  })

  it("surfaces step-1 errors without requiring later wizard fields", () => {
    const form = createMockForm<AccountSetupSingleFormValues>({
      ...prefilledSingleStep1,
      agree: false,
    })

    const valid = validateWizardStep(
      form,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema
    )

    expect(valid).toBe(false)
    expect(form.formState.errors.agree?.message).toBeTruthy()
  })
})

/** Invite prefill with passwords/terms still empty — step incomplete. */
const prefilledIncompleteStep1 = {
  ...accountSetupSingleDefaultValues,
  token: "setup-token",
  email: "operator@example.com",
  fullName: "Alex Operator",
  restaurantName: "The Golden Fork",
  locationName: "The Golden Fork",
  phone: "07911123456",
}

const skipPasswordFields = (fieldPath: string) =>
  fieldPath === "password" || fieldPath === "confirmPassword"

describe("applyWizardStepValidationFeedback", () => {
  it("clears sticky fullName error after editing prefill through a short value", () => {
    let values: AccountSetupSingleFormValues = {
      ...prefilledIncompleteStep1,
      fullName: "A",
    }
    const form = createMockForm(values)

    applyWizardStepValidationFeedback(
      form,
      values,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema,
      false,
      { shouldSkipValidationFeedback: skipPasswordFields }
    )

    expect(form.formState.errors.fullName?.message).toBe(
      validationMessages.accountSetup.fullName.required
    )

    values = {
      ...prefilledIncompleteStep1,
      fullName: "Alex Operator Edited",
    }

    applyWizardStepValidationFeedback(
      form,
      values,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema,
      false,
      { shouldSkipValidationFeedback: skipPasswordFields }
    )

    expect(form.formState.errors.fullName).toBeUndefined()
  })

  it("does not clear empty-field errors left by validateWizardStep", () => {
    const values: AccountSetupSingleFormValues = {
      ...prefilledIncompleteStep1,
      agree: false,
    }
    const form = createMockForm(values)

    validateWizardStep(
      form,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema
    )
    expect(form.formState.errors.agree?.message).toBeTruthy()

    applyWizardStepValidationFeedback(
      form,
      values,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema,
      false,
      { shouldSkipValidationFeedback: skipPasswordFields }
    )

    expect(form.formState.errors.agree?.message).toBeTruthy()
  })

  it("clears step-field errors when the step becomes complete", () => {
    const values: AccountSetupSingleFormValues = { ...prefilledSingleStep1 }
    const form = createMockForm(values)
    form.setError("fullName", {
      message: validationMessages.accountSetup.fullName.required,
    })

    applyWizardStepValidationFeedback(
      form,
      values,
      Array.from(accountSetupSingleStep1Fields),
      accountSetupSingleStep1Schema,
      true,
      { shouldSkipValidationFeedback: skipPasswordFields }
    )

    expect(form.formState.errors).toEqual({})
  })
})
