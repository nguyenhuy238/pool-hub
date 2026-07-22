import type { Metadata } from "next";
import { AuthProvider } from "@/components/auth-provider";
import { ToastProvider } from "@/components/toast";
import "./globals.css";

export const metadata: Metadata = {
  title: "PoolHub - Đặt bàn bi-a online nhanh chóng",
  description: "Trung tâm bi-a và giải trí với bàn chuẩn, không gian hiện đại, đồ uống đa dạng và đặt bàn online."
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="vi">
      <body>
        <ToastProvider>
          <AuthProvider>{children}</AuthProvider>
        </ToastProvider>
      </body>
    </html>
  );
}
