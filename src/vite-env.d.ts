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

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_APP_ENV?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
