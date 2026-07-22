export function DashboardError({ message }: { message: string }) {
  return <div className="state-card error">{message}</div>;
}
