import { Suspense, lazy, useState } from "react"
import { CirclePlusIcon, UploadIcon } from "lucide-react"
import { useFieldArray, type UseFormReturn } from "react-hook-form"

import {
  accountSetupMultiStep3Schema,
  emptyLocationItem,
  type AccountSetupMultiFormValues,
  type LocationFormItem,
} from "@/schemas/accountSetupMulti"
import { Button } from "@/components/ui/button"

import { GuestLoopLocationCard } from "./GuestLoopLocationCard"
import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import { GUEST_LOOP_MULTI_STEPS, type GuestLoopProgressStep } from "./guestLoopSteps"
import { useGuestLoopStepCanSubmit, useGuestLoopStepValidationFeedback } from "./useGuestLoopStepCanSubmit"

const GuestLoopUploadLocationsDialog = lazy(() =>
  import("./GuestLoopUploadLocationsDialog").then((module) => ({
    default: module.GuestLoopUploadLocationsDialog,
  }))
)

type GuestLoopLocationsStepProps = {
  form: UseFormReturn<AccountSetupMultiFormValues>
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  onContinue: () => void | Promise<void>
  isSubmitting?: boolean
}

export function GuestLoopLocationsStep({
  form,
  activeStep,
  steps = GUEST_LOOP_MULTI_STEPS,
  onContinue,
  isSubmitting = false,
}: GuestLoopLocationsStepProps) {
  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: "locations",
  })

  const [collapsedByIndex, setCollapsedByIndex] = useState<
    Record<number, boolean>
  >(() => ({}))
  const [isUploadDialogOpen, setIsUploadDialogOpen] = useState(false)
  const [hasUploadDialogBeenOpened, setHasUploadDialogBeenOpened] =
    useState(false)
  const [isUploadSubmitting, setIsUploadSubmitting] = useState(false)

  const canContinue = useGuestLoopStepCanSubmit(
    form,
    [],
    accountSetupMultiStep3Schema,
    {
      selectStepValues: (values) => ({ locations: values.locations }),
    }
  )

  useGuestLoopStepValidationFeedback(
    form,
    [],
    accountSetupMultiStep3Schema,
    canContinue,
    {
      selectStepValues: (values) => ({ locations: values.locations }),
      shouldSkipValidationFeedback: (fieldPath) =>
        fieldPath.endsWith(".postcode"),
    }
  )

  const handleAddLocation = () => {
    append(emptyLocationItem)
  }

  const handleDeleteLocation = (index: number) => {
    if (fields.length <= 1) {
      return
    }

    remove(index)
    setCollapsedByIndex((current) => {
      const next: Record<number, boolean> = {}
      let writeIndex = 0

      for (let readIndex = 0; readIndex < fields.length; readIndex += 1) {
        if (readIndex === index) {
          continue
        }

        if (current[readIndex]) {
          next[writeIndex] = true
        }
        writeIndex += 1
      }

      return next
    })
  }

  const handleOpenUploadDialog = () => {
    setHasUploadDialogBeenOpened(true)
    setIsUploadDialogOpen(true)
  }

  const handleUploadConfirm = async (locations: LocationFormItem[]) => {
    setIsUploadSubmitting(true)

    try {
      form.setValue("locations", locations, {
        shouldDirty: true,
        shouldValidate: true,
      })
      setCollapsedByIndex(
        Object.fromEntries(locations.map((_, index) => [index, index !== 0]))
      )
      setIsUploadDialogOpen(false)
      await onContinue()
    } finally {
      setIsUploadSubmitting(false)
    }
  }

  return (
    <div className="flex w-full flex-col gap-8 sm:gap-10">
      <GuestLoopStepHeader
        title="Add your locations"
        description="Add the locations you want to include in your first Tummly rollout. You can add more locations later from your workspace."
      />

      <div className="flex flex-col gap-6">
        {fields.map((field, index) => (
          <GuestLoopLocationCard
            key={field.id}
            form={form}
            index={index}
            isExpanded={!collapsedByIndex[index]}
            canDelete={fields.length > 1}
            onToggle={() =>
              setCollapsedByIndex((current) => ({
                ...current,
                [index]: !current[index],
              }))
            }
            onDelete={() => handleDeleteLocation(index)}
          />
        ))}

        <div className="h-px w-full bg-[#dfdfdf]" aria-hidden />

        <div className="flex flex-col items-start gap-3.5">
          <Button
            type="button"
            variant="ghost"
            size="link-sm"
            onClick={handleAddLocation}
            className="justify-start gap-2.5 font-semibold text-[#232323] hover:bg-transparent hover:opacity-80"
          >
            <CirclePlusIcon className="size-4 shrink-0" aria-hidden />
            Add location
          </Button>

          <Button
            type="button"
            variant="ghost"
            size="link-sm"
            onClick={handleOpenUploadDialog}
            className="justify-start gap-2.5 font-semibold text-[#232323] hover:bg-transparent hover:opacity-80"
          >
            <UploadIcon className="size-4 shrink-0" aria-hidden />
            Upload locations instead
          </Button>
        </div>
      </div>

      <GuestLoopStepFooter steps={steps} activeStep={activeStep}>
        <GuestLoopStepButton
          enabled={canContinue}
          isSubmitting={isSubmitting}
          onClick={onContinue}
        >
          Continue to rollout
        </GuestLoopStepButton>
      </GuestLoopStepFooter>

      {hasUploadDialogBeenOpened ? (
        <Suspense fallback={null}>
          <GuestLoopUploadLocationsDialog
            open={isUploadDialogOpen}
            onOpenChange={setIsUploadDialogOpen}
            onConfirm={handleUploadConfirm}
            isSubmitting={isUploadSubmitting || isSubmitting}
          />
        </Suspense>
      ) : null}
    </div>
  )
}
