export function isInternalPath(pathname: string) {
  return pathname.startsWith("/admin")
    || pathname.startsWith("/management")
    || pathname.startsWith("/operation")
    || pathname.startsWith("/pos")
    || pathname.startsWith("/dashboard")
    || pathname.startsWith("/profile")
    || pathname.startsWith("/change-password")
    || pathname.startsWith("/notifications")
    || pathname.startsWith("/invoice-display");
}
