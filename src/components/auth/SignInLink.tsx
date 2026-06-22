import { Link, type LinkProps } from "react-router-dom";

import { prefetchAuthImages } from "@/lib/prefetchAuthImages";

function SignInLink({ onMouseEnter, onFocus, onTouchStart, ...props }: LinkProps) {
  const prefetch = () => prefetchAuthImages();

  return (
    <Link
      {...props}
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

export default SignInLink;
