**Name:** Sprint 2 - Phase 4 (AI) via AI Gateway
**Branch:** `main`
**Started:** 2026-06-19
**Focus:** Phase 4 — gọi AI qua dịch vụ "AI Gateway" riêng (wrap codex CLI, fallback Claude/Gemini). Service-side M1-M3 done; tiếp theo M4 (tích hợp .NET).
**Status:** Phase 1-3 done & merged `main` (fcb2b17, push e6829c2); AI Gateway M1-M3 done; DB = `localhost/01MultiSiteCMS`

## In Progress

| Task | Owner | Status | Notes |
|---|---|---|---|
| Phase 1-3 | claude | done | Foundation + site/mapping + manual publishing, 56 tests, merged `main` |
| AI Gateway M1-M3 (service) | claude | done | `ai-gateway/` Node/TS/Fastify: codex+claude+gemini, /v1/generate, 19 tests, Docker/systemd/nginx |
| Phase 4 M4: CMS .NET integration | — | next | `IAIContentService` (HttpClient), Prompt Template Manager, nút "AI Viết" trong PostEditor, token tracking |
| Phase 4 M5: E2E | — | todo | topic → gateway sinh bài → editor → publish lên site thật |

## Recent Context

- 2026-06-19: Build AI Gateway (Node/TS/Fastify) M1-M3 phía service: CodexProvider + Claude/Gemini fallback + Orchestrator + /v1/generate. 19/19 test pass, build + smoke test OK. Còn lại: M4 (tích hợp .NET vào CMS) + M5 E2E.
- 2026-06-19: Push `main` lên origin (e6829c2); nhánh feature đã xóa. Chốt D-006 (AI Gateway wrap codex CLI, fallback Claude/Gemini).
- 2026-06-19: Rà soát Phase 3 đối chiếu DoD → kết luận hoàn thiện (code + E2E pass).

## Next Recommended Moves

1. M4 — CMS .NET: `IAIContentService` + `AIGatewayOptions` (env `PUBLISHER_AIGATEWAY_KEY/URL`) + `AddHttpClient`; `PromptTemplateRenderer`; Template Manager (Admin CRUD `AIPromptTemplate`); nút "AI Viết" trong `PostEditor.razor` (đổ vào Quill, set IsAIGenerated/AIPromptUsed/AITokensUsed); audit `AI_CONTENT_GENERATED`
2. M2 còn lại: cung cấp `ANTHROPIC_API_KEY`/`GEMINI_API_KEY` để kiểm thật chuỗi fallback (hiện đã code + unit test, chưa gọi key thật)
3. M3 deploy: dựng VPS + domain, `npm i -g @openai/codex`, set key, systemd/Docker + nginx TLS, smoke test từ CMS
4. Phase 5: Hangfire jobs (PublishScheduledPostsJob, AIAutoPublishJob dùng gateway, RetryFailedPostsJob)

## Session Start Checklist

- Đọc `AGENTS.md` hoặc `CLAUDE.md`
- Đọc `.context/PROJECT.md`
- Đọc file docs liên quan đến task đang làm
- Nếu làm thay đổi kiến trúc, cập nhật `DECISIONS.md`
- Nếu fix bug/risk, cập nhật `ERRORS.md`
