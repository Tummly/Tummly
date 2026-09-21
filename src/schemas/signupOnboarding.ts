import { z } from "zod"

import type { SignupOnboardingPayload } from "@/api/signupApi"
import { validationMessages } from "@/schemas/messages"
import {
  emailSchema,
  optionalMobileSchema,
  optionalUrlSchema,
  passwordSchema,
  passwordsMatchWhenConfirmFilled,
} from "@/schemas/primitives"

const ukPostcodeRegex = /^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$/i

const passwordMatchRefine = {
  message: validationMessages.password.mismatch,
  path: ["confirmPassword"] as const,
}

export function joinSignupFullName(firstName: string, lastName: string): string {
  const first = firstName.trim()
  const last = lastName.trim()
  if (first.length === 0) {
    return last
  }
  if (last.length === 0) {
    return first
  }
  return `${first} ${last}`
}

export const signupAccountStepFields = [
  "email",
  "firstName",
  "lastName",
  "password",
  "confirmPassword",
] as const

export const signupRestaurantStepFields = [
  "restaurantName",
  "businessCategory",
  "businessLink",
  "phone",
] as const

export const signupLocationStepFields = [
  "locationName",
  "address",
  "city",
  "postcode",
] as const

const signupOnboardingBaseSchema = z.object({
  token: z.string().min(1),
  email: emailSchema,
  firstName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.fullName.required),
  lastName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.fullName.required),
  password: passwordSchema,
  confirmPassword: z.string().min(1, validationMessages.password.required),
  restaurantName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.restaurantName.required),
  businessCategory: z
    .string()
    .min(1, validationMessages.accountSetup.businessCategory.required),
  businessLink: optionalUrlSchema,
  phone: optionalMobileSchema,
  locationName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.locationName.required),
  address: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.address.required),
  city: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.city.required),
  postcode: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.postcode.required)
    .regex(ukPostcodeRegex, validationMessages.accountSetup.postcode.invalid),
  addressOverridden: z.boolean().optional(),
  country: z.string(),
  timezone: z.string(),
})

export const signupAccountStepSchema = signupOnboardingBaseSchema
  .pick({
    email: true,
    firstName: true,
    lastName: true,
    password: true,
    confirmPassword: true,
  })
  .refine(
    (data) =>
      passwordsMatchWhenConfirmFilled(data.password, data.confirmPassword),
    passwordMatchRefine
  )

export const signupRestaurantStepSchema = signupOnboardingBaseSchema.pick({
  restaurantName: true,
  businessCategory: true,
  businessLink: true,
  phone: true,
})

export const signupLocationStepSchema = signupOnboardingBaseSchema.pick({
  locationName: true,
  address: true,
  city: true,
  postcode: true,
})

export const signupOnboardingSchema = signupOnboardingBaseSchema.refine(
  (data) =>
    passwordsMatchWhenConfirmFilled(data.password, data.confirmPassword),
  passwordMatchRefine
)

export type SignupOnboardingFormValues = z.input<typeof signupOnboardingSchema>

export const signupOnboardingDefaultValues: SignupOnboardingFormValues = {
  token: "",
  email: "",
  firstName: "",
  lastName: "",
  password: "",
  confirmPassword: "",
  restaurantName: "",
  businessCategory: "takeaway",
  businessLink: "",
  phone: "",
  locationName: "",
  address: "",
  city: "",
  postcode: "",
  addressOverridden: false,
  country: "United Kingdom",
  timezone: "Europe/London",
}

export function toSignupOnboardingPayload(
  values: SignupOnboardingFormValues
): SignupOnboardingPayload {
  const parsed = signupOnboardingSchema.parse(values)
  const fullName = joinSignupFullName(parsed.firstName, parsed.lastName)

  return {
    password: parsed.password,
    confirmPassword: parsed.confirmPassword,
    fullName,
    groupName: parsed.restaurantName,
    businessCategory: parsed.businessCategory,
    primaryPhone: parsed.phone || undefined,
    businessLink: parsed.businessLink.trim() || undefined,
    locations: [
      {
        locationName: parsed.locationName,
        address: parsed.address,
        city: parsed.city.trim() || undefined,
        postcode: parsed.postcode.trim() || undefined,
        locationPhone: parsed.phone || undefined,
        localContact: fullName,
        ...(parsed.addressOverridden ? { addressOverridden: true } : {}),
      },
    ],
  }
}
