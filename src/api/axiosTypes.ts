import "axios"

declare module "axios" {
  export interface AxiosRequestConfig {
    /** Suppress 401 session clear + /login redirect (login-wizard calls). */
    skipAuthRedirect?: boolean
  }
}
