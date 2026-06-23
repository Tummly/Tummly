import { Link } from "react-router-dom"

import {
  authHeroFrame267Picture,
  authHeroFrame268Picture,
} from "@/assets/critical-images"
import authHeroLogo from "@/assets/images/auth-hero-logo.png"
import OptimizedImage from "@/components/media/OptimizedImage"

/** Figma node 557:2115 — hero overlay */
const AUTH_HERO_GRADIENT =
  "linear-gradient(177.49778957221042deg, rgb(20, 20, 20) 32.324%, rgba(20, 20, 20, 0) 98.478%)"

export function AuthHeroPanel() {
  return (
    <aside className="relative hidden min-w-0 overflow-hidden bg-[#141414] lg:flex lg:h-full lg:flex-[776]">
      {/* Matches Figma Frame 267: flex-col, items-start, p-[90px], size-full */}
      <div className="relative flex size-full flex-col items-start p-[90px]">
        <div aria-hidden className="pointer-events-none absolute inset-0">
          <OptimizedImage
            picture={authHeroFrame267Picture}
            sizes="(min-width: 1024px) 45.38vw, 0px"
            priority
            alt=""
            className="absolute size-full max-w-none object-cover"
          />
          <OptimizedImage
            picture={authHeroFrame268Picture}
            sizes="(min-width: 1024px) 45.38vw, 0px"
            priority
            alt=""
            className="absolute size-full max-w-none object-cover"
          />
          <div
            className="absolute inset-0"
            style={{ backgroundImage: AUTH_HERO_GRADIENT }}
          />
        </div>

        {/* Figma node 557:2116 — logo, headline, subcopy in one gap-[22px] stack */}
        <div className="relative flex w-full shrink-0 flex-col items-start gap-[22px]">
          <Link
            to="/"
            className="inline-flex shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40"
          >
            <img
              src={authHeroLogo}
              alt="Tummly"
              width={190}
              height={35}
              className="block h-[35px] w-auto"
            />
          </Link>

          <h1 className="m-0 min-w-full text-[36px] font-semibold leading-normal text-white">
            Turn everyday orders into direct guest relationships.
          </h1>

          <p className="m-0 max-w-[517px] text-lg leading-6 text-white">
            Manage guest capture, private feedback, offers and campaigns from
            one restaurant workspace.
          </p>
        </div>
      </div>
    </aside>
  )
}
