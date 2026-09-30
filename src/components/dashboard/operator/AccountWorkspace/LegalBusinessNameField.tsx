import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
} from "react"
import { Loader2Icon } from "lucide-react"

import {
  isCompanySearchAbortError,
  suggestCompanies,
} from "@/api/companySearchApi"
import { Input } from "@/components/ui/input"
import {
  COMPANY_SUGGEST_DEBOUNCE_MS,
  COMPANY_SUGGEST_MIN_CHARS,
  COMPANY_USE_MY_LEGAL_NAME_LABEL,
  type CompanySuggestion,
} from "@/lib/companySearch"
import { ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import { cn } from "@/lib/utils"

type LegalBusinessNameFieldProps = {
  id: string
  value: string
  maxLength?: number
  "aria-describedby"?: string
  className?: string
  onChange: (value: string) => void
  onSelectCompany: (company: {
    legalBusinessName: string
    companyNumber: string
  }) => void
}

export function LegalBusinessNameField({
  id,
  value,
  maxLength = 200,
  "aria-describedby": ariaDescribedBy,
  className,
  onChange,
  onSelectCompany,
}: LegalBusinessNameFieldProps) {
  const generatedId = useId()
  const menuId = `${generatedId}-company-menu`

  const containerRef = useRef<HTMLDivElement>(null)
  const debounceRef = useRef<number | null>(null)
  const suggestRequestRef = useRef(0)
  const suggestAbortRef = useRef<AbortController | null>(null)
  const displayedSuggestQueryRef = useRef<string | null>(null)

  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const [suggestions, setSuggestions] = useState<CompanySuggestion[]>([])
  const [isLoadingSuggestions, setIsLoadingSuggestions] = useState(false)

  useEffect(() => {
    return () => {
      if (debounceRef.current) {
        window.clearTimeout(debounceRef.current)
      }

      suggestAbortRef.current?.abort()
      suggestAbortRef.current = null
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

  const abortPendingSuggest = useCallback(() => {
    suggestAbortRef.current?.abort()
    suggestAbortRef.current = null
  }, [])

  const runSuggest = useCallback(
    async (query: string) => {
      const trimmed = query.trim()
      const normalizedQuery = trimmed.toLowerCase()

      if (trimmed.length < COMPANY_SUGGEST_MIN_CHARS) {
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
        const nextSuggestions = await suggestCompanies(
          trimmed,
          controller.signal
        )

        if (requestId !== suggestRequestRef.current) {
          return
        }

        displayedSuggestQueryRef.current = normalizedQuery
        setSuggestions(nextSuggestions)
      } catch (error) {
        if (isCompanySearchAbortError(error)) {
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

      if (trimmed.length < COMPANY_SUGGEST_MIN_CHARS) {
        displayedSuggestQueryRef.current = null
        setSuggestions([])
        setIsLoadingSuggestions(false)
        return
      }

      setIsLoadingSuggestions(true)

      debounceRef.current = window.setTimeout(() => {
        void runSuggest(trimmed)
      }, COMPANY_SUGGEST_DEBOUNCE_MS)
    },
    [abortPendingSuggest, runSuggest]
  )

  const handleFocus = useCallback(() => {
    setIsMenuOpen(true)

    const trimmed = value.trim()
    const normalizedQuery = trimmed.toLowerCase()

    if (trimmed.length < COMPANY_SUGGEST_MIN_CHARS) {
      return
    }

    if (displayedSuggestQueryRef.current === normalizedQuery) {
      return
    }

    if (isLoadingSuggestions) {
      return
    }

    scheduleSuggest(value)
  }, [isLoadingSuggestions, scheduleSuggest, value])

  const handleChange = useCallback(
    (nextValue: string) => {
      onChange(nextValue)
      setIsMenuOpen(true)
      scheduleSuggest(nextValue)
    },
    [onChange, scheduleSuggest]
  )

  const handleSelectSuggestion = useCallback(
    (suggestion: CompanySuggestion) => {
      setIsMenuOpen(false)
      onSelectCompany({
        legalBusinessName: suggestion.title,
        companyNumber: suggestion.companyNumber,
      })
    },
    [onSelectCompany]
  )

  const handleUseMyLegalName = useCallback(() => {
    onChange(value)
    setIsMenuOpen(false)
  }, [onChange, value])

  const showMenu =
    isMenuOpen
    && (isLoadingSuggestions
      || suggestions.length > 0
      || value.trim().length > 0)

  return (
    <div ref={containerRef} className="relative w-full">
      <Input
        id={id}
        type="text"
        autoComplete="off"
        role="combobox"
        aria-expanded={showMenu}
        aria-controls={menuId}
        aria-autocomplete="list"
        aria-describedby={ariaDescribedBy}
        maxLength={maxLength}
        value={value}
        onFocus={handleFocus}
        onChange={(event) => handleChange(event.target.value)}
        className={cn(ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS, className)}
      />

      {showMenu ? (
        <div
          id={menuId}
          role="listbox"
          className={cn(
            "absolute top-[calc(100%+4px)] z-[80] max-h-64 w-full overflow-y-auto rounded-[4px] border py-1 shadow-md",
            "border-op-card-border bg-op-surface-secondary"
          )}
        >
          {isLoadingSuggestions ? (
            <div className="flex items-center gap-2 px-3 py-2 text-sm text-op-text-muted">
              <Loader2Icon className="size-4 animate-spin" aria-hidden />
              <span>Searching companies…</span>
            </div>
          ) : null}

          {!isLoadingSuggestions
            ? suggestions.map((suggestion) => (
                <button
                  key={suggestion.companyNumber}
                  type="button"
                  role="option"
                  className="flex w-full cursor-pointer flex-col gap-0.5 px-3 py-2 text-left hover:bg-op-background-secondary"
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => {
                    handleSelectSuggestion(suggestion)
                  }}
                >
                  <span className="text-sm text-op-text-primary">
                    {suggestion.title}
                  </span>
                  <span className="text-xs text-op-text-muted">
                    {[suggestion.companyNumber, suggestion.addressSnippet]
                      .filter((part) => part.trim().length > 0)
                      .join(" · ")}
                  </span>
                </button>
              ))
            : null}

          {value.trim() ? (
            <button
              type="button"
              role="option"
              className="flex w-full cursor-pointer flex-col gap-0.5 border-t border-op-card-border px-3 py-2 text-left hover:bg-op-background-secondary"
              onMouseDown={(event) => event.preventDefault()}
              onClick={handleUseMyLegalName}
            >
              <span className="text-sm font-medium text-op-text-primary">
                {COMPANY_USE_MY_LEGAL_NAME_LABEL}
              </span>
              <span className="text-xs text-op-text-muted">{value}</span>
            </button>
          ) : null}
        </div>
      ) : null}
    </div>
  )
}
