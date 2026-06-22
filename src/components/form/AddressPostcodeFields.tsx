import { memo, useCallback, useEffect, useId, useRef, useState } from "react"
import { Loader2Icon, MapPinIcon } from "lucide-react"
import { motion, useReducedMotion } from "framer-motion"

import { resolvePostcodeAddress, suggestAddresses } from "@/api/addressLookupApi"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FieldErrorSlot } from "@/components/ui/field"
import {
  ADDRESS_MULTIPLE_PREMISES_NOTE,
  ADDRESS_POSTCODE_MISMATCH_WARNING,
  ADDRESS_RECONCILED_NOTE,
  ADDRESS_SUGGEST_DEBOUNCE_MS,
  ADDRESS_SUGGEST_MIN_CHARS,
  ADDRESS_USE_MY_ADDRESS_LABEL,
  type AddressSuggestion,
  isValidUkPostcode,
  postcodesMatch,
  shouldReconcileAddress,
} from "@/lib/addressLookup"
import { cn } from "@/lib/utils"

const INPUT_HEIGHT = 50
const LABEL_TOP = 8
const LABEL_REST_Y = 7
const INPUT_TEXT_TOP = 22
const INPUT_TEXT_HEIGHT = 20

const labelVariants = {
  rest: { y: LABEL_REST_Y, scale: 1 },
  active: { y: 0, scale: 12 / 14 },
}

const labelTransition = {
  type: "spring" as const,
  stiffness: 560,
  damping: 36,
  mass: 0.45,
}

const labelMotionStyle = {
  top: LABEL_TOP,
  transformOrigin: "0 0",
} as const

type AddressFloatingInputProps = {
  id: string
  value: string
  isActive: boolean
  isLocked: boolean
  showPin: boolean
  error?: string
  menuId: string
  showMenu: boolean
  onFocus: () => void
  onBlur: () => void
  onChange: (value: string) => void
}

const AddressFloatingInput = memo(function AddressFloatingInput({
  id,
  value,
  isActive,
  isLocked,
  showPin,
  error,
  menuId,
  showMenu,
  onFocus,
  onBlur,
  onChange,
}: AddressFloatingInputProps) {
  const shouldReduceMotion = useReducedMotion()

  return (
    <div
      className={cn(
        "box-border flex w-full shrink-0 items-center gap-0.5 rounded-[4px] border border-[rgba(74,74,76,0.4)] px-[13px]",
        isLocked && "bg-[rgba(54,54,56,0.07)]",
        error && "border-destructive",
        "focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50",
        error && "focus-within:ring-destructive/20"
      )}
      style={{
        height: INPUT_HEIGHT,
        minHeight: INPUT_HEIGHT,
        maxHeight: INPUT_HEIGHT,
      }}
    >
      {showPin ? (
        <MapPinIcon
          aria-hidden
          className="pointer-events-none shrink-0 size-[18px] text-[#7d7d7d]"
        />
      ) : null}

      <div
        className="relative min-w-0 flex-1 shrink-0"
        style={{ height: INPUT_HEIGHT }}
      >
        <motion.label
          htmlFor={id}
          initial={false}
          variants={labelVariants}
          animate={isActive ? "active" : "rest"}
          transition={shouldReduceMotion ? { duration: 0 } : labelTransition}
          style={labelMotionStyle}
          className="pointer-events-none absolute left-0 z-10 inline-flex origin-top-left items-center gap-1.5 text-sm leading-5 text-guest-feedback-placeholder"
        >
          <span>Address</span>
        </motion.label>

        <input
          id={id}
          name="tummly-setup-address"
          type="text"
          autoComplete="off"
          role="combobox"
          aria-expanded={showMenu}
          aria-controls={menuId}
          aria-autocomplete="list"
          aria-invalid={error ? true : undefined}
          value={value}
          readOnly={isLocked}
          onFocus={onFocus}
          onBlur={onBlur}
          onChange={(event) => onChange(event.target.value)}
          className={cn(
            "absolute left-0 w-full min-w-0 border-0 bg-transparent p-0 text-sm leading-5 text-[#141414] outline-none",
            isLocked && "cursor-default"
          )}
          style={{
            top: INPUT_TEXT_TOP,
            height: INPUT_TEXT_HEIGHT,
          }}
        />
      </div>
    </div>
  )
})

type AddressPostcodeFieldsProps = {
  address: string
  postcode: string
  addressOverridden: boolean
  onAddressChange: (value: string) => void
  onPostcodeChange: (value: string) => void
  onAddressOverriddenChange: (value: boolean) => void
  addressError?: string
  postcodeError?: string
  required?: boolean
  addressClassName?: string
  onPostcodeBlur?: () => void
}

export function AddressPostcodeFields({
  address,
  postcode,
  addressOverridden,
  onAddressChange,
  onPostcodeChange,
  onAddressOverriddenChange,
  addressError,
  postcodeError,
  required = true,
  addressClassName,
  onPostcodeBlur,
}: AddressPostcodeFieldsProps) {
  const generatedId = useId()
  const addressInputId = `${generatedId}-address`
  const postcodeInputId = `${generatedId}-postcode`
  const menuId = `${generatedId}-address-menu`

  const containerRef = useRef<HTMLDivElement>(null)
  const debounceRef = useRef<number | null>(null)
  const suggestRequestRef = useRef(0)

  const [focused, setFocused] = useState(false)
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState(address)
  const [suggestions, setSuggestions] = useState<AddressSuggestion[]>([])
  const [isLoadingSuggestions, setIsLoadingSuggestions] = useState(false)
  const [isResolvingPostcode, setIsResolvingPostcode] = useState(false)
  const [isLocked, setIsLocked] = useState(false)
  const [lockedOperatorText, setLockedOperatorText] = useState("")
  const [conflictWarning, setConflictWarning] = useState<string | null>(null)
  const [reconciliationNote, setReconciliationNote] = useState<string | null>(
    null
  )

  useEffect(() => {
    if (!isLocked) {
      setSearchQuery(address)
    }
  }, [address, isLocked])

  useEffect(() => {
    return () => {
      if (debounceRef.current) {
        window.clearTimeout(debounceRef.current)
      }
    }
  }, [])

  useEffect(() => {
    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    document.addEventListener("mousedown", handlePointerDown)
    return () => document.removeEventListener("mousedown", handlePointerDown)
  }, [])

  const runSuggest = useCallback(async (query: string) => {
    const trimmed = query.trim()

    if (trimmed.length < ADDRESS_SUGGEST_MIN_CHARS) {
      setSuggestions([])
      setIsLoadingSuggestions(false)
      return
    }

    const requestId = ++suggestRequestRef.current
    setIsLoadingSuggestions(true)

    try {
      const nextSuggestions = await suggestAddresses(trimmed)

      if (requestId !== suggestRequestRef.current) {
        return
      }

      setSuggestions(nextSuggestions)
    } catch {
      if (requestId === suggestRequestRef.current) {
        setSuggestions([])
      }
    } finally {
      if (requestId === suggestRequestRef.current) {
        setIsLoadingSuggestions(false)
      }
    }
  }, [])

  const scheduleSuggest = useCallback(
    (query: string) => {
      if (debounceRef.current) {
        window.clearTimeout(debounceRef.current)
      }

      const trimmed = query.trim()

      if (trimmed.length < ADDRESS_SUGGEST_MIN_CHARS) {
        setSuggestions([])
        setIsLoadingSuggestions(false)
        return
      }

      setIsLoadingSuggestions(true)

      debounceRef.current = window.setTimeout(() => {
        void runSuggest(trimmed)
      }, ADDRESS_SUGGEST_DEBOUNCE_MS)
    },
    [runSuggest]
  )

  const handleAddressFocus = useCallback(() => {
    setFocused(true)
    setIsMenuOpen(true)

    if (searchQuery.trim().length >= ADDRESS_SUGGEST_MIN_CHARS) {
      scheduleSuggest(searchQuery)
    }
  }, [scheduleSuggest, searchQuery])

  const handleAddressBlur = useCallback(() => {
    setFocused(false)
  }, [])

  const handleAddressChange = useCallback(
    (value: string) => {
      if (isLocked) {
        return
      }

      setSearchQuery(value)
      onAddressChange(value)
      onAddressOverriddenChange(false)
      setConflictWarning(null)
      setReconciliationNote(null)
      setIsMenuOpen(true)
      scheduleSuggest(value)
    },
    [
      isLocked,
      onAddressChange,
      onAddressOverriddenChange,
      scheduleSuggest,
    ]
  )

  const handleSelectSuggestion = (suggestion: AddressSuggestion) => {
    onAddressChange(suggestion.address)
    setSearchQuery(suggestion.address)
    onAddressOverriddenChange(false)
    setIsLocked(false)
    setReconciliationNote(null)

    if (!postcode.trim()) {
      onPostcodeChange(suggestion.postcode)
      setConflictWarning(null)
    } else if (
      suggestion.postcode &&
      !postcodesMatch(postcode, suggestion.postcode)
    ) {
      setConflictWarning(ADDRESS_POSTCODE_MISMATCH_WARNING)
    } else {
      setConflictWarning(null)
    }

    setIsMenuOpen(false)
  }

  const handleUseMyAddressInstead = () => {
    const nextAddress = isLocked ? lockedOperatorText : searchQuery

    onAddressChange(nextAddress)
    setSearchQuery(nextAddress)
    onAddressOverriddenChange(true)
    setIsLocked(false)
    setReconciliationNote(null)
    setConflictWarning(null)
    setIsMenuOpen(false)
  }

  const handlePostcodeChange = (value: string) => {
    onPostcodeChange(value)

    if (isLocked) {
      setIsLocked(false)
      setReconciliationNote(null)
    }
  }

  const handlePostcodeBlur = async () => {
    if (!isValidUkPostcode(postcode) || addressOverridden) {
      return
    }

    setIsResolvingPostcode(true)

    try {
      const result = await resolvePostcodeAddress(postcode, address)

      if (!result) {
        return
      }

      if (
        !shouldReconcileAddress(
          address,
          result.address,
          result.postcode,
          postcode
        )
      ) {
        setIsLocked(false)
        setReconciliationNote(null)
        return
      }

      setLockedOperatorText(address)
      onAddressChange(result.address)
      setSearchQuery(result.address)
      setIsLocked(true)
      onAddressOverriddenChange(false)
      setReconciliationNote(
        result.multiplePremises && result.usedBestMatch
          ? ADDRESS_MULTIPLE_PREMISES_NOTE
          : ADDRESS_RECONCILED_NOTE
      )
    } catch {
      // Leave the operator's entered address untouched when lookup fails.
    } finally {
      setIsResolvingPostcode(false)
      onPostcodeBlur?.()
    }
  }

  const showAddressPin = !searchQuery.trim()
  const isAddressActive = focused || searchQuery.length > 0 || isLocked
  const manualAddressText = isLocked ? lockedOperatorText : searchQuery
  const showMenu =
    isMenuOpen &&
    (isLoadingSuggestions ||
      suggestions.length > 0 ||
      manualAddressText.trim().length > 0)

  return (
    <div className="flex flex-col gap-6 sm:flex-row sm:gap-5">
      <div
        ref={containerRef}
        className={cn("relative min-w-0 flex-1", addressClassName)}
      >
        <div className="flex flex-col gap-1.5">
          <AddressFloatingInput
            id={addressInputId}
            value={searchQuery}
            isActive={isAddressActive}
            isLocked={isLocked}
            showPin={showAddressPin}
            error={addressError}
            menuId={menuId}
            showMenu={showMenu}
            onFocus={handleAddressFocus}
            onBlur={handleAddressBlur}
            onChange={handleAddressChange}
          />

          <FieldErrorSlot error={addressError} reserveClassName="min-h-0" />

          {conflictWarning ? (
            <p className="text-sm text-amber-700">{conflictWarning}</p>
          ) : null}

          {reconciliationNote ? (
            <p className="text-sm text-[#7d7d7d]">{reconciliationNote}</p>
          ) : null}
        </div>

        {showMenu ? (
          <div
            id={menuId}
            role="listbox"
            className="absolute top-[calc(100%+4px)] z-50 max-h-64 w-full overflow-y-auto rounded-[4px] border border-[rgba(74,74,76,0.2)] bg-white py-1 shadow-md"
          >
            {isLoadingSuggestions ? (
              <div className="flex items-center gap-2 px-3 py-2 text-sm text-[#7d7d7d]">
                <Loader2Icon className="size-4 animate-spin" aria-hidden />
                <span>Searching addresses…</span>
              </div>
            ) : null}

            {suggestions.map((suggestion) => (
              <button
                key={suggestion.id}
                type="button"
                role="option"
                className="flex w-full cursor-pointer px-3 py-2 text-left text-sm text-[#141414] hover:bg-[rgba(54,54,56,0.07)]"
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => handleSelectSuggestion(suggestion)}
              >
                {suggestion.label}
              </button>
            ))}

            {manualAddressText.trim() ? (
              <button
                type="button"
                role="option"
                className="flex w-full cursor-pointer flex-col gap-0.5 border-t border-[rgba(74,74,76,0.12)] px-3 py-2 text-left hover:bg-[rgba(54,54,56,0.07)]"
                onMouseDown={(event) => event.preventDefault()}
                onClick={handleUseMyAddressInstead}
              >
                <span className="text-sm font-medium text-[#141414]">
                  {ADDRESS_USE_MY_ADDRESS_LABEL}
                </span>
                <span className="text-xs text-[#7d7d7d]">{manualAddressText}</span>
              </button>
            ) : null}
          </div>
        ) : null}
      </div>

      <div className="relative min-w-0 flex-1">
        <FloatingLabelInput
          id={postcodeInputId}
          name="tummly-setup-postcode"
          label="Postcode"
          value={postcode}
          onChange={(event) => handlePostcodeChange(event.target.value)}
          onBlur={() => {
            void handlePostcodeBlur()
          }}
          autoComplete="off"
          required={required}
          error={postcodeError}
          reserveClassName="min-h-0"
        />

        {isResolvingPostcode ? (
          <Loader2Icon
            aria-hidden
            className="pointer-events-none absolute right-3 top-4 size-4 animate-spin text-[#7d7d7d]"
          />
        ) : null}
      </div>
    </div>
  )
}
