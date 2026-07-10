# Dual-maintained legal copy

Counsel-approved **Legal documents** live as `.docx` files under `backend/TummlyBackend/Assets/legal-docs` and are served for public download. On-page **Legal page** copy is maintained separately as frontend content modules under `src/content/legal/`, rendered through `LegalPageShell`.

We rejected generating the on-page copy from the `.docx` at build or request time: the Legal page layout needs structured sections, TOC ids, prose summaries of tables, and in-app cross-links that the Word originals do not encode. Serving HTML from the backend would also couple marketing-site rendering to API availability.

When legal text changes, update the matching `.docx` and the matching frontend content module in the same change. The downloadable file remains the counsel-approved original; the on-page modules are the presentation layer for `/privacy`, `/terms`, and `/cookie-policy`.
