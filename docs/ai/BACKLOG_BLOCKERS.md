# PoolHub Backlog, Blockers and Risks

## 1. Backlog hiện tại theo dấu vết repo
- Đồng bộ các path legacy giữa frontend service layer và controller thật.
- Chuẩn hóa contract response cho list endpoint có paging.
- Mở rộng test coverage cho auth, booking, session, invoice và role matrix.
- Hoàn thiện docs cho các flow báo cáo và vận hành.
- Đồng bộ swagger legacy với code hiện tại nếu swagger còn lệch.

## 2. Blockers / bottlenecks đang thấy
- Tài liệu cũ còn ghi trạng thái backend là skeleton net8.0, có thể gây hiểu nhầm cho AI mới.
- Một số service/frontend path vẫn có dấu hiệu mapping cũ hoặc chưa đồng nhất.
- Không có backlog/sprint board chính thức trong repo, nên tiến độ task phải suy ra từ code và migrations.

## 3. Rủi ro kỹ thuật
- Sai lệch giữa controller hiện tại và swagger cũ có thể làm frontend gọi nhầm endpoint.
- Thiếu test cho các luồng multi-step có thể gây lỗi khi mở rộng transaction.
- Các luồng tiền, invoice, payment và auth cần cẩn trọng vì tác động nghiệp vụ cao.
- Audit tự sinh dựa vào DbContext cần được giữ ổn định khi thêm entity mới.

## 4. Ưu tiên xử lý tiếp theo
1. Sync contract giữa frontend service và controller thực.
2. Tăng test cho core flows.
3. Chuẩn hóa docs/API contract.
4. Hoàn thiện reports và admin dashboards.