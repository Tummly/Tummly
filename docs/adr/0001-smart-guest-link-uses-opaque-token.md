# Smart Guest Link uses an opaque per-location token

The Smart Guest Link is the public URL a guest reaches by scanning a location's QR code. We decided to key it on an opaque random token stored per `RestaurantLocation`, not the location's sequential numeric primary key. URLs become `https://tummly.com/scan/{token}`.

The numeric ID made links enumerable — incrementing the integer exposed every Tummly location and enabled automated feedback spam. A random token makes enumeration computationally infeasible and survives location renames without breaking printed QR codes. We rejected a human-readable slug because slug collisions across operators require uniqueness logic and renaming a location would invalidate already-printed QRs. Operators never type the URL (they scan QR codes), so readability carries no value.

The token is generated once during Guest Loop provisioning and stored on `RestaurantLocation`. It does not replace the numeric primary key, which remains the internal identifier.
