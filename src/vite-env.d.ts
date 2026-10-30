/// <reference types="vite/client" />

type ImagetoolsPicture = {
  sources: Record<string, string>;
  img: {
    src: string;
    w: number;
    h: number;
  };
};

declare module "*.png?as=picture" {
  const picture: ImagetoolsPicture;
  export default picture;
}

declare module "*.jpg?as=picture" {
  const picture: ImagetoolsPicture;
  export default picture;
}

declare module "*.jpeg?as=picture" {
  const picture: ImagetoolsPicture;
  export default picture;
}

declare module "*.webp?as=picture" {
  const picture: ImagetoolsPicture;
  export default picture;
}

declare module "*.png?as=srcset" {
  const srcset: string;
  export default srcset;
}

declare module "*.jpg?as=srcset" {
  const srcset: string;
  export default srcset;
}

declare module "*.jpeg?as=srcset" {
  const srcset: string;
  export default srcset;
}

declare module "*.webp?as=srcset" {
  const srcset: string;
  export default srcset;
}

declare module "*.png?*" {
  const output: ImagetoolsPicture | string;
  export default output;
}

declare module "*.jpg?*" {
  const output: ImagetoolsPicture | string;
  export default output;
}

declare module "*.jpeg?*" {
  const output: ImagetoolsPicture | string;
  export default output;
}

declare module "*.webp?*" {
  const output: ImagetoolsPicture | string;
  export default output;
}

/** vite-imagetools: any query ending in `&as=srcset` / `&format=webp` (one `*` only). */
declare module "*&as=srcset" {
  const srcset: string;
  export default srcset;
}

declare module "*&format=webp" {
  const src: string;
  export default src;
}

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_APP_ENV?: string;
  readonly VITE_GA_MEASUREMENT_ID?: string;
  readonly VITE_TEAM_PERMISSIONS_MATRIX_EDIT?: string;
  /** Public marketing media base (Azure Blob `marketing` container, no trailing slash). */
  readonly VITE_MARKETING_MEDIA_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
