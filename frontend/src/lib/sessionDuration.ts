import { utcTimestampMs } from "@/lib/dateTime";

export function getSessionElapsedMs(startedAtUtc?: string, endedAtUtc?: string) {
  if (!startedAtUtc) return 0;
  const startedAt = utcTimestampMs(startedAtUtc);
  if (!Number.isFinite(startedAt)) return 0;

  const endedAt = endedAtUtc ? utcTimestampMs(endedAtUtc) : Date.now();
  if (!Number.isFinite(endedAt)) return 0;

  return Math.max(0, endedAt - startedAt);
}

export function formatElapsedDuration(startedAtUtc?: string, endedAtUtc?: string) {
  const totalSeconds = Math.floor(getSessionElapsedMs(startedAtUtc, endedAtUtc) / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  return [hours, minutes, seconds].map((value) => String(value).padStart(2, "0")).join(":");
}
