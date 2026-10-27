import { AxiosError, type AxiosResponse } from "axios"

/** Test helper: an Axios error whose response body is `data` (Shop API error shape). */
export function axiosErrorWithBody(status: number, data: unknown): AxiosError {
  const response = {
    status,
    data,
    headers: {},
    statusText: "",
    config: {},
  } as AxiosResponse
  return new AxiosError("Request failed", "ERR_BAD_REQUEST", undefined, undefined, response)
}
