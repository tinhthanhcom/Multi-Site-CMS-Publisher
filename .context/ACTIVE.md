**Name:** Sprint 2 - Phase 4 (AI Gateway) + Multi-language
**Branch:** `main`
**Started:** 2026-06-19
**Focus:** Phase 4 (AI Gateway wrap codex) DONE & chạy thật. Tiếp: đa ngôn ngữ (D-007) — P1-P4 done, còn P5 E2E.
**Status:** Phase 1-3 + Phase 4 (gateway+codex thật) + đa ngôn ngữ P1-P4 done; DB = `localhost/01MultiSiteCMS` (migration AddMultiLanguage applied)

## In Progress

| Task | Owner | Status | Notes |
|---|---|---|---|
| Phase 1-3 | claude | done | Foundation + site/mapping + manual publishing, 56 tests, merged `main` |
| AI Gateway M1-M3 (service) | claude | done | `ai-gateway/` Node/TS/Fastify: codex+claude+gemini, /v1/generate, 19 tests, Docker/systemd/nginx |
| Phase 4 M4: CMS .NET integration | claude | done | IAIContentService (HttpClient), Prompt Template Manager, nút "AI Viết", token tracking. Build sạch, 63/63 test |
| Phase 4 E2E local (mock) | claude | done | Gateway MockProvider + CMS trỏ localhost:8080; integration test PASS; app boot :5099 OK |
| Phase 4 codex THẬT (local) | claude | done | @openai/codex 0.141 + login CODEX_HOME riêng; codex sinh bài VI SEO + dịch EN chất lượng cao; fallback OK |
| Đa ngôn ngữ P1-P4 (D-007) | claude | done | model+migration, BuildLocalized+publish nhóm, SiteEdit/SiteMapping UI, PostEditor tabs. 70/70 test, app boot OK |
| Đa ngôn ngữ P5: E2E | — | next | Tạo site 2 ngôn ngữ + bảng remote cột *_vi/*_en → posts/new tabs → AI lấp → publish 1 dòng |
| Phase 4 M5: deploy gateway VPS | — | later | Bê CODEX_HOME/login lên VPS+domain, nginx TLS |

## Recent Context

- 2026-06-19: Đa ngôn ngữ (D-007) — P1 model + migration AddMultiLanguage; P4 BuildLocalized + PostPublisher gom nhóm (1 INSERT cột-theo-ngôn-ngữ); P2 SiteEdit chọn nhiều ngôn ngữ + SiteMapping ma trận cột; P3 PostEditor tabs (1 Quill/ngôn ngữ) + lưu nhóm + AI lấp tab. Build sạch, 70/70 test, app :5099 OK. Còn P5 E2E (bảng remote *_vi/*_en).
- 2026-06-19: Chạy THẬT codex CLI 0.141 qua gateway — bài VI SEO chất lượng cao (~22-25s) + dịch EN tốt; fallback codex→claude→gemini OK. Fix codex 0.141: prompt qua stdin, shell trên Windows, bỏ --ask-for-approval, auth qua CODEX_HOME/login. Keys → .env (gitignored). Gemini→2.5-flash, Claude prefill '{'.
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
