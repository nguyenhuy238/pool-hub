export type MetricTone = "neutral" | "success" | "warning" | "danger" | "info";

export function MetricCard({ label, value, hint, tone = "neutral" }: {
  label: string;
  value: React.ReactNode;
  hint?: string;
  tone?: MetricTone;
}) {
  return (
    <div className={`card metric dashboard-metric tone-${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      {hint ? <small className="muted-text">{hint}</small> : null}
    </div>
  );
}
