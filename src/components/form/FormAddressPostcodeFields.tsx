import {
  type Control,
  type FieldPath,
  type FieldValues,
  useFormContext,
  useWatch,
} from "react-hook-form"

import { AddressPostcodeFields } from "@/components/form/AddressPostcodeFields"
import { FormItem } from "@/components/ui/form"
import { getCrossFieldPeers } from "@/lib/form"

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

type FormAddressPostcodeFieldsProps<
  TFieldValues extends FieldValues,
  TAddressName extends FieldPath<TFieldValues>,
  TPostcodeName extends FieldPath<TFieldValues>,
  TOverrideName extends FieldPath<TFieldValues>,
> = {
  control: Control<TFieldValues>
  addressName: TAddressName
  postcodeName: TPostcodeName
  addressOverriddenName: TOverrideName
  addressClassName?: string
}

export function FormAddressPostcodeFields<
  TFieldValues extends FieldValues,
  TAddressName extends FieldPath<TFieldValues>,
  TPostcodeName extends FieldPath<TFieldValues>,
  TOverrideName extends FieldPath<TFieldValues>,
>({
  control,
  addressName,
  postcodeName,
  addressOverriddenName,
  addressClassName,
}: FormAddressPostcodeFieldsProps<
  TFieldValues,
  TAddressName,
  TPostcodeName,
  TOverrideName
>) {
  const { setValue, trigger, formState } = useFormContext<TFieldValues>()
  const address = useWatch({ control, name: addressName })
  const postcode = useWatch({ control, name: postcodeName })
  const addressOverridden = useWatch({ control, name: addressOverriddenName })
  const crossFieldPeers = getCrossFieldPeers(String(postcodeName))

  const revalidateCrossFieldPeers = () => {
    for (const peer of crossFieldPeers) {
      void trigger(peer as FieldPath<TFieldValues>)
    }
  }

  return (
    <FormItem className="w-full gap-0">
      <AddressPostcodeFields
        address={String(address ?? "")}
        postcode={String(postcode ?? "")}
        addressOverridden={Boolean(addressOverridden)}
        onAddressChange={(value) => {
          setValue(addressName, value as never, {
            shouldDirty: true,
            shouldTouch: true,
          })
          void trigger(addressName)
          revalidateCrossFieldPeers()
        }}
        onPostcodeChange={(value) => {
          setValue(postcodeName, value as never, {
            shouldDirty: true,
            shouldTouch: true,
          })
        }}
        onAddressOverriddenChange={(value) => {
          setValue(addressOverriddenName, value as never, {
            shouldDirty: true,
          })
        }}
        onPostcodeBlur={() => {
          void trigger(postcodeName)
        }}
        addressError={getFieldErrorMessage(
          formState.errors as Record<string, unknown>,
          String(addressName)
        )}
        postcodeError={getFieldErrorMessage(
          formState.errors as Record<string, unknown>,
          String(postcodeName)
        )}
        addressClassName={addressClassName}
      />
    </FormItem>
  )
}
