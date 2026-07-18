import { Outlet, useLocation } from "react-router-dom";

import { HELP_CENTRE_URL } from "@/config/support";
import { isOperatorDashboardPath } from "@/lib/operatorAppearance";
import Navbar from "../components/layout/Navbar";

function MainLayout() {
  const { pathname } = useLocation();
  const isHelpCentreHub = pathname === HELP_CENTRE_URL;
  const isOperatorDashboard = isOperatorDashboardPath(pathname);

  return (
    <div
      className={
        isOperatorDashboard
          ? "flex h-dvh flex-col overflow-hidden"
          : "flex min-h-dvh flex-col"
      }
    >
      {isOperatorDashboard ? null : (
        <Navbar variant={isHelpCentreHub ? "transparent" : "solid"} />
      )}
      <div
        className={
          isOperatorDashboard
            ? "flex min-h-0 flex-1 flex-col overflow-hidden"
            : "flex flex-1 flex-col"
        }
      >
        <Outlet />
      </div>
    </div>
  );
}

export default MainLayout;
