import { memo, useCallback, useEffect, useId, useRef, useState } from "react"
import { Loader2Icon } from "lucide-react"
import { motion, useReducedMotion } from "framer-motion"

import {
  isAddressLookupAbortError,
  resolveSuggestionAddress,
  suggestAddresses,
} from "@/api/addressLookupApi"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FieldErrorSlot } from "@/components/ui/field"
import {
  ADDRESS_SUGGEST_DEBOUNCE_MS,
  ADDRESS_SUGGEST_MIN_CHARS,
  ADDRESS_USE_MY_ADDRESS_LABEL,
  type AddressSuggestion,
  extractTownCityFromAddress,
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

type MainLocationFloatingInputProps = {
  id: string
  value: string
  isActive: boolean
  error?: string
  menuId: string
  showMenu: boolean
  lookupEnabled: boolean
  disableFocusRing?: boolean
  onFocus: () => void
  onBlur: () => void
  onChange: (value: string) => void
}

const MainLocationFloatingInput = memo(function MainLocationFloatingInput({
  id,
  value,
  isActive,
  error,
  menuId,
  showMenu,
  lookupEnabled,
  disableFocusRing,
  onFocus,
  onBlur,
  onChange,
}: MainLocationFloatingInputProps) {
  const shouldReduceMotion = useReducedMotion()

  return (
    <div
      className={cn(
        "box-border flex w-full shrink-0 items-center gap-0.5 rounded-[4px] border border-[rgba(74,74,76,0.4)] px-[13px]",
        error && "border-destructive",
        !disableFocusRing &&
          "focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50",
        error &&
          !disableFocusRing &&
          "focus-within:ring-destructive/20"
      )}
      style={{
        height: INPUT_HEIGHT,
        minHeight: INPUT_HEIGHT,
        maxHeight: INPUT_HEIGHT,
      }}
    >
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
          <span>Main location</span>
        </motion.label>

        <input
          id={id}
          name="tummly-trial-main-location"
          type="text"
          autoComplete="off"
          role={lookupEnabled ? "combobox" : undefined}
          aria-expanded={lookupEnabled ? showMenu : undefined}
          aria-controls={lookupEnabled ? menuId : undefined}
          aria-autocomplete={lookupEnabled ? "list" : undefined}
          aria-invalid={error ? true : undefined}
          value={value}
          onFocus={onFocus}
          onBlur={onBlur}
          onChange={(event) => onChange(event.target.value)}
          className="absolute left-0 h-5 w-full min-w-0 border-0 bg-transparent p-0 text-sm leading-5 text-[#141414] outline-none"
          style={{
            top: INPUT_TEXT_TOP,
            height: INPUT_TEXT_HEIGHT,
          }}
        />
      </div>
    </div>
  )
})

export type TrialMainLocationFieldsProps = {
  mainLocation: string
  townCity: string
  postcode: string
  committed: boolean
  manual: boolean
  onMainLocationChange: (value: string) => void
  onTownCityChange: (value: string) => void
  onPostcodeChange: (value: string) => void
  onCommittedChange: () => void
  onManualChange: (value: boolean) => void
  onResolvedAddressApply: (payload: {
    mainLocation: string
    townCity: string
    postcode: string
  }) => void
  mainLocationError?: string
  townCityError?: string
  postcodeError?: string
  disableFocusRing?: boolean
  reserveSpace?: boolean
  errorClassName?: string
}

export function TrialMainLocationFields({
  mainLocation,
  townCity,
  postcode,
  committed,
  manual,
  onMainLocationChange,
  onTownCityChange,
  onPostcodeChange,
  onCommittedChange,
  onManualChange,
  onResolvedAddressApply,
  mainLocationError,
  townCityError,
  postcodeError,
  disableFocusRing,
  reserveSpace,
  errorClassName,
}: TrialMainLocationFieldsProps) {
  const generatedId = useId()
  const mainLocationInputId = `${generatedId}-main-location`
  const menuId = `${generatedId}-main-location-menu`

  const containerRef = useRef<HTMLDivElement>(null)
  const debounceRef = useRef<number | null>(null)
  const suggestRequestRef = useRef(0)
  const suggestAbortRef = useRef<AbortController | null>(null)
  const displayedSuggestQueryRef = useRef<string | null>(null)

  const [focused, setFocused] = useState(false)
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState(mainLocation)
  const [suggestions, setSuggestions] = useState<AddressSuggestion[]>([])
  const [isLoadingSuggestions, setIsLoadingSuggestions] = useState(false)
  const [isResolvingSuggestion, setIsResolvingSuggestion] = useState(false)

  useEffect(() => {
    setSearchQuery(mainLocation)
  }, [mainLocation])

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

  useEffect(() => {
    if (!manual) {
      return
    }

    abortPendingSuggest()
    displayedSuggestQueryRef.current = null
    setSuggestions([])
    setIsLoadingSuggestions(false)
    setIsMenuOpen(false)
  }, [abortPendingSuggest, manual])

  useEffect(() => {
    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    document.addEventListener("mousedown", handlePointerDown)
    return () => document.removeEventListener("mousedown", handlePointerDown)
  }, [])

  const runSuggest = useCallback(
    async (query: string) => {
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
        const nextSuggestions = await suggestAddresses(trimmed, controller.signal)

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
    },
    [abortPendingSuggest]
  )

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

  const handleMainLocationFocus = useCallback(() => {
    setFocused(true)

    if (manual) {
      return
    }

    setIsMenuOpen(true)

    const trimmed = searchQuery.trim()
    const normalizedQuery = trimmed.toLowerCase()

    if (trimmed.length < ADDRESS_SUGGEST_MIN_CHARS) {
      return
    }

    if (displayedSuggestQueryRef.current === normalizedQuery) {
      return
    }

    if (isLoadingSuggestions) {
      return
    }

    scheduleSuggest(searchQuery)
  }, [isLoadingSuggestions, manual, scheduleSuggest, searchQuery])

  const handleMainLocationBlur = useCallback(() => {
    setFocused(false)
  }, [])

  const applyResolvedAddress = useCallback(
    (
      resolvedAddress: string,
      resolvedTownCity: string,
      resolvedPostcode: string
    ) => {
      const townCityValue =
        resolvedTownCity.trim() || extractTownCityFromAddress(resolvedAddress)

      onResolvedAddressApply({
        mainLocation: resolvedAddress,
        townCity: townCityValue,
        postcode: resolvedPostcode,
      })
      setSearchQuery(resolvedAddress)
      setIsMenuOpen(false)
    },
    [onResolvedAddressApply]
  )

  const handleMainLocationChange = useCallback(
    (value: string) => {
      setSearchQuery(value)
      onMainLocationChange(value)

      if (manual) {
        return
      }

      setIsMenuOpen(true)
      scheduleSuggest(value)
    },
    [manual, onMainLocationChange, scheduleSuggest]
  )

  const handleSelectSuggestion = async (suggestion: AddressSuggestion) => {
    setIsMenuOpen(false)
    setIsResolvingSuggestion(true)

    try {
      const resolved = await resolveSuggestionAddress(suggestion.id)
      const resolvedAddress = resolved?.address ?? suggestion.address ?? suggestion.label
      const resolvedTownCity = resolved?.postTown ?? ""
      const resolvedPostcode = resolved?.postcode ?? suggestion.postcode ?? ""

      applyResolvedAddress(resolvedAddress, resolvedTownCity, resolvedPostcode)
    } catch {
      const resolvedAddress =
        suggestion.address || suggestion.label
      applyResolvedAddress(
        resolvedAddress,
        extractTownCityFromAddress(resolvedAddress),
        suggestion.postcode ?? ""
      )
    } finally {
      setIsResolvingSuggestion(false)
    }
  }

  const handleUseMyAddressInstead = () => {
    const nextAddress = searchQuery.trim()

    abortPendingSuggest()
    displayedSuggestQueryRef.current = null
    setSuggestions([])
    setIsLoadingSuggestions(false)

    onMainLocationChange(nextAddress)
    setSearchQuery(nextAddress)
    onManualChange(true)
    onCommittedChange()
    setIsMenuOpen(false)
  }

  const isMainLocationActive = focused || searchQuery.length > 0
  const manualAddressText = searchQuery
  const showMenu =
    !manual &&
    isMenuOpen &&
    (isLoadingSuggestions ||
      suggestions.length > 0 ||
      manualAddressText.trim().length > 0)

  const fieldErrorProps = {
    reserveSpace,
    errorClassName,
  } as const

  const mainLocationField = (
    <div
      ref={containerRef}
      className={cn("relative min-w-0", committed ? "flex-1" : "w-full")}
    >
      <div className="flex flex-col gap-1.5">
        <div className="relative">
          <MainLocationFloatingInput
            id={mainLocationInputId}
            value={searchQuery}
            isActive={isMainLocationActive}
            error={mainLocationError}
            menuId={menuId}
            showMenu={showMenu}
            lookupEnabled={!manual}
            disableFocusRing={disableFocusRing}
            onFocus={handleMainLocationFocus}
            onBlur={handleMainLocationBlur}
            onChange={handleMainLocationChange}
          />

          {showMenu ? (
            <div
              id={menuId}
              role="listbox"
              className="absolute top-full z-50 mt-0.5 max-h-64 w-full overflow-y-auto rounded-[4px] border border-[rgba(74,74,76,0.2)] bg-white py-1 shadow-md"
            >
              {isLoadingSuggestions || isResolvingSuggestion ? (
                <div className="flex items-center gap-2 px-3 py-2 text-sm text-[#7d7d7d]">
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
                      className="flex w-full cursor-pointer px-3 py-2 text-left text-sm text-[#141414] hover:bg-[rgba(54,54,56,0.07)]"
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
                  className="flex w-full cursor-pointer flex-col gap-0.5 border-t border-[rgba(74,74,76,0.12)] px-3 py-2 text-left hover:bg-[rgba(54,54,56,0.07)]"
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={handleUseMyAddressInstead}
                >
                  <span className="text-sm font-medium text-[#141414]">
                    {ADDRESS_USE_MY_ADDRESS_LABEL}
                  </span>
                  <span className="text-xs text-[#7d7d7d]">
                    {manualAddressText}
                  </span>
                </button>
              ) : null}
            </div>
          ) : null}
        </div>

        <FieldErrorSlot
          error={mainLocationError}
          reserveSpace={reserveSpace}
          className={mainLocationError ? errorClassName : undefined}
        />
      </div>
    </div>
  )

  return (
    <div className="flex flex-col">
      {committed ? (
        <div className="grid grid-cols-1 gap-0 lg:grid-cols-2 lg:gap-4">
          {mainLocationField}
          <FloatingLabelInput
            id={`${generatedId}-town-city`}
            name="tummly-trial-town-city"
            label="Town/City"
            value={townCity}
            onChange={(event) => onTownCityChange(event.target.value)}
            autoComplete="address-level2"
            required
            error={townCityError}
            disableFocusRing={disableFocusRing}
            {...fieldErrorProps}
          />
        </div>
      ) : (
        mainLocationField
      )}

      {committed ? (
        <FloatingLabelInput
          id={`${generatedId}-postcode`}
          name="tummly-trial-postcode"
          label="Postcode"
          value={postcode}
          onChange={(event) => onPostcodeChange(event.target.value)}
          autoComplete="postal-code"
          required
          error={postcodeError}
          disableFocusRing={disableFocusRing}
          {...fieldErrorProps}
        />
      ) : null}
    </div>
  )
}
