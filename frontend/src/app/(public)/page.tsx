import type { Metadata } from "next";
import { LandingPage } from "@/components/landing/LandingPage";

export const metadata: Metadata = {
  title: "PoolHub - Đặt bàn bi-a online nhanh chóng",
  description: "Trung tâm bi-a và giải trí với bàn chuẩn, không gian hiện đại, đồ uống đa dạng và đặt bàn online."
};

export default function HomePage() {
  return <LandingPage />;
}
