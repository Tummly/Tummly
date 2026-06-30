import { z } from "zod"

import { validationMessages } from "@/schemas/messages"
import {
  emailSchema,
  optionalMobileSchema,
  optionalUrlSchema,
  passwordSchema,
  passwordsMatchWhenConfirmFilled,
} from "@/schemas/primitives"
import type { CompleteSetupPayload } from "@/types/trial"

const ukPostcodeRegex = /^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$/i

export const accountSetupSingleStep1Fields = [
  "email",
  "fullName",
  "password",
  "confirmPassword",
  "agree",
] as const

export const accountSetupSingleStep2Fields = [
  "restaurantName",
  "locationName",
  "address",
  "postcode",
  "phone",
  "businessLink",
  "businessCategory",
] as const

const passwordMatchRefine = {
  message: validationMessages.password.mismatch,
  path: ["confirmPassword"],
}

const accountSetupSingleBaseSchema = z.object({
  token: z.string().min(1),
  email: emailSchema,
  fullName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.fullName.required)
    .min(2, validationMessages.accountSetup.fullName.required)
    .max(100),
  password: passwordSchema,
  confirmPassword: z.string().min(1, validationMessages.password.required),
  agree: z.boolean().refine((value) => value === true, {
    message: validationMessages.accountSetup.terms.required,
  }),
  restaurantName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.restaurantName.required),
  locationName: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.locationName.required),
  address: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.address.required),
  postcode: z
    .string()
    .trim()
    .min(1, validationMessages.accountSetup.postcode.required)
    .regex(ukPostcodeRegex, validationMessages.accountSetup.postcode.invalid),
  addressOverridden: z.boolean().optional(),
  phone: optionalMobileSchema,
  businessLink: optionalUrlSchema,
  businessCategory: z
    .string()
    .min(1, validationMessages.accountSetup.businessCategory.required),
})

export const accountSetupSingleStep1Schema = accountSetupSingleBaseSchema
  .pick({
    email: true,
    fullName: true,
    password: true,
    confirmPassword: true,
    agree: true,
  })
  .refine(
    (data) =>
      passwordsMatchWhenConfirmFilled(data.password, data.confirmPassword),
    passwordMatchRefine
  )

export const accountSetupSingleStep2Schema = accountSetupSingleBaseSchema.pick({
  restaurantName: true,
  locationName: true,
  address: true,
  postcode: true,
  phone: true,
  businessLink: true,
  businessCategory: true,
})

export const accountSetupSingleSchema = accountSetupSingleBaseSchema.refine(
  (data) =>
    passwordsMatchWhenConfirmFilled(data.password, data.confirmPassword),
  passwordMatchRefine
)

export type AccountSetupSingleFormValues = z.input<
  typeof accountSetupSingleSchema
>

export const accountSetupSingleDefaultValues: AccountSetupSingleFormValues = {
  token: "",
  email: "",
  fullName: "",
  password: "",
  confirmPassword: "",
  agree: false,
  restaurantName: "",
  locationName: "",
  address: "",
  postcode: "",
  addressOverridden: false,
  phone: "",
  businessLink: "",
  businessCategory: "takeaway",
}

export function toSingleLocationSetupPayload(
  values: AccountSetupSingleFormValues
): CompleteSetupPayload {
  const parsed = accountSetupSingleSchema.parse(values)

  return {
    token: parsed.token,
    fullName: parsed.fullName,
    password: parsed.password,
    confirmPassword: parsed.confirmPassword,
    groupName: parsed.restaurantName,
    businessCategory: parsed.businessCategory,
    primaryPhone: parsed.phone || undefined,
    businessLink: parsed.businessLink.trim() || undefined,
    locations: [
      {
        locationName: parsed.locationName,
        address: parsed.address,
        postcode: parsed.postcode.trim() || undefined,
        locationPhone: parsed.phone || undefined,
        localContact: parsed.fullName,
        ...(parsed.addressOverridden ? { addressOverridden: true } : {}),
      },
    ],
  }
}
