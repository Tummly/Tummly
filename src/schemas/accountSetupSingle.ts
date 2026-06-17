import { z } from "zod"

import { validationMessages } from "@/schemas/messages"
import {
  emailSchema,
  mobileSchema,
  optionalUrlSchema,
  passwordSchema,
} from "@/schemas/primitives"
import type { CompleteSetupPayload } from "@/types/trial"

// Strict UK Postcode regex validation pattern matching London client spec perfectly
const ukPostcodeRegex = /^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$/i;

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

export const accountSetupSingleStep3Fields = [
  "thankYouMessage",
] as const

export const accountSetupSingleSchema = z
  .object({
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
      
    // 🛠️ TWEAK 1: Strict UK Postcode check - Handles empty string nicely during validation
    postcode: z.string()
      .min(1, "Postcode is required")
      .regex(ukPostcodeRegex, "Please enter a valid UK postcode (e.g., SW1A 1AA)"),
      
    phone: mobileSchema,
    businessLink: optionalUrlSchema,
    
    // 🛠️ TWEAK 2: Dropdown constraint mapping to ensure Radix/Shadcn select doesn't throw uncontrolled errors
    businessCategory: z
      .string()
      .min(1, validationMessages.accountSetup.businessCategory.required),
      
    touchpoints: z.array(z.string()),
    feedbackTags: z.array(z.string()),
    thankYouMessage: z
      .string()
      .trim()
      .min(1, validationMessages.accountSetup.thankYouMessage.required),
    offerHeadline: z.string(),
    offerDetails: z.string(),
    offerExpiry: z.string(),
    offerRedemption: z.string(),
    offerUsageLimit: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: validationMessages.password.mismatch,
    path: ["confirmPassword"],
  })

// ⚠️ Dynamic structural alignment mapping fixes
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
  phone: "",
  businessLink: "",
  businessCategory: "Restaurant", // 👈 Pre-filled default safe key to prevent dropdown crash on step 2 load
  touchpoints: [],
  feedbackTags: [],
  thankYouMessage: "Thank you for sharing your feedback with us!", // 👈 Safe fallback default string
  offerHeadline: "",
  offerDetails: "",
  offerExpiry: "",
  offerRedemption: "",
  offerUsageLimit: "",
}

export function toSingleLocationSetupPayload(
  values: AccountSetupSingleFormValues
): CompleteSetupPayload {
  const parsed = accountSetupSingleSchema.parse(values)

  return {
    token: parsed.token,
    password: parsed.password,
    confirmPassword: parsed.confirmPassword,
    groupName: parsed.restaurantName,
    businessCategory: parsed.businessCategory,
    primaryPhone: parsed.phone,
    businessLink: parsed.businessLink.trim() || undefined,
    locations: [
      {
        locationName: parsed.locationName,
        address: parsed.address,
        postcode: parsed.postcode.trim() || undefined,
        locationPhone: parsed.phone,
        localContact: parsed.fullName,
        includeInRollout: true,
      },
    ],
    rolloutApproach: "Single",
    touchpoints: parsed.touchpoints.join(", "),
    feedbackTags: parsed.feedbackTags.join(", "),
    guestPrompt: "Please leave feedback",
    thankYouMessage: parsed.thankYouMessage,
    offerType: "Single",
    offerTitle: parsed.offerHeadline,
    offerMessage: parsed.offerDetails,
    offerExpiry: parsed.offerExpiry,
    redemptionMethod: parsed.offerRedemption,
    usageLimit: parsed.offerUsageLimit,
  }
}