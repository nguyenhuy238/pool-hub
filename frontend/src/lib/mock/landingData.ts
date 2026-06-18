import type { PricingPlan, PricingPlanRule, TableType, VenueLayoutResponse } from "@/types";

export type LandingService = {
  title: string;
  description: string;
  price: string;
  image: string;
};

export type ReviewItem = {
  name: string;
  rating: number;
  content: string;
  avatar: string;
};

export const promotionMessages = [
  "Tặng 30 phút chơi khi đặt bàn qua Web",
  "Giảm 10% cho nhóm từ 4 người"
];

export const uspItems = [
  { title: "Bàn chuẩn thi đấu", text: "Mặt bàn, bóng và cơ gậy được kiểm tra định kỳ để mỗi trận đấu ổn định." },
  { title: "Không gian thoải mái", text: "Điều hòa, wifi mạnh, khu vực chờ riêng và ánh sáng dễ tập trung." },
  { title: "Phục vụ tận bàn", text: "Đồ uống, đồ ăn nhẹ và combo nhóm được mang tới bàn nhanh." },
  { title: "Đặt bàn online", text: "Chọn ngày giờ, loại bàn và gửi yêu cầu trước để hạn chế chờ đợi." }
];

export const serviceItems: LandingService[] = [
  {
    title: "Billiard / Pool",
    description: "Bàn pool phổ thông và bàn VIP cho nhóm bạn hoặc luyện tập nghiêm túc.",
    price: "Từ 80.000đ/giờ",
    image: "/images/poolhub/hero.png"
  },
  {
    title: "Carom & Snooker",
    description: "Khu vực yên tĩnh hơn, phù hợp người chơi kỹ thuật và trận đấu dài.",
    price: "Từ 90.000đ/giờ",
    image: "/images/poolhub/hero.png"
  },
  {
    title: "Đồ uống & snack",
    description: "Cà phê, trà trái cây, nước ngọt, khô gà, khoai chiên và combo nhóm.",
    price: "Từ 25.000đ",
    image: "/images/poolhub/hero.png"
  }
];

export const reviews: ReviewItem[] = [
  { name: "Minh Quân", rating: 5, content: "Đặt bàn trên web nhanh, tới nơi là có bàn sẵn. Không gian sạch và ánh sáng đẹp.", avatar: "MQ" },
  { name: "Thảo Vy", rating: 5, content: "Nhóm mình đi sau giờ làm, đồ uống lên nhanh và nhân viên xác nhận lịch rất gọn.", avatar: "TV" },
  { name: "Hoàng Nam", rating: 4, content: "Bàn VIP chơi ổn, cơ gậy mới. Sẽ quay lại vì không phải gọi điện hỏi bàn trống.", avatar: "HN" }
];

export const galleryImages = [
  { src: "/images/poolhub/hero.png", alt: "Khu vực bàn pool hiện đại của PoolHub" },
  { src: "/images/poolhub/hero.png", alt: "Quầy bar và không gian giải trí PoolHub" },
  { src: "/images/poolhub/hero.png", alt: "Bàn billiard ánh sáng đẹp cho nhóm bạn" },
  { src: "/images/poolhub/hero.png", alt: "Khu vực chờ và phục vụ đồ uống" }
];

export const mockTableTypes: TableType[] = [
  { tableTypeId: 1, name: "Pool Standard", code: "POOL", defaultCapacity: 4, isActive: true },
  { tableTypeId: 2, name: "Pool VIP", code: "VIP", defaultCapacity: 6, isActive: true },
  { tableTypeId: 3, name: "Carom", code: "CAROM", defaultCapacity: 4, isActive: true }
];

export const mockPricingPlans: PricingPlan[] = [
  { pricingPlanId: 1, name: "Ngay thuong", isDefault: true, isActive: true },
  { pricingPlanId: 2, name: "Cuoi tuan", isDefault: false, isActive: true },
  { pricingPlanId: 3, name: "Gio vang truoc 17h", isDefault: false, isActive: true }
];

export const mockPricingRules: PricingPlanRule[] = [
  { pricingPlanRuleId: 1, pricingPlanId: 1, tableTypeId: 1, dayOfWeek: 1, hourlyRate: 80000, startTime: "09:00:00", endTime: "17:00:00", isActive: true },
  { pricingPlanRuleId: 2, pricingPlanId: 1, tableTypeId: 2, dayOfWeek: 1, hourlyRate: 120000, startTime: "17:00:00", endTime: "24:00:00", isActive: true },
  { pricingPlanRuleId: 3, pricingPlanId: 2, tableTypeId: 1, dayOfWeek: 6, hourlyRate: 110000, startTime: "09:00:00", endTime: "24:00:00", isActive: true },
  { pricingPlanRuleId: 4, pricingPlanId: 3, tableTypeId: 3, dayOfWeek: 1, hourlyRate: 70000, startTime: "09:00:00", endTime: "17:00:00", isActive: true }
];

export const mockVenueLayout: VenueLayoutResponse = {
  totalTables: 12,
  availableTables: 7,
  occupiedTables: 3,
  fetchedAtUtc: new Date().toISOString(),
  floors: [
    {
      floorId: 1,
      floorName: "Tang 1",
      displayOrder: 1,
      zones: [
        {
          zoneId: 1,
          zoneName: "Khu Pool",
          displayOrder: 1,
          tables: [
            { tableId: 1, tableCode: "P01", tableName: "Pool 01", tableTypeId: 1, tableTypeName: "Pool Standard", capacity: 4, operationalStatus: 1, isActive: true },
            { tableId: 2, tableCode: "P02", tableName: "Pool 02", tableTypeId: 1, tableTypeName: "Pool Standard", capacity: 4, operationalStatus: 2, isActive: true, activeSessionId: 12 },
            { tableId: 3, tableCode: "V01", tableName: "VIP 01", tableTypeId: 2, tableTypeName: "Pool VIP", capacity: 6, operationalStatus: 3, isActive: true },
            { tableId: 4, tableCode: "C01", tableName: "Carom 01", tableTypeId: 3, tableTypeName: "Carom", capacity: 4, operationalStatus: 4, isActive: true }
          ]
        }
      ]
    }
  ]
};
