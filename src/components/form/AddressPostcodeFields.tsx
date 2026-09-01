import { memo, useCallback, useEffect, useId, useRef, useState } from "react"
import { Loader2Icon, MapPinIcon } from "lucide-react"
import { motion, useReducedMotion } from "framer-motion"

import { resolvePostcodeAddress, resolveSuggestionAddress, suggestAddresses, isAddressLookupAbortError } from "@/api/addressLookupApi"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FieldErrorSlot } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  ADDRESS_MULTIPLE_PREMISES_NOTE,
  ADDRESS_POSTCODE_MISMATCH_WARNING,
  ADDRESS_RECONCILED_NOTE,
  ADDRESS_SUGGEST_DEBOUNCE_MS,
  ADDRESS_SUGGEST_MIN_CHARS,
  ADDRESS_USE_MY_ADDRESS_LABEL,
  type AddressSuggestion,
  addressPostcodePairsMatch,
  isDuplicatePostcodeBlurSnapshot,
  isValidUkPostcode,
  postcodesMatch,
  resolveTownCity,
  shouldDeferPostcodeBlurLookup,
  shouldReconcileAddress,
  type VerifiedAddressPostcodePair,
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

type AddressInputProps = {
  id: string
  value: string
  isActive: boolean
  isLocked: boolean
  showPin: boolean
  error?: string
  menuId: string
  showMenu: boolean
  appearance: "default" | "operator"
  onFocus: () => void
  onBlur: () => void
  onChange: (value: string) => void
}

const AddressInput = memo(function AddressInput({
  id,
  value,
  isActive,
  isLocked,
  showPin,
  error,
  menuId,
  showMenu,
  appearance,
  onFocus,
  onBlur,
  onChange,
}: AddressInputProps) {
  const shouldReduceMotion = useReducedMotion()
  const isOperator = appearance === "operator"

  if (isOperator) {
    return (
      <div
        className={cn(
          "box-border flex h-[50px] w-full shrink-0 items-center gap-2 rounded border px-[15px]",
          "border-op-input-border",
          isLocked && "bg-op-background-secondary",
          error && "border-destructive",
          "focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50",
          error && "focus-within:ring-destructive/20"
        )}
      >
        {showPin ? (
          <MapPinIcon
            aria-hidden
            className="pointer-events-none size-[18px] shrink-0 text-op-text-muted"
          />
        ) : null}

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
          placeholder="Enter"
          value={value}
          readOnly={isLocked}
          onFocus={onFocus}
          onBlur={onBlur}
          onChange={(event) => onChange(event.target.value)}
          className={cn(
            "min-w-0 flex-1 border-0 bg-transparent p-0 text-sm leading-5 text-op-text-primary outline-none placeholder:text-op-text-muted",
            isLocked && "cursor-default"
          )}
        />
      </div>
    )
  }

  return (
    <div
      className={cn(
        "box-border flex w-full shrink-0 items-center gap-0.5 rounded-[4px] border px-[13px]",
        "border-[rgba(74,74,76,0.4)]",
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
          className="pointer-events-none size-[18px] shrink-0 text-[#7d7d7d]"
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

export type AddressDetailsRevealSource = "suggestion" | "manual"

type AddressPostcodeFieldsProps = {
  address: string
  postcode: string
  addressOverridden: boolean
  onAddressChange: (value: string) => void
  onPostcodeChange: (value: string) => void
  onAddressOverriddenChange: (value: boolean) => void
  /** Ideal Postcodes post_town (or address fallback) after a successful lookup. */
  onCityResolved?: (city: string) => void
  city?: string
  onCityChange?: (value: string) => void
  cityError?: string
  /**
   * Operator layout: hide City and Postcode until a suggestion is picked
   * or the operator uses their own address. Omit to keep both fields visible.
   */
  showCityAndPostcode?: boolean
  onDetailsRevealed?: (source: AddressDetailsRevealSource) => void
  addressError?: string
  postcodeError?: string
  required?: boolean
  addressClassName?: string
  appearance?: "default" | "operator"
  onPostcodeBlur?: () => void
}

export function AddressPostcodeFields({
  address,
  postcode,
  addressOverridden,
  onAddressChange,
  onPostcodeChange,
  onAddressOverriddenChange,
  onCityResolved,
  city = "",
  onCityChange,
  cityError,
  showCityAndPostcode,
  onDetailsRevealed,
  addressError,
  postcodeError,
  required = true,
  addressClassName,
  appearance = "default",
  onPostcodeBlur,
}: AddressPostcodeFieldsProps) {
  const generatedId = useId()
  const addressInputId = `${generatedId}-address`
  const postcodeInputId = `${generatedId}-postcode`
  const menuId = `${generatedId}-address-menu`

  const containerRef = useRef<HTMLDivElement>(null)
  const debounceRef = useRef<number | null>(null)
  const suggestRequestRef = useRef(0)
  const suggestAbortRef = useRef<AbortController | null>(null)
  const displayedSuggestQueryRef = useRef<string | null>(null)
  const verifiedPairRef = useRef<VerifiedAddressPostcodePair | null>(null)
  const lastBlurSnapshotRef = useRef<VerifiedAddressPostcodePair | null>(null)
  const pendingPostcodeBlurRef = useRef(false)

  const [focused, setFocused] = useState(false)
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState(address)
  const [suggestions, setSuggestions] = useState<AddressSuggestion[]>([])
  const [isLoadingSuggestions, setIsLoadingSuggestions] = useState(false)
  const [isResolvingSuggestion, setIsResolvingSuggestion] = useState(false)
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

      suggestAbortRef.current?.abort()
      suggestAbortRef.current = null
    }
  }, [])

  const abortPendingSuggest = useCallback(() => {
    suggestAbortRef.current?.abort()
    suggestAbortRef.current = null
  }, [])

  const clearVerifiedPair = useCallback(() => {
    verifiedPairRef.current = null
    lastBlurSnapshotRef.current = null
  }, [])

  const recordBlurSnapshot = useCallback((nextAddress: string, nextPostcode: string) => {
    lastBlurSnapshotRef.current = {
      address: nextAddress,
      postcode: nextPostcode,
    }
  }, [])

  const rememberVerifiedPair = useCallback(
    (nextAddress: string, nextPostcode: string) => {
      verifiedPairRef.current = {
        address: nextAddress,
        postcode: nextPostcode,
      }
    },
    []
  )

  const isCurrentPairVerified = useCallback(() => {
    const verified = verifiedPairRef.current

    if (!verified) {
      return false
    }

    return addressPostcodePairsMatch(verified, address, postcode)
  }, [address, postcode])

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
    const normalizedQuery = trimmed.toLowerCase()

    if (trimmed.length < ADDRESS_SUGGEST_MIN_CHARS) {
      displayedSuggestQueryRef.current = null
      setSuggestions([])
      setIsLoadingSuggestions(false)
      return
    }

    abortPendingSuggest()

    const controller = new AbortController()
    suggestAbortRef.current = controller

    const requestId = ++suggestRequestRef.current
    setIsLoadingSuggestions(true)

    try {
      const nextSuggestions = await suggestAddresses(
        trimmed,
        controller.signal
      )

      if (requestId !== suggestRequestRef.current) {
        return
      }

      displayedSuggestQueryRef.current = normalizedQuery
      setSuggestions(nextSuggestions)
    } catch (error) {
      if (isAddressLookupAbortError(error)) {
        return
      }

      if (requestId === suggestRequestRef.current) {
        displayedSuggestQueryRef.current = null
        setSuggestions([])
      }
    } finally {
      if (requestId === suggestRequestRef.current) {
        setIsLoadingSuggestions(false)
        if (suggestAbortRef.current === controller) {
          suggestAbortRef.current = null
        }
      }
    }
  }, [abortPendingSuggest])

  const scheduleSuggest = useCallback(
    (query: string) => {
      if (debounceRef.current) {
        window.clearTimeout(debounceRef.current)
      }

      abortPendingSuggest()

      const trimmed = query.trim()

      if (trimmed.length < ADDRESS_SUGGEST_MIN_CHARS) {
        displayedSuggestQueryRef.current = null
        setSuggestions([])
        setIsLoadingSuggestions(false)
        return
      }

      setIsLoadingSuggestions(true)

      debounceRef.current = window.setTimeout(() => {
        void runSuggest(trimmed)
      }, ADDRESS_SUGGEST_DEBOUNCE_MS)
    },
    [abortPendingSuggest, runSuggest]
  )

  const handleAddressFocus = useCallback(() => {
    setFocused(true)
    setIsMenuOpen(true)

    const trimmed = searchQuery.trim()
    const normalizedQuery = trimmed.toLowerCase()

    if (trimmed.length < ADDRESS_SUGGEST_MIN_CHARS) {
      return
    }

    if (
      displayedSuggestQueryRef.current === normalizedQuery
    ) {
      return
    }

    if (isLoadingSuggestions) {
      return
    }

    scheduleSuggest(searchQuery)
  }, [isLoadingSuggestions, scheduleSuggest, searchQuery])

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
      clearVerifiedPair()
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
      clearVerifiedPair,
    ]
  )

  const applyResolvedSuggestion = useCallback(
    (
      resolvedAddress: string,
      resolvedPostcode: string,
      existingPostcode: string,
      resolvedCity: string
    ) => {
      onAddressChange(resolvedAddress)
      setSearchQuery(resolvedAddress)
      onAddressOverriddenChange(false)
      setIsLocked(false)
      setReconciliationNote(null)

      if (resolvedCity.trim()) {
        onCityResolved?.(resolvedCity.trim())
      }

      onDetailsRevealed?.("suggestion")

      if (!existingPostcode.trim()) {
        if (resolvedPostcode.trim()) {
          onPostcodeChange(resolvedPostcode)
          rememberVerifiedPair(resolvedAddress, resolvedPostcode)
        } else {
          clearVerifiedPair()
        }
        setConflictWarning(null)
        return
      }

      if (!resolvedPostcode.trim()) {
        clearVerifiedPair()
        setConflictWarning(null)
        return
      }

      if (!postcodesMatch(existingPostcode, resolvedPostcode)) {
        clearVerifiedPair()
        setConflictWarning(ADDRESS_POSTCODE_MISMATCH_WARNING)
        return
      }

      setConflictWarning(null)
      rememberVerifiedPair(resolvedAddress, existingPostcode)
    },
    [
      clearVerifiedPair,
      onAddressChange,
      onAddressOverriddenChange,
      onCityResolved,
      onDetailsRevealed,
      onPostcodeChange,
      rememberVerifiedPair,
    ]
  )

  const handleSelectSuggestion = async (suggestion: AddressSuggestion) => {
    setIsMenuOpen(false)
    setIsResolvingSuggestion(true)

    try {
      const resolved = await resolveSuggestionAddress(suggestion.id)
      const resolvedAddress = resolved?.address ?? suggestion.label
      const resolvedPostcode = resolved?.postcode ?? ""
      const resolvedCity = resolveTownCity({
        postTown: resolved?.postTown,
        address: resolvedAddress,
      })

      applyResolvedSuggestion(
        resolvedAddress,
        resolvedPostcode,
        postcode,
        resolvedCity
      )
    } catch {
      const fallbackAddress = suggestion.label
      onAddressChange(fallbackAddress)
      setSearchQuery(fallbackAddress)
      onAddressOverriddenChange(false)
      setIsLocked(false)
      setReconciliationNote(null)
      clearVerifiedPair()
      setConflictWarning(null)
      const fallbackCity = resolveTownCity({ address: fallbackAddress })
      if (fallbackCity) {
        onCityResolved?.(fallbackCity)
      }
      onDetailsRevealed?.("suggestion")
    } finally {
      setIsResolvingSuggestion(false)
    }
  }

  const handleUseMyAddressInstead = () => {
    const nextAddress = isLocked ? lockedOperatorText : searchQuery

    onAddressChange(nextAddress)
    setSearchQuery(nextAddress)
    onAddressOverriddenChange(true)
    if (appearance === "operator") {
      onCityChange?.("")
      onPostcodeChange("")
      onDetailsRevealed?.("manual")
    }
    clearVerifiedPair()
    setIsLocked(false)
    setReconciliationNote(null)
    setConflictWarning(null)
    setIsMenuOpen(false)
  }

  const handlePostcodeChange = (value: string) => {
    onPostcodeChange(value)
    clearVerifiedPair()
    pendingPostcodeBlurRef.current = false

    if (isLocked) {
      setIsLocked(false)
      setReconciliationNote(null)
    }
  }

  const handlePostcodeBlur = useCallback(async () => {
    if (!isValidUkPostcode(postcode) || addressOverridden) {
      pendingPostcodeBlurRef.current = false
      return
    }

    if (isCurrentPairVerified()) {
      pendingPostcodeBlurRef.current = false
      recordBlurSnapshot(address, postcode)
      onPostcodeBlur?.()
      return
    }

    if (
      shouldDeferPostcodeBlurLookup({
        isResolvingSuggestion,
        isResolvingPostcode,
      })
    ) {
      pendingPostcodeBlurRef.current = true
      return
    }

    pendingPostcodeBlurRef.current = false

    if (isDuplicatePostcodeBlurSnapshot(lastBlurSnapshotRef.current, address, postcode)) {
      onPostcodeBlur?.()
      return
    }

    setIsResolvingPostcode(true)

    try {
      const result = await resolvePostcodeAddress(postcode, address)

      if (!result) {
        recordBlurSnapshot(address, postcode)
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
        rememberVerifiedPair(address, postcode)
        recordBlurSnapshot(address, postcode)
        const city = resolveTownCity({
          postTown: result.postTown,
          address,
        })
        if (city) {
          onCityResolved?.(city)
        }
        return
      }

      setLockedOperatorText(address)
      onAddressChange(result.address)
      setSearchQuery(result.address)
      setIsLocked(true)
      onAddressOverriddenChange(false)
      rememberVerifiedPair(result.address, result.postcode)
      recordBlurSnapshot(result.address, result.postcode)
      setReconciliationNote(
        result.multiplePremises && result.usedBestMatch
          ? ADDRESS_MULTIPLE_PREMISES_NOTE
          : ADDRESS_RECONCILED_NOTE
      )
      const city = resolveTownCity({
        postTown: result.postTown,
        address: result.address,
      })
      if (city) {
        onCityResolved?.(city)
      }
    } catch {
      // Leave the operator's entered address untouched when lookup fails.
      recordBlurSnapshot(address, postcode)
    } finally {
      setIsResolvingPostcode(false)
      onPostcodeBlur?.()
    }
  }, [
    address,
    addressOverridden,
    isCurrentPairVerified,
    isResolvingPostcode,
    isResolvingSuggestion,
    onAddressChange,
    onAddressOverriddenChange,
    onCityResolved,
    onPostcodeBlur,
    postcode,
    recordBlurSnapshot,
    rememberVerifiedPair,
  ])

  useEffect(() => {
    if (isResolvingSuggestion || isResolvingPostcode) {
      return
    }

    if (!pendingPostcodeBlurRef.current) {
      return
    }

    pendingPostcodeBlurRef.current = false
    void handlePostcodeBlur()
  }, [handlePostcodeBlur, isResolvingPostcode, isResolvingSuggestion])

  const showAddressPin = !searchQuery.trim()
  const isAddressActive = focused || searchQuery.length > 0 || isLocked
  const manualAddressText = isLocked ? lockedOperatorText : searchQuery
  const showMenu =
    isMenuOpen &&
    (isLoadingSuggestions ||
      suggestions.length > 0 ||
      manualAddressText.trim().length > 0)
  const isOperator = appearance === "operator"
  const operatorDetailsVisible = !isOperator || showCityAndPostcode !== false
  const operatorFieldClass =
    "h-[50px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-text-muted md:text-sm dark:bg-transparent"
  const operatorLabelClass =
    "text-sm font-semibold leading-5 text-op-text-primary"

  const addressField = (
    <div
      ref={containerRef}
      className={cn("relative min-w-0 flex-1", addressClassName)}
    >
      <div className={cn("flex flex-col", isOperator ? "gap-2" : "gap-1.5")}>
        {isOperator ? (
          <Label htmlFor={addressInputId} className={operatorLabelClass}>
            Address
          </Label>
        ) : null}

        <AddressInput
          id={addressInputId}
          value={searchQuery}
          isActive={isAddressActive}
          isLocked={isLocked}
          showPin={showAddressPin}
          error={addressError}
          menuId={menuId}
          showMenu={showMenu}
          appearance={appearance}
          onFocus={handleAddressFocus}
          onBlur={handleAddressBlur}
          onChange={handleAddressChange}
        />

        <FieldErrorSlot error={addressError} reserveClassName="min-h-0" />

        {conflictWarning ? (
          <p
            className={cn(
              "text-sm",
              isOperator ? "text-amber-500" : "text-amber-700"
            )}
          >
            {conflictWarning}
          </p>
        ) : null}

        {reconciliationNote ? (
          <p
            className={cn(
              "text-sm",
              isOperator ? "text-op-text-muted" : "text-[#7d7d7d]"
            )}
          >
            {reconciliationNote}
          </p>
        ) : null}
      </div>

      {showMenu ? (
        <div
          id={menuId}
          role="listbox"
          className={cn(
            "absolute top-[calc(100%+4px)] z-[80] max-h-64 w-full overflow-y-auto rounded-[4px] border py-1 shadow-md",
            isOperator
              ? "border-op-card-border bg-op-surface-secondary"
              : "border-[rgba(74,74,76,0.2)] bg-white"
          )}
        >
          {isLoadingSuggestions || isResolvingSuggestion ? (
            <div
              className={cn(
                "flex items-center gap-2 px-3 py-2 text-sm",
                isOperator ? "text-op-text-muted" : "text-[#7d7d7d]"
              )}
            >
              <Loader2Icon className="size-4 animate-spin" aria-hidden />
              <span>
                {isResolvingSuggestion
                  ? "Loading address…"
                  : "Searching addresses…"}
              </span>
            </div>
          ) : null}

          {!isResolvingSuggestion
            ? suggestions.map((suggestion) => (
                <button
                  key={suggestion.id}
                  type="button"
                  role="option"
                  className={cn(
                    "flex w-full cursor-pointer px-3 py-2 text-left text-sm",
                    isOperator
                      ? "text-op-text-primary hover:bg-op-background-secondary"
                      : "text-[#141414] hover:bg-[rgba(54,54,56,0.07)]"
                  )}
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => {
                    void handleSelectSuggestion(suggestion)
                  }}
                >
                  {suggestion.label}
                </button>
              ))
            : null}

          {!isResolvingSuggestion && manualAddressText.trim() ? (
            <button
              type="button"
              role="option"
              className={cn(
                "flex w-full cursor-pointer flex-col gap-0.5 border-t px-3 py-2 text-left",
                isOperator
                  ? "border-op-card-border hover:bg-op-background-secondary"
                  : "border-[rgba(74,74,76,0.12)] hover:bg-[rgba(54,54,56,0.07)]"
              )}
              onMouseDown={(event) => event.preventDefault()}
              onClick={handleUseMyAddressInstead}
            >
              <span
                className={cn(
                  "text-sm font-medium",
                  isOperator ? "text-op-text-primary" : "text-[#141414]"
                )}
              >
                {ADDRESS_USE_MY_ADDRESS_LABEL}
              </span>
              <span
                className={cn(
                  "text-xs",
                  isOperator ? "text-op-text-muted" : "text-[#7d7d7d]"
                )}
              >
                {manualAddressText}
              </span>
            </button>
          ) : null}
        </div>
      ) : null}
    </div>
  )

  if (!isOperator) {
    return (
      <div className="flex flex-col gap-6 sm:flex-row sm:gap-5">
        {addressField}
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
          {isResolvingPostcode || isResolvingSuggestion ? (
            <Loader2Icon
              aria-hidden
              className="pointer-events-none absolute top-4 right-3 size-4 animate-spin text-[#7d7d7d]"
            />
          ) : null}
        </div>
      </div>
    )
  }

  return (
    <div className="contents">
      {addressField}

      {operatorDetailsVisible ? (
        <>
          <div className="flex min-w-0 flex-col gap-2">
            <Label htmlFor={`${generatedId}-city`} className={operatorLabelClass}>
              City
            </Label>
            <Input
              id={`${generatedId}-city`}
              className={operatorFieldClass}
              placeholder="Enter"
              value={city}
              onChange={(event) => {
                onCityChange?.(event.target.value)
              }}
              autoComplete="address-level2"
              aria-invalid={cityError ? true : undefined}
            />
            <FieldErrorSlot error={cityError} reserveClassName="min-h-0" />
          </div>

          <div className="relative min-w-0">
            <div className="flex flex-col gap-2">
              <Label htmlFor={postcodeInputId} className={operatorLabelClass}>
                Postcode
              </Label>
              <Input
                id={postcodeInputId}
                name="tummly-setup-postcode"
                className={operatorFieldClass}
                placeholder="Enter"
                value={postcode}
                onChange={(event) => handlePostcodeChange(event.target.value)}
                onBlur={() => {
                  void handlePostcodeBlur()
                }}
                autoComplete="off"
                required={required}
                aria-invalid={postcodeError ? true : undefined}
              />
              <FieldErrorSlot error={postcodeError} reserveClassName="min-h-0" />
            </div>
            {isResolvingPostcode || isResolvingSuggestion ? (
              <Loader2Icon
                aria-hidden
                className="pointer-events-none absolute top-11 right-3 size-4 animate-spin text-op-text-muted"
              />
            ) : null}
          </div>
        </>
      ) : null}
    </div>
  )
}
