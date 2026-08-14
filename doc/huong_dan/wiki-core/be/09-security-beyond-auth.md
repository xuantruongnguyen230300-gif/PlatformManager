# 9. Bảo mật ngoài phạm vi Auth

Đã bàn Auth/Identity kỹ ở [02-identity-auth.md](02-identity-auth.md) — còn vài điểm khác hay bị bỏ sót khi mới thiết kế:

- **Rate limiting** (giới hạn số request/IP hoặc /user) — chặn brute-force đăng nhập, chặn 1 client gọi API quá tải làm chậm cả hệ thống cho người khác.
- **Quản lý secret** (connection string, API key bên thứ 3) — không commit vào git dạng plaintext; môi trường production nên dùng cơ chế secret manager thật (Azure Key Vault, AWS Secrets Manager, hoặc tối thiểu biến môi trường không nằm trong git).
- **Input validation cho đường raw SQL/Dapper** (nếu dùng cho phần "field mở rộng"/`sysgrid` ở [03-metadata-driven-design.md](03-metadata-driven-design.md)) — EF Core tự parameterize query nên chống SQL injection mặc định; nhưng bất kỳ chỗ nào tự ráp chuỗi SQL tay (kể cả cho tính năng "linh hoạt" như lọc động) đều phải parameterize thủ công, không nối chuỗi trực tiếp giá trị người dùng nhập vào.
