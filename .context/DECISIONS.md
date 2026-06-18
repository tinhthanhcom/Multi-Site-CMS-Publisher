# Decisions

## Active Decisions

### D-001: Shared AI context uses `.context/` as single source of truth
- Date: 2026-06-18
- Status: accepted
- Why:
  - Tránh lặp nội dung giữa `AGENTS.md` và `CLAUDE.md`
  - Giảm drift khi thay đổi kiến trúc, phase hoặc convention
  - Phù hợp với mô hình trong repo `orynvn/Multi-AI-Workspace-Setup-`
- Consequence:
  - Mọi cập nhật trạng thái dự án nên đi vào `.context/*`
  - Entry files cho từng AI chỉ còn vai trò điều hướng

### D-002: Tạm coi dự án đang ở giai đoạn docs-first / pre-scaffold
- Date: 2026-06-18
- Status: accepted
- Why:
  - Workspace hiện chỉ có thư mục `docs`
  - Chưa có `.sln`, source code, hoặc Git history cục bộ
- Consequence:
  - Các AI không nên giả định codebase đã tồn tại
  - Mọi task triển khai tiếp theo cần bám vào tài liệu thiết kế hiện có

## When To Add A New Decision

Thêm decision mới khi có thay đổi ở một trong các nhóm sau:
- thay đổi kiến trúc solution;
- thay đổi auth model;
- thay đổi cách mã hóa secret / connection string;
- thay đổi scheduler / background processing;
- thay đổi AI provider strategy;
- thay đổi database strategy hoặc publish flow.
