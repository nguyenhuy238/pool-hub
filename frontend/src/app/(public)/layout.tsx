"use client";

import { Suspense } from "react";
import { PublicHeader } from "@/components/landing/PublicHeader";
import { usePathname } from "next/navigation";

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isAuthPage = pathname === "/login" || pathname === "/admin/login";
  return (
    <div className="public-shell">
      {!isAuthPage ? <Suspense fallback={null}><PublicHeader /></Suspense> : null}
      {children}
    </div>
  );
}
