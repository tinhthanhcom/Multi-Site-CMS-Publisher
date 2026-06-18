# ADR-001: Shared Context For Multi-AI Collaboration

- Date: 2026-06-18
- Status: Accepted

## Context

Dự án cần để Claude CLI và Codex CLI chia sẻ cùng một hiểu biết về:
- mục tiêu sản phẩm;
- trạng thái hiện tại;
- quyết định kỹ thuật;
- backlog gần hạn;
- rủi ro và bài học.

Nếu mỗi công cụ giữ một file context riêng đầy đủ, nội dung sẽ bị lặp, nhanh drift, và khó bảo trì khi dự án thay đổi.

## Decision

Dùng thư mục `.context/` làm bộ nhớ dùng chung cho dự án, trong đó:
- `.context/PROJECT.md` là nguồn sự thật chính;
- `.context/ACTIVE.md` giữ trạng thái sprint/session hiện tại;
- các file còn lại giữ lịch sử, quyết định, lỗi và chỉ mục file.

`AGENTS.md` và `CLAUDE.md` chỉ đóng vai trò adapter mỏng, điều hướng AI đọc đúng file shared context.

## Consequences

- Một thay đổi về kiến trúc hoặc phase chỉ cần cập nhật ở `.context/*`
- Claude CLI và Codex CLI có thể handoff qua lại ít mất ngữ cảnh hơn
- Khi dự án có Git repo thật, chỉ cần tiếp tục commit các file `.context/*` là đủ để giữ trí nhớ dài hạn
