import { Outlet, useLocation } from "react-router-dom";

import { HELP_CENTRE_URL } from "@/config/support";
import Navbar from "../components/layout/Navbar";

function MainLayout() {
  const { pathname } = useLocation();
  const isHelpCentreHub = pathname === HELP_CENTRE_URL;

  return (
    <>
      <Navbar variant={isHelpCentreHub ? "transparent" : "solid"} />
      <Outlet />
    </>
  );
}

export default MainLayout;
