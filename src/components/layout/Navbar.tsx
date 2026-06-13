import { Link, useNavigate } from "react-router-dom";

import logo from "@/assets/svg/logo.svg";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const navButtonClass =
  "h-8 min-h-8 px-3 text-sm sm:h-[38px] sm:min-h-[38px] sm:px-[17px] sm:text-base sm:leading-5";

function Navbar() {
  const navigate = useNavigate();

  return (
    <header className="sticky top-0 z-100 w-full bg-[#141414] shadow-[0_6px_8px_rgba(0,0,0,0.17)]">
      <nav
        aria-label="Main"
        className="mx-auto flex h-16 items-center justify-between gap-3 px-4 sm:h-[78px] sm:gap-4 sm:px-6 md:px-10 lg:px-16 xl:px-45"
      >
        <Link
          to="/"
          className="shrink-0 rounded-sm focus-visible:ring-2 focus-visible:ring-white/40 focus-visible:outline-none"
        >
          <img
            src={logo}
            alt="Tummly"
            width={138}
            height={34}
            className="block h-7 w-auto max-w-[min(138px,42vw)] object-contain sm:h-8 md:h-8.5"
          />
        </Link>

        <div className="flex shrink-0 items-center gap-2 sm:gap-3">
          <Button
            onClick={() => navigate("/request-trial")}
            className={cn(navButtonClass)}
          >
            Request trial
          </Button>

          <Button variant="secondary" asChild className={cn(navButtonClass)}>
            <Link to="/login">Sign in</Link>
          </Button>
        </div>
      </nav>
    </header>
  );
}

export default Navbar;
