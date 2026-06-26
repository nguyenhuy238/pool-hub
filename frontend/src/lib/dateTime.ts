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
    hourCycle: "h23"
  }).formatToParts(date);

  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return {
    year: values.year,
    month: values.month,
    day: values.day,
    hour: values.hour,
    minute: values.minute
  };
}

export function formatVietnamDateTime(utcString?: string) {
  const parts = getVietnamParts(utcString);
  if (!parts) return "-";
  return `${parts.hour}:${parts.minute} ${parts.day}/${parts.month}/${parts.year}`;
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

export function vietnamDateRangeToUtcIso(dateValue: string) {
  return {
    startUtc: vietnamDateTimeToUtcIso(dateValue, "00:00"),
    endUtc: vietnamDateTimeToUtcIso(dateValue, "23:59:59")
  };
}

export function getVietnamDateInputValue(date = new Date()) {
  const parts = getVietnamParts(date.toISOString());
  if (!parts) return "";
  return `${parts.year}-${parts.month}-${parts.day}`;
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
