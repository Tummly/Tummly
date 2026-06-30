import {
  type Control,
  type FieldPath,
  type FieldValues,
  useFormContext,
  useWatch,
} from "react-hook-form"

import { TrialMainLocationFields } from "@/components/home/TrialMainLocationFields"
import { FormItem } from "@/components/ui/form"

function getFieldErrorMessage(
  errors: Record<string, unknown>,
  path: string
): string | undefined {
  const segments = path.split(".")
  let current: unknown = errors

  for (const segment of segments) {
    if (!current || typeof current !== "object") {
      return undefined
    }

    current = (current as Record<string, unknown>)[segment]
  }

  if (
    current &&
    typeof current === "object" &&
    "message" in current &&
    typeof (current as { message?: unknown }).message === "string"
  ) {
    return (current as { message: string }).message
  }

  return undefined
}

type FormTrialMainLocationFieldsProps<
  TFieldValues extends FieldValues,
  TMainLocationName extends FieldPath<TFieldValues>,
  TTownCityName extends FieldPath<TFieldValues>,
  TPostcodeName extends FieldPath<TFieldValues>,
  TCommittedName extends FieldPath<TFieldValues>,
  TManualName extends FieldPath<TFieldValues>,
> = {
  control: Control<TFieldValues>
  mainLocationName: TMainLocationName
  townCityName: TTownCityName
  postcodeName: TPostcodeName
  committedName: TCommittedName
  manualName: TManualName
  disableFocusRing?: boolean
  reserveSpace?: boolean
  errorClassName?: string
}

export function FormTrialMainLocationFields<
  TFieldValues extends FieldValues,
  TMainLocationName extends FieldPath<TFieldValues>,
  TTownCityName extends FieldPath<TFieldValues>,
  TPostcodeName extends FieldPath<TFieldValues>,
  TCommittedName extends FieldPath<TFieldValues>,
  TManualName extends FieldPath<TFieldValues>,
>({
  control,
  mainLocationName,
  townCityName,
  postcodeName,
  committedName,
  manualName,
  disableFocusRing,
  reserveSpace,
  errorClassName,
}: FormTrialMainLocationFieldsProps<
  TFieldValues,
  TMainLocationName,
  TTownCityName,
  TPostcodeName,
  TCommittedName,
  TManualName
>) {
  const { setValue, clearErrors, formState } = useFormContext<TFieldValues>()
  const mainLocation = useWatch({ control, name: mainLocationName })
  const townCity = useWatch({ control, name: townCityName })
  const postcode = useWatch({ control, name: postcodeName })
  const committed = useWatch({ control, name: committedName })
  const manual = useWatch({ control, name: manualName })

  const addressFieldNames = [
    mainLocationName,
    townCityName,
    postcodeName,
    committedName,
  ] as FieldPath<TFieldValues>[]

  const setAddressField = (
    name: FieldPath<TFieldValues>,
    value: string | boolean
  ) => {
    setValue(name, value as never, {
      shouldDirty: true,
      shouldValidate: formState.isSubmitted,
    })
  }

  return (
    <FormItem className="w-full gap-0">
      <TrialMainLocationFields
        mainLocation={String(mainLocation ?? "")}
        townCity={String(townCity ?? "")}
        postcode={String(postcode ?? "")}
        committed={Boolean(committed)}
        manual={Boolean(manual)}
        disableFocusRing={disableFocusRing}
        reserveSpace={reserveSpace}
        errorClassName={errorClassName}
        mainLocationError={getFieldErrorMessage(
          formState.errors as Record<string, unknown>,
          String(mainLocationName)
        )}
        townCityError={getFieldErrorMessage(
          formState.errors as Record<string, unknown>,
          String(townCityName)
        )}
        postcodeError={getFieldErrorMessage(
          formState.errors as Record<string, unknown>,
          String(postcodeName)
        )}
        onMainLocationChange={(value) => {
          setAddressField(mainLocationName, value)
        }}
        onTownCityChange={(value) => {
          setAddressField(townCityName, value)
        }}
        onPostcodeChange={(value) => {
          setAddressField(postcodeName, value)
        }}
        onCommittedChange={() => {
          setAddressField(committedName, true)
        }}
        onManualChange={(value) => {
          setAddressField(manualName, value)
        }}
        onResolvedAddressApply={({ mainLocation, townCity, postcode }) => {
          setValue(mainLocationName, mainLocation as never, {
            shouldDirty: true,
            shouldValidate: false,
          })
          setValue(townCityName, townCity as never, {
            shouldDirty: true,
            shouldValidate: false,
          })
          setValue(postcodeName, postcode as never, {
            shouldDirty: true,
            shouldValidate: false,
          })
          setValue(committedName, true as never, {
            shouldDirty: true,
            shouldValidate: false,
          })
          setValue(manualName, false as never, {
            shouldDirty: true,
            shouldValidate: false,
          })
          clearErrors(addressFieldNames)
        }}
      />
    </FormItem>
  )
}
