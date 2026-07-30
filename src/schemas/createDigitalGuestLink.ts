import { z } from "zod"

import { OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY } from "@/lib/operatorCapture/capturePresentation"
import type {
  CaptureDigitalGuestLinkChannel,
  CapturePlacementStatus,
} from "@/types/dashboard"

const copy = OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY

const channelValues = [
  "SocialMedia",
  "Email",
  "WhatsApp",
  "Website",
  "OnlineOrdering",
  "Other",
] as const satisfies readonly CaptureDigitalGuestLinkChannel[]

const statusValues = ["Active", "Paused"] as const satisfies readonly CapturePlacementStatus[]

export const createDigitalGuestLinkFormSchema = z.object({
  linkName: z
    .string()
    .trim()
    .min(1, copy.linkNameRequired)
    .max(copy.linkNameMaxLength, copy.linkNameMax),
  internalDescription: z
    .string()
    .max(copy.internalDescriptionMaxLength, copy.internalDescriptionMax),
  channel: z
    .union([z.enum(channelValues), z.literal("")])
    .refine((value) => value !== "", { message: copy.channelRequired }),
  status: z.enum(statusValues),
  locationId: z.number().nullable().optional(),
})

export type CreateDigitalGuestLinkFormValues = z.infer<
  typeof createDigitalGuestLinkFormSchema
>

export function createDigitalGuestLinkFormSchemaWithLocation(
  requireLocation: boolean
) {
  if (!requireLocation) {
    return createDigitalGuestLinkFormSchema
  }
  return createDigitalGuestLinkFormSchema.superRefine((data, ctx) => {
    if (data.locationId == null) {
      ctx.addIssue({
        code: "custom",
        message: copy.locationRequired,
        path: ["locationId"],
      })
    }
  })
}
