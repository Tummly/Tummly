import { MapPinIcon, ChevronDownIcon, Trash2Icon } from "lucide-react"
import type { UseFormReturn } from "react-hook-form"

import { FormFloatingInput } from "@/components/form/FormFloatingInput"
import { cn } from "@/lib/utils"
import type { AccountSetupMultiFormValues } from "@/schemas/accountSetupMulti"

type GuestLoopLocationCardProps = {
  form: UseFormReturn<AccountSetupMultiFormValues>
  index: number
  isExpanded: boolean
  canDelete: boolean
  onToggle: () => void
  onDelete: () => void
}

export function GuestLoopLocationCard({
  form,
  index,
  isExpanded,
  canDelete,
  onToggle,
  onDelete,
}: GuestLoopLocationCardProps) {
  const address = form.watch(`locations.${index}.address`)
  const showAddressPin = !address?.trim()

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <button
          type="button"
          onClick={onToggle}
          className="min-w-0 flex-1 cursor-pointer text-left text-lg font-semibold leading-5 tracking-[-0.36px] text-[#232323]"
          aria-expanded={isExpanded}
        >
          Location {index + 1}
        </button>

        <div className="flex shrink-0 items-center gap-2">
          {canDelete ? (
            <button
              type="button"
              onClick={onDelete}
              aria-label={`Delete location ${index + 1}`}
              className="flex size-8 cursor-pointer items-center justify-center rounded-full text-[#232323] transition-colors hover:text-red-600"
            >
              <Trash2Icon className="size-4" aria-hidden />
            </button>
          ) : null}

          <button
            type="button"
            onClick={onToggle}
            aria-label={isExpanded ? "Collapse location" : "Expand location"}
            className="flex size-8 cursor-pointer items-center justify-center rounded-full text-[#232323]"
          >
            <ChevronDownIcon
              className={cn(
                "size-4 transition-transform duration-200",
                isExpanded && "rotate-180"
              )}
              aria-hidden
            />
          </button>
        </div>
      </div>

      {isExpanded ? (
        <div className="flex flex-col gap-6">
          <FormFloatingInput
            control={form.control}
            name={`locations.${index}.locationName`}
            label="Location name"
            required
          />

          <div className="flex flex-col gap-6 sm:flex-row sm:gap-5">
            <div className="relative min-w-0 flex-1">
              {showAddressPin ? (
                <MapPinIcon
                  aria-hidden
                  className="pointer-events-none absolute left-[13px] top-4 z-10 size-[18px] text-[#7d7d7d]"
                />
              ) : null}
              <FormFloatingInput
                control={form.control}
                name={`locations.${index}.address`}
                label="Address"
                required
                className={cn(
                  showAddressPin &&
                  "[&_label]:left-[22px] [&_input]:pl-[22px]"
                )}
              />
            </div>

            <div className="min-w-0 flex-1">
              <FormFloatingInput
                control={form.control}
                name={`locations.${index}.postcode`}
                label="Postcode"
                required
                validateOnBlur
              />
            </div>
          </div>

          <FormFloatingInput
            control={form.control}
            name={`locations.${index}.locationPhone`}
            label="Location phone"
            type="tel"
            autoComplete="tel"
            optional
          />

          <FormFloatingInput
            control={form.control}
            name={`locations.${index}.localContact`}
            label="Local contact"
            optional
          />
        </div>
      ) : null}
    </div>
  )
}
