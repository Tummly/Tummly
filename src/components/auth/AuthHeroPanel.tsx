import authHeadlineBrush from "@/assets/images/auth-headline-brush.png"
import { authSignInPanelPicture } from "@/assets/critical-images/auth-sign-in-panel"
import OptimizedImage from "@/components/media/OptimizedImage"

/** Figma Sign in panel `5017:1573` — bottom fade over lifestyle photo */
const AUTH_HERO_GRADIENT =
  "linear-gradient(164.96deg, rgba(0, 0, 0, 0) 20.08%, rgb(0, 0, 0) 98.66%)"

/**
 * Right-side marketing panel for Auth shell.
 * Figma Marketing Website — Sign in panel `5017:1573` / copy `5017:1574`.
 *
 * Brush (`5017:1537`) sits behind the first headline line at a snug width so
 * the text stays optically centered inside the stroke.
 */
export function AuthHeroPanel() {
  return (
    <aside className="relative hidden min-w-0 overflow-hidden bg-[#e2eae5] lg:flex lg:h-full lg:flex-[714]">
      <div className="relative size-full">
        <div aria-hidden className="pointer-events-none absolute inset-0">
          <OptimizedImage
            picture={authSignInPanelPicture}
            sizes="(min-width: 1024px) 41.3vw, 0px"
            priority
            alt=""
            className="absolute size-full max-w-none object-cover"
          />
          <div
            className="absolute inset-0"
            style={{ backgroundImage: AUTH_HERO_GRADIENT }}
          />
        </div>

        {/* Copy — Figma `5017:1570` panel-relative: top 71.31% */}
        <div className="absolute left-1/2 top-[71.31%] z-[2] flex w-[74.1%] max-w-[529px] -translate-x-1/2 flex-col items-center gap-5 text-center">
          <h2 className="m-0 flex w-full flex-col items-center font-jakarta text-[clamp(1.75rem,2.2vw,2.375rem)] font-medium leading-[1.29]">
            <span className="relative inline-block whitespace-nowrap text-[#141414]">
              <img
                src={authHeadlineBrush}
                alt=""
                aria-hidden
                width={460}
                height={59}
                className="pointer-events-none absolute left-1/2 top-1/2 z-[-1] h-[1.45em] w-[calc(100%+0.7em)] max-w-none -translate-x-1/2 -translate-y-1/2 object-fill"
              />
              Build guest relationships
            </span>
            <span className="text-white">beyond the table.</span>
          </h2>

          <p className="m-0 max-w-[359px] text-base leading-[22px] text-white">
            Capture feedback, understand your guests and give them a reason to
            return.
          </p>
        </div>
      </div>
    </aside>
  )
}
