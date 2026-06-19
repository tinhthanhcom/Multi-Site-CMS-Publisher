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
| Phase 4 M4: CMS .NET integration | claude | done | IAIContentService (HttpClient), Prompt Template Manager, nút "AI Viết", token tracking. Build sạch, 63/63 test |
| Phase 4 E2E local (mock) | claude | done | Gateway MockProvider + CMS trỏ localhost:8080; integration test PASS; app boot :5099 OK |
| Phase 4 M5: deploy thật | — | next | Dựng VPS+domain, codex/key thật, đổi PROVIDER_ORDER, E2E publish lên site |

## Recent Context

- 2026-06-19: E2E local (mock) — gateway MockProvider (MOCK_PROVIDER=true, PROVIDER_ORDER=mock) chạy :8080; CMS appsettings.Development.json trỏ AIGateway→localhost:8080; integration test CMS→gateway PASS; app boot :5099 OK (/login=200). Login: admin/Admin@123. Fix null-serialize + zod nullish.
- 2026-06-19: Phase 4 M4 — tích hợp CMS với AI Gateway: IAIContentService (typed HttpClient) + PromptTemplateRenderer + DI (env PUBLISHER_AIGATEWAY_URL/KEY); Prompt Template Manager (Admin) + nút "AI Viết" trong PostEditor + tạo nháp bản dịch. Build 3 project sạch, 63/63 test.
- 2026-06-19: Build AI Gateway (Node/TS/Fastify) M1-M3 phía service: CodexProvider + Claude/Gemini fallback + Orchestrator + /v1/generate. 19/19 test pass, build + smoke test OK.
- 2026-06-19: Push `main` lên origin; nhánh feature đã xóa. Chốt D-006 (AI Gateway wrap codex CLI, fallback Claude/Gemini).

## Next Recommended Moves

1. M5/deploy — dựng VPS + domain cho gateway: `npm i -g @openai/codex`, set `OPENAI_API_KEY` (+ optional `ANTHROPIC_API_KEY`/`GEMINI_API_KEY`), systemd/Docker + nginx TLS; rồi đặt `PUBLISHER_AIGATEWAY_URL/KEY` cho CMS và chạy app E2E (topic → sinh bài → publish)
2. Chạy app .NET thật để verify UI "AI Viết" + Template Manager (cần DB `localhost/01MultiSiteCMS` + gateway URL/key)
3. (gateway) Cân nhắc `--output-schema` cho codex để siết JSON output; thêm retry/backoff khi 429
4. Phase 5: Hangfire jobs (PublishScheduledPostsJob, AIAutoPublishJob dùng gateway, RetryFailedPostsJob)

## Session Start Checklist

- Đọc `AGENTS.md` hoặc `CLAUDE.md`
- Đọc `.context/PROJECT.md`
- Đọc file docs liên quan đến task đang làm
- Nếu làm thay đổi kiến trúc, cập nhật `DECISIONS.md`
- Nếu fix bug/risk, cập nhật `ERRORS.md`
