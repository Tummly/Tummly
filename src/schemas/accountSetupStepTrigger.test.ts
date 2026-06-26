import { describe, expect, it } from "vitest"
import type { UseFormReturn } from "react-hook-form"

import { validateWizardStep } from "@/components/guest-loop/useGuestLoopStepCanSubmit"
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
