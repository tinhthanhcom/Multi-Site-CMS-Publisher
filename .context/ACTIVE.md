**Name:** Sprint 1 - Phases 1-3 Implementation
**Branch:** `feature/phases-1-3-foundation`
**Started:** 2026-06-18
**Focus:** Đã hoàn thành Phase 1 (Setup & Foundation), Phase 2 (Site & Mapping), Phase 3 (Manual Publishing). Tiếp theo: Phase 4 (AI).
**Status:** Phase 1-3 done & merged vào `main` (fcb2b17); DB = `localhost/01MultiSiteCMS`

## In Progress

| Task | Owner | Status | Notes |
|---|---|---|---|
| Phase 1: Setup & Foundation | claude (Agent A+B) | done | Solution scaffold, AppDB+EF, cookie auth, layout, Users CRUD |
| Phase 2: Site & Field Mapping | claude (Agent C+D) | done | SiteDbConnector, encrypt conn, mapping UI, INSERT preview/validate |
| Phase 3: Manual Publishing | claude (Agent E) | done | PostPublisher, post editor (Quill), publish/schedule/retry |
| Wave 4: E2E verification | claude | done | Publish thật vào LocalDB OK, 56 tests pass, app start OK |
| Phase 4: AI Content Generator | — | next | Claude API streaming, prompt templates, token tracking |

## Recent Context

- 2026-06-19: Rà soát Phase 3 đối chiếu DoD → kết luận hoàn thiện (code + E2E pass); tổng hợp scope Phase 4 (AI streaming, prompt templates, AI trong editor, token). Treo: chưa push origin, chưa xóa nhánh feature.
- 2026-06-18: Trỏ AppDb sang `localhost/01MultiSiteCMS`, áp migration (7 bảng + 2 view + 2 proc), seed OK, app start + /login=200.
- 2026-06-18: Review (build sạch, 56/56 test) rồi merge nhánh Phase 1-3 vào `main` (--no-ff, commit fcb2b17). Chưa push origin.

## Next Recommended Moves

1. (tùy chọn) `git push origin main` + xóa nhánh `feature/phases-1-3-foundation`
2. Phase 4: tích hợp Claude API (streaming) cho AIContentGenerator + Prompt Template Manager + nút "AI Viết" trong post editor
3. Phase 5: Hangfire jobs (PublishScheduledPostsJob đọc các post đang `scheduled`, RetryFailedPostsJob)
4. Cân nhắc `IDbContextFactory<AppDbContext>` nếu gặp lỗi concurrency DbContext trong Blazor Server

## Session Start Checklist

- Đọc `AGENTS.md` hoặc `CLAUDE.md`
- Đọc `.context/PROJECT.md`
- Đọc file docs liên quan đến task đang làm
- Nếu làm thay đổi kiến trúc, cập nhật `DECISIONS.md`
- Nếu fix bug/risk, cập nhật `ERRORS.md`
