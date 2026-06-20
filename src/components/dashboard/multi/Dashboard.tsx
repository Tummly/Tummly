import { useEffect, useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { DashboardContent } from "../DashboardContent";
import { getLocations } from "@/api/dashboardApi";
import type { LocationItem } from "@/types/dashboard";

function MultiDashboard() {
  const [locations, setLocations] = useState<LocationItem[]>([]);
  const [selectedId, setSelectedId] = useState<string>("");
  const [state, setState] = useState<"loading" | "loaded" | "error">(
    "loading"
  );

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const result = await getLocations();
        if (active) {
          const sorted = [...result.locations].sort(
            (a, b) =>
              new Date(a.createdAt).getTime() -
              new Date(b.createdAt).getTime()
          );
          setLocations(sorted);
          if (sorted.length > 0) {
            setSelectedId(String(sorted[0].id));
          }
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

  const selectedLocation = useMemo(
    () => locations.find((l) => String(l.id) === selectedId) ?? null,
    [locations, selectedId]
  );

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

  if (locations.length === 0) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-muted-foreground">
          No locations found for your account.
        </p>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold text-foreground">
          {selectedLocation?.locationName ?? "Dashboard"}
        </h1>

        <div className="w-full sm:w-64">
          <Select
            value={selectedId}
            onValueChange={setSelectedId}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select location" />
            </SelectTrigger>
            <SelectContent>
              {locations.map((loc) => (
                <SelectItem
                  key={loc.id}
                  value={String(loc.id)}
                >
                  {loc.locationName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {selectedLocation && (
        <DashboardContent
          locationId={selectedLocation.id}
          linkToken={selectedLocation.linkToken}
          locationName={selectedLocation.locationName}
        />
      )}
    </div>
  );
}

export default MultiDashboard;
