import { permanentRedirect } from "next/navigation";

export default function ResetPasswordPage() {
  permanentRedirect("/forgot-password");
}
