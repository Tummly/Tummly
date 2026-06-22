# UK address lookup via Ideal Postcodes backend proxy

Operator Setup Address and Postcode fields use UK address autocomplete and postcode reconciliation. The frontend calls Tummly backend endpoints only; the backend proxies Ideal Postcodes (Address Finder for suggestions, postcode resolution for reconciliation). API keys stay server-side. Public endpoints are rate-limited by IP to limit abuse during unauthenticated setup. Identical autocomplete and postcode requests are cached on the backend to reduce duplicate Ideal Postcodes calls and cost.

**Considered options:** Client-side getAddress.io or Ideal Postcodes (rejected — exposes API key); postcodes.io alone (rejected — no street-level addresses); no caching (rejected — duplicate keystrokes and postcode blurs would multiply cost).
