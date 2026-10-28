import {
  useEffect,
  useReducer,
  useRef,
  useState,
  type ReactNode,
} from "react"
import { ChevronLeftIcon } from "lucide-react"
import { toast } from "sonner"

import {
  setLocationMuted,
  setRestaurantMuted,
  type GuestPreferenceLocation,
  type GuestPreferenceRestaurant,
} from "@/api/publicGuestPreferencesApi"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Spinner } from "@/components/ui/spinner"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  isNotFoundError,
  routeGuestPreferencesRequestError,
} from "@/lib/guestPreferences/guestPreferencesRequestErrors"
import { GUEST_PREFERENCES_PILL_BUTTON_CLASS } from "@/lib/guestPreferences/guestPreferencesPresentation"
import {
  brandInitial,
  commitBrandMute,
  commitLocationMute,
  createGuestPreferencesRestaurantsState,
  createRestaurantsLoader,
  guestPreferencesRestaurantsReducer,
  isBrandAllMuted,
  resolveBrandLogo,
  type GuestPreferencesRestaurantsState,
  type GuestPreferencesRestaurantsStore,
} from "@/lib/guestPreferences/guestPreferencesRestaurantsState"
import { cn } from "@/lib/utils"

type GuestPreferencesRestaurantsProps = {
  /** Returns to the overview, which the page refetches. */
  onBack: () => void
  /** Called when a request returns 401. The page falls back to the entry view. */
  onSessionExpired: () => void
}

/**
 * Manage restaurants sub-view. Owns the list state, the debounced search and
 * the mute requests. Figma 7191:598.
 */
export function GuestPreferencesRestaurants({
  onBack,
  onSessionExpired,
}: GuestPreferencesRestaurantsProps) {
  const [state, dispatch] = useReducer(
    guestPreferencesRestaurantsReducer,
    undefined,
    createGuestPreferencesRestaurantsState
  )
  const [loader] = useState(() => createRestaurantsLoader(dispatch))
  // Mute handlers read the latest committed state, so a second click before
  // the re-render sees the mute that is already in flight.
  const stateRef = useRef(state)
  useEffect(() => {
    stateRef.current = state
  }, [state])
  const store: GuestPreferencesRestaurantsStore = {
    dispatch,
    getState: () => stateRef.current,
  }

  useEffect(() => {
    void loader.reload()
    return () => loader.cancelSearch()
  }, [loader])

  useEffect(() => {
    if (state.sessionExpired) {
      onSessionExpired()
    }
  }, [state.sessionExpired, onSessionExpired])

  function handleMuteFailure(error: unknown) {
    const route = routeGuestPreferencesRequestError(
      error,
      GUEST_PREFERENCES_COPY.saveFailed
    )
    if (route.view === "entry") {
      dispatch({ type: "session-expired" })
      return
    }
    if (isNotFoundError(error)) {
      // The change was already rolled back. Refetch the latest query so the list shows the server state.
      void loader.reload()
      return
    }
    toast.error(route.message)
  }

  return (
    <GuestPreferencesRestaurantsContent
      state={state}
      onBack={onBack}
      onQueryChange={(query) => {
        dispatch({ type: "query-changed", query })
        loader.search(query)
      }}
      onRetry={() => void loader.reload()}
      onOpenBrandsChange={(openBrandIds) =>
        dispatch({ type: "brands-open-changed", openBrandIds })
      }
      onMuteLocation={(locationId, muted) =>
        void commitLocationMute(
          setLocationMuted,
          store,
          locationId,
          muted
        ).catch(handleMuteFailure)
      }
      onMuteBrand={(restaurantId, muted) =>
        void commitBrandMute(
          setRestaurantMuted,
          store,
          restaurantId,
          muted
        ).catch(handleMuteFailure)
      }
    />
  )
}

export type GuestPreferencesRestaurantsContentProps = {
  state: GuestPreferencesRestaurantsState
  onBack: () => void
  onQueryChange: (query: string) => void
  onRetry: () => void
  onOpenBrandsChange: (openBrandIds: number[]) => void
  onMuteLocation: (locationId: number, muted: boolean) => void
  onMuteBrand: (restaurantId: number, muted: boolean) => void
}

/**
 * Presentation only, with no hooks, so it renders for any state. The
 * container supplies the state and the handlers.
 */
export function GuestPreferencesRestaurantsContent({
  state,
  onBack,
  onQueryChange,
  onRetry,
  onOpenBrandsChange,
  onMuteLocation,
  onMuteBrand,
}: GuestPreferencesRestaurantsContentProps) {
  // One mute request at a time. Every mute control reads this flag.
  const busy = state.pendingMute !== null

  return (
    <>
      <header className="flex w-full flex-col gap-[34px] px-5 pt-5 pb-2.5">
        <Button
          type="button"
          variant="ghost"
          onClick={onBack}
          className="h-auto min-h-0 gap-3 self-start rounded-[28px] bg-guest-feedback-secondary px-3.5 py-[13px] font-sans text-sm font-bold text-guest-feedback-text shadow-none hover:bg-guest-feedback-secondary/90 hover:text-guest-feedback-text"
        >
          <ChevronLeftIcon aria-hidden className="size-4" />
          {GUEST_PREFERENCES_COPY.backLabel}
        </Button>
        <div className="flex w-full flex-col gap-3">
          <h1 className="m-0 font-heading text-2xl font-bold leading-7 text-guest-feedback-text">
            {GUEST_PREFERENCES_COPY.manageRestaurantsLabel}
          </h1>
          <p className="m-0 font-sans text-sm leading-[19px] text-guest-feedback-muted-soft">
            {GUEST_PREFERENCES_COPY.restaurantsLead}
          </p>
        </div>
      </header>

      <div className="flex w-full flex-col gap-3.5">
        <Input
          type="search"
          value={state.query}
          onChange={(event) => onQueryChange(event.target.value)}
          placeholder={GUEST_PREFERENCES_COPY.restaurantSearchPlaceholder}
          aria-label={GUEST_PREFERENCES_COPY.restaurantSearchPlaceholder}
          className="h-14 rounded-[18px] border-guest-feedback-border bg-guest-feedback-glass px-[15px] font-sans text-sm font-medium text-guest-feedback-text placeholder:text-guest-feedback-placeholder"
        />
        <RestaurantList
          state={state}
          busy={busy}
          onRetry={onRetry}
          onOpenBrandsChange={onOpenBrandsChange}
          onMuteLocation={onMuteLocation}
          onMuteBrand={onMuteBrand}
        />
      </div>
    </>
  )
}

type RestaurantListProps = Pick<
  GuestPreferencesRestaurantsContentProps,
  | "onRetry"
  | "onOpenBrandsChange"
  | "onMuteLocation"
  | "onMuteBrand"
> & {
  state: GuestPreferencesRestaurantsState
  busy: boolean
}

function RestaurantList({
  state,
  busy,
  onRetry,
  onOpenBrandsChange,
  onMuteLocation,
  onMuteBrand,
}: RestaurantListProps): ReactNode {
  if (state.status === "failed") {
    return (
      <div className="flex flex-col items-center gap-3 py-8 text-center">
        <p className="m-0 font-sans text-sm leading-body text-guest-feedback-muted-soft">
          {GUEST_PREFERENCES_COPY.loadFailed}
        </p>
        <Button
          type="button"
          variant="link"
          size="link-sm"
          onClick={onRetry}
          className="font-sans font-medium text-guest-feedback-accent"
        >
          {GUEST_PREFERENCES_COPY.tryAgainLabel}
        </Button>
      </div>
    )
  }

  if (state.restaurants.length === 0) {
    return state.status === "loading" ? (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    ) : (
      <p className="m-0 py-8 text-center font-sans text-sm text-guest-feedback-muted-soft">
        {state.query.trim() === ""
          ? GUEST_PREFERENCES_COPY.restaurantsEmptyLabel
          : GUEST_PREFERENCES_COPY.restaurantsNoMatchLabel}
      </p>
    )
  }

  return (
    <Accordion
      type="multiple"
      value={state.openBrandIds.map(String)}
      onValueChange={(values) => onOpenBrandsChange(values.map(Number))}
      className="gap-3.5"
    >
      {state.restaurants.map((restaurant) => (
        <BrandItem
          key={restaurant.restaurantId}
          restaurant={restaurant}
          busy={busy}
          onMuteLocation={onMuteLocation}
          onMuteBrand={onMuteBrand}
        />
      ))}
    </Accordion>
  )
}

type BrandItemProps = {
  restaurant: GuestPreferenceRestaurant
  busy: boolean
  onMuteLocation: (locationId: number, muted: boolean) => void
  onMuteBrand: (restaurantId: number, muted: boolean) => void
}

function BrandItem({
  restaurant,
  busy,
  onMuteLocation,
  onMuteBrand,
}: BrandItemProps) {
  const allMuted = isBrandAllMuted(restaurant)

  return (
    <AccordionItem
      value={String(restaurant.restaurantId)}
      className="rounded-[28px] border-0 bg-guest-feedback-card px-5 not-last:border-b-0"
    >
      <AccordionTrigger className="items-center rounded-none border-0 py-5 hover:no-underline **:data-[slot=accordion-trigger-icon]:text-guest-feedback-muted">
        <span className="flex items-center gap-3">
          <BrandLogo name={restaurant.name} logoUrl={restaurant.logoUrl} />
          <span className="flex flex-col gap-1">
            <span className="font-heading text-base font-semibold leading-normal text-guest-feedback-text">
              {restaurant.name}
            </span>
            <span className="font-sans text-xs leading-normal text-guest-feedback-muted">
              {GUEST_PREFERENCES_COPY.brandLocationCount(
                restaurant.locations.length
              )}
            </span>
          </span>
        </span>
      </AccordionTrigger>
      <AccordionContent className="pt-0 pb-5">
        <div className="flex flex-col gap-4">
          <Separator className="bg-guest-feedback-border" />
          <ul className="m-0 flex list-none flex-col gap-4 p-0">
            {restaurant.locations.map((location) => (
              <LocationRow
                key={location.locationId}
                location={location}
                busy={busy}
                onToggle={() =>
                  onMuteLocation(location.locationId, !location.muted)
                }
              />
            ))}
          </ul>
          {restaurant.locations.length > 0 ? (
            <Button
              type="button"
              disabled={busy}
              onClick={() => onMuteBrand(restaurant.restaurantId, !allMuted)}
              className={cn(
                GUEST_PREFERENCES_PILL_BUTTON_CLASS,
                "text-guest-feedback-destructive hover:text-guest-feedback-destructive"
              )}
            >
              {allMuted
                ? GUEST_PREFERENCES_COPY.unmuteAllBrandLocationsLabel
                : GUEST_PREFERENCES_COPY.muteAllBrandLocationsLabel}
            </Button>
          ) : null}
        </div>
      </AccordionContent>
    </AccordionItem>
  )
}

type LocationRowProps = {
  location: GuestPreferenceLocation
  busy: boolean
  onToggle: () => void
}

function LocationRow({ location, busy, onToggle }: LocationRowProps) {
  const actionLabel = location.muted
    ? GUEST_PREFERENCES_COPY.unmuteLabel
    : GUEST_PREFERENCES_COPY.muteLabel

  return (
    <li className="flex items-center justify-between gap-3">
      <div className="flex min-w-0 flex-col gap-0.5">
        <span className="font-sans text-sm font-semibold leading-[19px] text-guest-feedback-muted-soft">
          {location.name}
        </span>
        <span className="font-sans text-xs font-semibold leading-[19px] text-guest-feedback-text">
          {location.muted
            ? GUEST_PREFERENCES_COPY.locationMuted
            : GUEST_PREFERENCES_COPY.locationNotMuted}
        </span>
      </div>
      <Button
        type="button"
        variant="link"
        size="link-sm"
        disabled={busy}
        onClick={onToggle}
        aria-label={`${actionLabel} ${location.name}`}
        className="shrink-0 font-sans text-sm font-medium text-guest-feedback-destructive hover:text-guest-feedback-destructive"
      >
        {actionLabel}
      </Button>
    </li>
  )
}

/**
 * The brand logo, or a neutral tile with the brand's first letter when there is
 * none or it fails to load.
 */
function BrandLogo({ name, logoUrl }: { name: string; logoUrl: string | null }) {
  const [failedLogoUrl, setFailedLogoUrl] = useState<string | null>(null)
  const logo = resolveBrandLogo(logoUrl, failedLogoUrl)
  if (logo.kind === "image") {
    return (
      <img
        src={logo.src}
        alt=""
        onError={() => setFailedLogoUrl(logo.src)}
        className="size-[42px] shrink-0 rounded-[6px] object-cover"
      />
    )
  }
  return (
    <span
      aria-hidden
      className="flex size-[42px] shrink-0 items-center justify-center rounded-[6px] bg-guest-feedback-secondary font-heading text-base font-bold text-guest-feedback-text"
    >
      {brandInitial(name)}
    </span>
  )
}
