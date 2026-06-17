import { PublicHeader } from "@/components/landing/PublicHeader";

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="public-shell">
      <PublicHeader />
      {children}
    </div>
  );
}
