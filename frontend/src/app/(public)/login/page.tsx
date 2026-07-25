import { permanentRedirect } from "next/navigation";

export default function CustomerLoginPage() {
  permanentRedirect("/?login=customer");
}
