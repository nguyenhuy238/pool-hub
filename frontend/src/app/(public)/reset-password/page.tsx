import { Suspense } from "react";
import { ResetPasswordForm } from "./reset-password-form";

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={<div className="auth-wrap"><div className="state-card">Đang tải biểu mẫu...</div></div>}>
      <ResetPasswordForm />
    </Suspense>
  );
}
