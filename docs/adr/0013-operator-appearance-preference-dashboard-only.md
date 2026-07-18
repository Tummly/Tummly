# Operator appearance preference applies only inside the Operator dashboard

Theme chrome uses `next-themes` at the app root for storage and the Operator dashboard navbar toggle, but the resolved document theme (`.dark` / `color-scheme`) follows the **Operator appearance preference** only on `/single-dashboard` and `/multi-dashboard`. Every other route is forced light — including when the stored preference is Dark or System and the OS is dark — so marketing, auth, and admin never inherit dashboard dark mode after sign-out or navigation.

We rejected clearing the preference on leave (operators would re-choose every visit) and rejected scoping the `dark` class to a dashboard wrapper (portaled drawers/menus would miss it). A boot script plus layout sync keep non-dashboard routes light before paint when storage still holds Dark.
