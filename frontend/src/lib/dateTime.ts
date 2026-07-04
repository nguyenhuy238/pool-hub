export const VIETNAM_TIME_ZONE = "Asia/Ho_Chi_Minh";
const VIETNAM_UTC_OFFSET = "+07:00";

function ensureUtcString(value?: string) {
  if (!value) return "";
  return /z$/i.test(value) || /[+-]\d{2}:\d{2}$/.test(value) ? value : `${value}Z`;
}

function pad(value: number) {
  return value.toString().padStart(2, "0");
}

function getVietnamParts(utcString?: string) {
  if (!utcString) return null;
  const date = new Date(ensureUtcString(utcString));
  if (Number.isNaN(date.getTime())) return null;
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: VIETNAM_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hourCycle: "h23"
  }).formatToParts(date);

  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return {
    year: values.year,
    month: values.month,
    day: values.day,
    hour: values.hour,
    minute: values.minute,
    second: values.second || "00"
  };
}

export function formatVietnamDateTimeWithSeconds(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return "-";
  return `${parts.hour}:${parts.minute}:${parts.second} ${parts.day}/${parts.month}/${parts.year}`;
}

export function formatVietnamDateTime(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return "-";
  return `${parts.hour}:${parts.minute} ${parts.day}/${parts.month}/${parts.year}`;
}

export function parseUtcFromApi(value?: string | null) {
  if (!value) return null;
  const date = new Date(ensureUtcString(value));
  return Number.isNaN(date.getTime()) ? null : date;
}

export function utcTimestampMs(value?: string | null) {
  return parseUtcFromApi(value)?.getTime() ?? NaN;
}

export function formatDateTimeLocal(value?: string | null, timezone = VIETNAM_TIME_ZONE) {
  const date = parseUtcFromApi(value);
  if (!date) return "-";
  return new Intl.DateTimeFormat("vi-VN", {
    timeZone: timezone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23"
  }).format(date);
}

export function formatVietnamDate(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return "-";
  return `${parts.day}/${parts.month}/${parts.year}`;
}

export function formatVietnamTime(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return "-";
  return `${parts.hour}:${parts.minute}`;
}

export function utcToVietnamDatetimeLocal(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return "";
  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
}

export function vietnamDatetimeLocalToUtcIso(localValue: string) {
  if (!localValue) return "";
  const normalized = localValue.length === 16 ? `${localValue}:00` : localValue;
  return new Date(`${normalized}${VIETNAM_UTC_OFFSET}`).toISOString();
}

export function vietnamDateTimeToUtcIso(dateValue: string, timeValue = "00:00") {
  return vietnamDatetimeLocalToUtcIso(`${dateValue}T${timeValue}`);
}

export function formatDateLocal(value?: string | null, timezone = VIETNAM_TIME_ZONE) {
  const date = parseUtcFromApi(value);
  if (!date) return "-";
  return new Intl.DateTimeFormat("vi-VN", {
    timeZone: timezone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit"
  }).format(date);
}

export function toUtcIsoFromDate(date: Date) {
  return date.toISOString();
}

export function localDateTimeToUtcIso(localDate: string, localTime = "00:00", timezone = VIETNAM_TIME_ZONE) {
  if (timezone !== VIETNAM_TIME_ZONE) {
    throw new Error(`Unsupported timezone: ${timezone}`);
  }
  return vietnamDateTimeToUtcIso(localDate, localTime);
}

export function localDateRangeToUtcRange(dateValue: string, timezone = VIETNAM_TIME_ZONE) {
  if (timezone !== VIETNAM_TIME_ZONE) {
    throw new Error(`Unsupported timezone: ${timezone}`);
  }
  return {
    fromUtc: vietnamDateTimeToUtcIso(dateValue, "00:00"),
    toUtc: vietnamDateTimeToUtcIso(addDaysToVietnamDateInput(dateValue, 1), "00:00")
  };
}

export function vietnamDateRangeToUtcIso(dateValue: string) {
  const range = localDateRangeToUtcRange(dateValue);
  return {
    startUtc: range.fromUtc,
    endUtc: range.toUtc
  };
}

export function getVietnamDateInputValue(date = new Date()) {
  const parts = getVietnamParts(date.toISOString());
  if (!parts) return "";
  return `${parts.year}-${parts.month}-${parts.day}`;
}

export function getCurrentVietnamMonthRange() {
  const current = getVietnamDateInputValue();
  const [year, month] = current.split("-").map(Number);
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return {
    fromDate: `${year}-${pad(month)}-01`,
    toDate: `${year}-${pad(month)}-${pad(lastDay)}`
  };
}

export function addDaysToVietnamDateInput(dateValue: string, days: number) {
  const utc = new Date(vietnamDateTimeToUtcIso(dateValue, "00:00"));
  utc.setUTCDate(utc.getUTCDate() + days);
  return getVietnamDateInputValue(utc);
}

export function getVietnamHourOfDay(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return 0;
  return Number(parts.hour) + Number(parts.minute) / 60;
}

export function getCurrentVietnamHourOfDay() {
  const parts = getVietnamParts(new Date().toISOString());
  if (!parts) return 0;
  return Number(parts.hour) + Number(parts.minute) / 60;
}

export function getVietnamDayOfWeek(dateValue: string) {
  return new Date(vietnamDateTimeToUtcIso(dateValue, "00:00")).getUTCDay();
}
