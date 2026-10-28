import { Navigate, useLocation } from "react-router-dom"

/** Old `/unsubscribe?t=` email links land on `/preferences` with the same query. */
export function UnsubscribeRedirect() {
  const { search } = useLocation()

  return <Navigate to={{ pathname: "/preferences", search }} replace />
}
