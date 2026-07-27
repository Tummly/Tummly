# Operator notes use soft-delete and in-place edit

**Feedback internal notes** and **Location Guest notes** are staff operational memory with timeline side effects, not disposable drafts. We soft-delete (hide in product UI, retain the row for audit; no operator restore in v1) and edit by overwriting the body in place with last-editor metadata — not hard-delete and not a version store. Soft-delete emits a delete activity beat (Feedback derived history keeps note-added and adds note-deleted; Location Guest activity recorder appends note-deleted); body edits stay silent except an “Edited” cue on the note. Hard-delete was rejected because it forces awkward history rewrites; full versioning was rejected as overkill for typo fixes.

## Considered Options

- Hard-delete / hard overwrite with no audit retention
- Soft-delete + full body version history
- Soft-delete + in-place overwrite (chosen)
