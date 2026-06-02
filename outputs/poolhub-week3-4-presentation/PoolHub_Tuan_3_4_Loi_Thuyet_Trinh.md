# Lời thuyết trình PoolHub tuần 3-4

## Slide 1 - Mở đầu
Em xin trình bày báo cáo phát triển tuần 3 và tuần 4 của hệ thống PoolHub. Trọng tâm của hai tuần này là backend: xây kiến trúc API, triển khai các core APIs, sau đó bổ sung authentication và authorization bằng JWT. Hiện tại solution đã build và chạy test pass, gồm unit test cho AuthService và integration test cho Swagger.

## Slide 2 - Bối cảnh roadmap
Theo roadmap 10 tuần, tuần 1-2 tập trung phân tích yêu cầu, ERD và API specification. Tuần 3-4 là giai đoạn chuyển từ thiết kế sang backend chạy được. Vì vậy, tiêu chí đánh giá không phải UI, mà là API structure, EF Core, CRUD, JWT, protected APIs và phân quyền theo role.

## Slide 3 - Đối chiếu tuần 3
Tuần 3 yêu cầu ASP.NET Web API structure, EF Core, service/repository pattern, CRUD APIs, DTO, validation, async/await và migration. Trong repo hiện đã có project PoolHub.API, Core, Infrastructure, Services và Shared. DbContext có schema 27 bảng, có migration, nhiều controller CRUD và service async. Một số điểm vẫn cần cải thiện là CRUD chưa đồng đều ở mọi module và validation domain chưa nhất quán như Auth DTO.

## Slide 4 - Kiến trúc backend
Kiến trúc hiện tại theo hướng layered architecture. API chịu trách nhiệm controller, middleware, Swagger và JWT config. Core chứa entities, DTOs và interfaces. Services chứa AuthService, CrudService và các domain services. Infrastructure chứa DbContext, migration, seed data. Shared chứa ApiResponse, exceptions, constants. Cách tách này giúp controller mỏng hơn và dễ mở rộng nghiệp vụ.

## Slide 5 - Core APIs
Các API hiện tại đã bao phủ nhiều nhóm dữ liệu vận hành chính: venue, booking, product, session, order, invoice, notification và audit logs. Điểm tốt là pagination đã xuất hiện ở nhiều danh sách, search đã có ở ProductService, và một phần business logic tuần 5 đã được khởi động sớm như kiểm tra tồn kho khi thêm order item và tạo invoice từ session.

## Slide 6 - Đánh giá tuần 3
Kết luận cho tuần 3 là nền backend đã đạt mục tiêu. Tuy nhiên, để chuẩn bị cho tuần 5-6, nhóm nên tách CrudService thành các service nhỏ theo module, chuẩn hóa Repository/Service pattern và bổ sung validation cho các DTO tạo/sửa dữ liệu.

## Slide 7 - Đối chiếu tuần 4
Tuần 4 yêu cầu JWT Authentication, login/register, role-based authorization, password hashing và protected APIs. Repo hiện đã triển khai đầy đủ các phần bắt buộc: JwtBearer trong Program.cs, AuthController cho login/register/me/change-password/refresh/logout, BCrypt password hashing và [Authorize] trên các controller. Ngoài ra hệ thống còn có refresh token, đây là phần khuyến khích nhưng đã làm được.

## Slide 8 - Luồng auth
Luồng demo có thể đi theo 5 bước: đăng nhập bằng seed account, hệ thống verify password bằng BCrypt, phát access token và refresh token, dùng Bearer token để gọi protected APIs, sau đó demo refresh token hoặc logout để revoke token. Đây là kịch bản rõ nhất để chứng minh phần tuần 4.

## Slide 9 - Phân quyền
Role-based authorization đã được gắn theo nhóm API. Admin có quyền cao nhất, Manager có quyền quản lý vận hành, Staff và Cashier có quyền với các API vận hành như venue table read, session và order. Điểm quan trọng là hệ thống không chỉ đăng nhập được, mà còn kiểm soát quyền theo nghiệp vụ.

## Slide 10 - Tình trạng thực tế
Về kiểm chứng, em đã chạy `dotnet test pool-hub.sln` và kết quả pass 2/2 tests. README có seed accounts và docs/postman có collection Week4 Auth. Tuy nhiên coverage hiện còn mỏng: chưa có nhiều test cho CRUD, domain flows, refresh token edge cases và role access matrix.

## Slide 11 - Kết luận
Tổng kết lại, tuần 3 đã tạo nền backend với architecture, EF Core, migration, CRUD APIs, Swagger và error handling. Tuần 4 đã tạo lớp bảo mật với JWT, BCrypt, refresh token và role-based authorization. Hệ thống sẵn sàng bước sang tuần 5-6, nhưng cần ưu tiên tách service, bổ sung validation và mở rộng test để backend ổn định hơn khi thêm business logic nâng cao.
