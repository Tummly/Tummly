# Query attachments stored in S3-compatible object storage

Signed-in operators may attach files when submitting a **Help Centre query** from **Contact us**. We store file bytes in **DigitalOcean Spaces** (S3-compatible API) and persist metadata in SQL Server on `HelpCentreQueryAttachment`.

Guest submissions and **Operator query reply** follow-ups do not accept attachments in v1. The API uploads through the backend (multipart form post) rather than browser-to-bucket presigned URLs so we can enforce auth, size limits, and content-type rules in one place before anything is written.

We rejected storing blobs in SQL Server (backup bloat, awkward downloads) and a local filesystem fallback on Railway (ephemeral containers). When Spaces is not configured, submit with attachments **fails fast** with a clear error; submit without attachments still works.

## Limits (v1)

- Five files per query, ten megabytes each, fifty megabytes total
- JPEG, PNG, WebP, GIF, PDF only
- Support and the submitting operator download via authenticated API routes (not public bucket URLs)

## Configuration

`ObjectStorage__Endpoint`, `ObjectStorage__Bucket`, `ObjectStorage__AccessKey`, `ObjectStorage__SecretKey`, `ObjectStorage__Region` (e.g. `lon1` for DO Spaces).
