# Errors And Risks

## Known Risks

### R-001: Dynamic SQL mapping có rủi ro bảo mật cao
- Source: `docs/system-design.md`, `docs/deployment-plan.md`
- Risk:
  - SQL injection nếu tên bảng/cột không được whitelist chặt
  - publish fail nếu schema site có trigger/constraint đặc biệt
- Guardrail:
  - chỉ chấp nhận identifier theo regex an toàn
  - kiểm tra bảng/cột có thật trong schema
  - values luôn parameterized
  - validate bằng test insert trong transaction rồi rollback

### R-002: Secret management là critical path
- Risk:
  - lộ connection string hoặc mất encryption key sẽ gây sự cố lớn
- Guardrail:
  - dùng env vars cho key và API secret
  - không log secret
  - backup key có kiểm soát

### R-003: Tài liệu và code có thể lệch nhau khi bắt đầu scaffold
- Risk:
  - triển khai thực tế khác tài liệu dẫn tới context cũ sai
- Guardrail:
  - cập nhật `.context/PROJECT.md` ngay khi quyết định kiến trúc được chốt
  - thêm entry vào `HISTORY.md` sau mỗi thay đổi lớn

## Bugs

Chưa có bug runtime nào được ghi nhận trong workspace hiện tại vì chưa có source code thực thi.

## Repeated Anti-Patterns To Avoid

- giả định mọi database site có cùng schema
- nhúng connection string trực tiếp vào config file commit lên repo
- cho phép editor chỉnh raw SQL hoặc identifier tự do
- triển khai publish flow trước khi có mapping validation đầy đủ
