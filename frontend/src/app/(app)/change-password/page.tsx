import { ChangePasswordForm } from "@/components/auth/ChangePasswordForm";
import { PageHeader } from "@/components/ui";

export default function ChangePasswordPage() {
  return (
    <>
      <PageHeader title="Đổi mật khẩu" description="Sau khi đổi mật khẩu, các refresh token hiện tại sẽ bị thu hồi." />
      <ChangePasswordForm />
    </>
  );
}
