# AI Gateway

Dịch vụ HTTP sinh nội dung bài viết (kèm dịch đa ngôn ngữ) cho **Multi-Site CMS Publisher**.
Bọc **codex CLI** làm provider chính, **Claude** và **Gemini** làm fallback. Chạy trên VPS riêng
sau Nginx + TLS; CMS gọi vào qua 1 endpoint duy nhất.

## Yêu cầu
- Node.js >= 20
- Để dùng provider **codex**: `npm i -g @openai/codex` rồi đăng nhập (xem dưới).

## Chạy thật với codex CLI
codex 0.141+ xác thực qua `CODEX_HOME/auth.json` (env `OPENAI_API_KEY` đơn lẻ KHÔNG đủ — sẽ 401).
Dùng `CODEX_HOME` riêng để không đụng `~/.codex` cá nhân:

```bash
# 1) đăng nhập bằng API key vào CODEX_HOME riêng (1 lần)
export CODEX_HOME=/đường/dẫn/codex-home   # vd C:/Users/<you>/.codex-aigw
printf '%s' "$OPENAI_API_KEY" | codex login --with-api-key
codex login status            # "Logged in using an API key"
#   (hoặc ChatGPT login: `codex login`, rồi đặt CODEX_ENABLED=true)

# 2) trỏ gateway tới CODEX_HOME đó trong .env: CODEX_HOME=...
```
Lưu ý: gateway truyền prompt cho codex qua **stdin**; trên Windows tự dùng shell để gọi `codex.cmd`.
`CODEX_MODEL` để trống = dùng model mặc định của codex. Nếu codex chậm/timeout, gateway tự fallback
sang Claude → Gemini.

## Chạy local
```bash
cp .env.example .env      # điền AI_GATEWAY_API_KEY + ít nhất 1 provider key
npm install
npm test                  # unit + integration (không gọi provider thật)
npm run dev               # http://localhost:8080
```

## Cấu hình (env)
Xem `.env.example`. Quan trọng:
- `AI_GATEWAY_API_KEY` — secret CMS phải gửi qua `Authorization: Bearer`.
- `PROVIDER_ORDER` — mặc định `codex,claude,gemini` (thứ tự fallback).
- `OPENAI_API_KEY`/`CODEX_API_KEY`, `ANTHROPIC_API_KEY`, `GEMINI_API_KEY` — provider chỉ được
  dùng khi có key tương ứng (provider thiếu key sẽ tự bị bỏ qua trong chuỗi fallback).

## API
### `GET /healthz` (no auth)
`{ "status": "ok" }`

### `GET /v1/providers` (auth) — provider nào đang sẵn sàng
### `POST /v1/generate` (auth)
Request (tối thiểu cần `topic` **hoặc** `userPrompt`):
```json
{
  "contentType": "article",
  "topic": "Lợi ích của cà phê",
  "keywords": ["cà phê", "sức khỏe"],
  "length": 800,
  "tone": "seo-friendly",
  "language": "vi",
  "translateTo": ["en", "ja"]
}
```
Response:
```json
{
  "provider": "codex",
  "model": "gpt-5.4",
  "original":   { "language": "vi", "title": "...", "content": "<html>", "excerpt": "..." },
  "translations": [ { "language": "en", "title": "...", "content": "<html>", "excerpt": "..." } ],
  "usage": { "inputTokens": 0, "outputTokens": 0, "totalTokens": 0 },
  "durationMs": 1234,
  "requestId": "uuid"
}
```
Lỗi: `{ "error": { "code": "...", "message": "...", "provider": "..." } }`
(400 validate, 401 auth, 429 rate-limit, 502/503/504 provider).

## Bảo mật
- Bearer token (so sánh constant-time), rate-limit 60 req/phút.
- Codex chạy `--sandbox read-only --ask-for-approval never` → không shell/file/network, chỉ sinh text.
- Không log key/prompt nhạy cảm. Trên VPS chỉ mở HTTPS, firewall giới hạn IP/VPN của CMS.

## Deploy
Xem `deploy/` (systemd unit + cấu hình Nginx reverse-proxy TLS).
