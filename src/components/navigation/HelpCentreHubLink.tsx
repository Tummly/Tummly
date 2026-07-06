import { Link, type LinkProps } from "react-router-dom";

import { HELP_CENTRE_URL } from "@/config/support";
import { prefetchHelpCentreHero } from "@/lib/prefetchHelpCentreHero";

function HelpCentreHubLink({
  onMouseEnter,
  onFocus,
  onTouchStart,
  to = HELP_CENTRE_URL,
  ...props
}: LinkProps) {
  const prefetch = () => prefetchHelpCentreHero();

  return (
    <Link
      {...props}
      to={to}
      onMouseEnter={(event) => {
        prefetch();
        onMouseEnter?.(event);
      }}
      onFocus={(event) => {
        prefetch();
        onFocus?.(event);
      }}
      onTouchStart={(event) => {
        prefetch();
        onTouchStart?.(event);
      }}
    />
  );
}

export default HelpCentreHubLink;
