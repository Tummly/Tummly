import { useEffect, useState } from "react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { getFeedback, downloadQrCode } from "@/api/dashboardApi";
import type { FeedbackItem } from "@/types/dashboard";

interface DashboardContentProps {
  locationId: number;
  linkToken: string;
  locationName: string;
}

type LoadState = "loading" | "loaded" | "error";

function formatDate(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function contactTypeBadge(type: FeedbackItem["contactType"]) {
  if (type === "Email") return <Badge variant="ready">Email</Badge>;
  if (type === "Phone") return <Badge variant="secondary">Phone</Badge>;
  return <Badge variant="outline">Other</Badge>;
}

export function DashboardContent({
  locationId,
  linkToken,
  locationName,
}: DashboardContentProps) {
  const [total, setTotal] = useState(0);
  const [recent, setRecent] = useState<FeedbackItem[]>([]);
  const [state, setState] = useState<LoadState>("loading");
  const [qrDownloading, setQrDownloading] = useState(false);

  useEffect(() => {
    let active = true;

    setState("loading");

    void (async () => {
      try {
        const result = await getFeedback(locationId);
        if (active) {
          setTotal(result.total);
          setRecent(result.recent);
          setState("loaded");
        }
      } catch {
        if (active) {
          setState("error");
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [locationId]);

  const guestLink = `${window.location.origin}/scan/${linkToken}`;

  const handleDownloadQr = async () => {
    try {
      setQrDownloading(true);
      const blob = await downloadQrCode(locationId);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `QR_${locationName}.png`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch {
      // silently fail — button state resets
    } finally {
      setQrDownloading(false);
    }
  };

  if (state === "loading") {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-muted-foreground">Loading feedback…</p>
      </div>
    );
  }

  if (state === "error") {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <p className="text-destructive">
          Could not load feedback. Please try again.
        </p>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setState("loading")}
        >
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      {/* Stats + actions */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <span className="text-4xl font-bold text-foreground">
            {total}
          </span>
          <span className="text-sm text-muted-foreground">
            {total === 1 ? "submission" : "submissions"}
          </span>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleDownloadQr}
            disabled={qrDownloading}
          >
            {qrDownloading ? "Downloading…" : "Download QR"}
          </Button>
          <Button
            variant="secondary"
            size="sm"
            asChild
          >
            <a href={guestLink} target="_blank" rel="noopener noreferrer">
              Open guest link
            </a>
          </Button>
        </div>
      </div>

      {/* Recent feedback */}
      <div className="rounded-lg border border-border bg-card">
        <div className="border-b border-border px-4 py-3">
          <h3 className="text-sm font-semibold text-foreground">
            Recent feedback
          </h3>
        </div>

        {recent.length === 0 ? (
          <div className="px-4 py-12 text-center">
            <p className="text-sm text-muted-foreground">
              No feedback yet. Feedback will appear here once guests
              scan the QR code and submit.
            </p>
          </div>
        ) : (
          <ul className="divide-y divide-border">
            {recent.map((item) => (
              <li
                key={item.id}
                className="flex flex-col gap-2 px-4 py-4"
              >
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-foreground">
                      {item.guestName}
                    </span>
                    {contactTypeBadge(item.contactType)}
                  </div>
                  <span className="text-xs text-muted-foreground">
                    {formatDate(item.createdAt)}
                  </span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {item.comment}
                </p>
                <p className="text-xs text-muted-foreground">
                  Contact: {item.guestContact}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
