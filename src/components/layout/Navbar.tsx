import { Link, useNavigate } from "react-router-dom";

import logo from "@/assets/svg/logo.svg";
import SignInLink from "@/components/auth/SignInLink";
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { clearAuthSession } from "@/pages/utils/authHelpers";
import { useAuthStore } from "@/stores/authStore";

const navButtonClass =
  "h-8 min-h-8 px-3 text-sm sm:h-[38px] sm:min-h-[38px] sm:px-[17px] sm:text-base sm:leading-5";

function Navbar() {
  const navigate = useNavigate();
  const token = useAuthStore((state) => state.token);
  const role = useAuthStore((state) => state.role);
  const accountType = useAuthStore((state) => state.accountType);
  const hasHydrated = useAuthStore((state) => state._hasHydrated);

  const isSignedIn =
    hasHydrated
    && Boolean(token)
    && (role === "ADMIN" || role === "USER");

  const homePath = (() => {
    if (!isSignedIn) {
      return "/";
    }

    if (role === "ADMIN") {
      return "/admin-dashboard";
    }

    if (accountType === "Single") {
      return "/single-dashboard";
    }

    return "/multi-dashboard";
  })();

  const handleLogout = () => {
    clearAuthSession();
    navigate("/login", { replace: true });
  };

  return (
    <header className="sticky top-0 z-100 w-full bg-[#141414] shadow-[0_6px_8px_rgba(0,0,0,0.17)]">
      <nav
        aria-label="Main"
        className="mx-auto flex h-16 items-center justify-between gap-3 px-4 sm:h-[78px] sm:gap-4 sm:px-6 md:px-10 lg:px-16 xl:px-45"
      >
        <Link
          to={homePath}
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
          {isSignedIn ? (
            <Button
              variant="secondary"
              onClick={handleLogout}
              className={cn(navButtonClass)}
            >
              Log out
            </Button>
          ) : (
            <>
              <Button asChild className={cn(navButtonClass)}>
                <RequestTrialLink>Request trial</RequestTrialLink>
              </Button>

              <Button variant="secondary" asChild className={cn(navButtonClass)}>
                <SignInLink to="/login">Sign in</SignInLink>
              </Button>
            </>
          )}
        </div>
      </nav>
    </header>
  );
}

export default Navbar;
