# PoolHub Roadmap and Progress

## 1. Trạng thái hiện tại
Repo đang ở trạng thái backend và frontend đã chạy được cho các luồng cốt lõi. Đây là snapshot hiện tại, không phải roadmap cam kết tương lai.

## 2. Mốc đã đạt được
- Week 1: chốt domain PoolHub, roles và nghiệp vụ chính.
- Week 2: có seed script và ERD 27 bảng.
- Week 3: dựng layered backend, service/repository pattern, core APIs.
- Week 4: thêm JWT auth, refresh token, role authorization và demo auth.

## 3. Hiện trạng thực thi trong repo
- Backend có controller/service/repository thật cho nhiều module vận hành.
- Frontend có page và service layer để gọi booking, session, invoice, dashboard và venue APIs.
- Seed data có account demo và dữ liệu vận hành mẫu.

## 4. Release-note style summary
- Authentication và refresh token đã hoạt động.
- Booking calendar/public booking đã có trên backend và frontend.
- Session lifecycle, invoice generation và payment flow đã xuất hiện.
- Dashboard và report endpoints đã có service map ở frontend.

## 5. Cách đọc tiến độ cho AI
Khi AI cần suy luận mức độ hoàn thiện, hãy xem đây là mức: core flows đã có, nhưng chuẩn hóa contract, test coverage và đồng bộ legacy docs vẫn đang cần làm tiếp.