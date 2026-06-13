import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import type { UserRole } from "../../types/auth";

interface RoleRouteProps {
  children: ReactNode;
  role?: UserRole;
}

const RoleRoute = ({ children, role }: RoleRouteProps) => {
  const token = localStorage.getItem("token");
  const userRole = localStorage.getItem("role");

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (role && userRole !== role) {
    return <Navigate to="/login" replace />;
  }

  return children;
};

export default RoleRoute;
