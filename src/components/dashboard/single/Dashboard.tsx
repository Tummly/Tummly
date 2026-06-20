import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { DashboardContent } from "../DashboardContent";
import { getLocations } from "@/api/dashboardApi";
import type { LocationItem } from "@/types/dashboard";

function Dashboard() {
  const [location, setLocation] = useState<LocationItem | null>(null);
  const [state, setState] = useState<"loading" | "loaded" | "error">(
    "loading"
  );

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const result = await getLocations();
        if (active) {
          setLocation(result.locations[0] ?? null);
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
  }, []);

  if (state === "loading") {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-muted-foreground">Loading…</p>
      </div>
    );
  }

  if (state === "error") {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <p className="text-destructive">
          Could not load your dashboard. Please try again.
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

  if (!location) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-muted-foreground">
          No location found for your account.
        </p>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <h1 className="mb-6 text-2xl font-bold text-foreground">
        {location.locationName}
      </h1>
      <DashboardContent
        locationId={location.id}
        linkToken={location.linkToken}
        locationName={location.locationName}
      />
    </div>
  );
}

export default Dashboard;
