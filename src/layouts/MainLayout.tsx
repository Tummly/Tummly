import { Outlet, useLocation } from "react-router-dom";

import { HELP_CENTRE_URL } from "@/config/support";
import Navbar from "../components/layout/Navbar";

function MainLayout() {
  const { pathname } = useLocation();
  const isHelpCentreHub = pathname === HELP_CENTRE_URL;

  return (
    <div className="flex min-h-dvh flex-col">
      <Navbar variant={isHelpCentreHub ? "transparent" : "solid"} />
      <div className="flex flex-1 flex-col">
        <Outlet />
      </div>
    </div>
  );
}

export default MainLayout;
