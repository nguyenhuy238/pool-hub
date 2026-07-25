import { ChangePasswordForm } from "@/components/auth/ChangePasswordForm";

export default function CustomerChangePasswordPage() {
  return (
    <section>
      <div className="customer-page-heading">
        <div>
          <span className="customer-kicker">Bảo mật tài khoản</span>
          <h2>Đổi mật khẩu</h2>
          <p>Sau khi đổi mật khẩu, bạn sẽ được đăng xuất và cần đăng nhập lại bằng mật khẩu mới.</p>
        </div>
      </div>
      <ChangePasswordForm />
    </section>
  );
}
