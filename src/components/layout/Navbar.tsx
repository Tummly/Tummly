import { Link, useNavigate } from "react-router-dom";

import logo from "@/assets/svg/logo.svg";
import SignInLink from "@/components/auth/SignInLink";
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink";
import { Button } from "@/components/ui/button";
import { HELP_CENTRE_URL } from "@/config/support";
import { cn } from "@/lib/utils";
import { marketingSectionInset } from "@/lib/marketing-layout";
import { clearAuthSession } from "@/pages/utils/authHelpers";
import { useAuthStore } from "@/stores/authStore";

const navButtonClass =
  "h-[35px] min-h-[35px] px-3 text-sm lg:h-[38px] lg:min-h-[38px] lg:px-[17px] lg:text-base lg:leading-5";

type NavbarProps = {
  showRequestTrial?: boolean;
  variant?: "solid" | "transparent";
};

function Navbar({ showRequestTrial = true, variant = "solid" }: NavbarProps) {
  const isTransparent = variant === "transparent";
  const navigate = useNavigate();
  const token = useAuthStore((state) => state.token);
  const role = useAuthStore((state) => state.role);
  const accountType = useAuthStore((state) => state.accountType);
  const hasHydrated = useAuthStore((state) => state._hasHydrated);

  const isSignedIn =
    hasHydrated
    && Boolean(token)
    && (role === "ADMIN" || role === "USER" || role === "SUPPORT");

  const homePath = (() => {
    if (!isSignedIn) {
      return "/";
    }

    if (role === "ADMIN") {
      return "/admin-dashboard";
    }

    if (role === "SUPPORT") {
      return "/support-dashboard";
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
    <header
      className={cn(
        "top-0 z-100 w-full",
        isTransparent
          ? "absolute bg-transparent shadow-none"
          : "sticky bg-[#141414] shadow-[0_6px_8px_rgba(0,0,0,0.17)]"
      )}
    >
      <nav
        aria-label="Main"
        className={cn(
          "mx-auto flex h-[77px] items-center justify-between gap-3 lg:h-[78px] lg:gap-4",
          marketingSectionInset
        )}
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
            <>
              {role === "USER" && (
                <Link
                  to={HELP_CENTRE_URL}
                  className="rounded-sm px-1 text-sm font-medium text-white no-underline hover:underline focus-visible:ring-2 focus-visible:ring-white/40 focus-visible:outline-none lg:text-base"
                >
                  Help
                </Link>
              )}
              <Button
                variant="secondary"
                onClick={handleLogout}
                className={cn(navButtonClass)}
              >
                Log out
              </Button>
            </>
          ) : (
            <>
              {showRequestTrial ? (
                <Button asChild className={cn(navButtonClass)}>
                  <RequestTrialLink>Request trial</RequestTrialLink>
                </Button>
              ) : null}

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
