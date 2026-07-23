"use client";

import { CustomerShell } from "@/components/customer/CustomerShell";
import { ProtectedRoute } from "@/components/guards";
import { ROLES } from "@/lib/auth/constants";

export default function CustomerLayout({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute roles={[ROLES.CUSTOMER]} loginPath="/?login=customer">
      <CustomerShell>{children}</CustomerShell>
    </ProtectedRoute>
  );
}
