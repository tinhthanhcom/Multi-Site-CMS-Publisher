// Quill rich-text editor interop for the Post editor.
// Loads Quill (CSS + JS) from CDN on demand, then exposes init / getHtml / setHtml / dispose.
// Used as a JS module imported from PostEditor.razor.

const QUILL_JS = "https://cdn.jsdelivr.net/npm/quill@1.3.7/dist/quill.min.js";
const QUILL_CSS = "https://cdn.jsdelivr.net/npm/quill@1.3.7/dist/quill.snow.css";

const editors = {};
let loadPromise = null;

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const el = document.createElement("script");
        el.src = src;
        el.async = true;
        el.onload = () => resolve();
        el.onerror = () => reject(new Error("Failed to load " + src));
        document.head.appendChild(el);
    });
}

function loadCss(href) {
    return new Promise((resolve) => {
        if (document.querySelector(`link[href="${href}"]`)) {
            resolve();
            return;
        }
        const el = document.createElement("link");
        el.rel = "stylesheet";
        el.href = href;
        el.onload = () => resolve();
        el.onerror = () => resolve(); // CSS failure should not block editing
        document.head.appendChild(el);
    });
}

async function ensureQuillLoaded() {
    if (window.Quill) return;
    if (!loadPromise) {
        loadPromise = (async () => {
            await loadCss(QUILL_CSS);
            await loadScript(QUILL_JS);
        })();
    }
    await loadPromise;
}

const toolbar = [
    [{ header: [1, 2, 3, false] }],
    ["bold", "italic", "underline", "strike"],
    [{ list: "ordered" }, { list: "bullet" }],
    [{ color: [] }, { background: [] }],
    ["blockquote", "code-block"],
    ["link", "image"],
    ["clean"]
];

// Initializes a Quill editor on the element with the given id and seeds it with initialHtml.
export async function init(elementId, initialHtml) {
    await ensureQuillLoaded();
    const el = document.getElementById(elementId);
    if (!el) return false;

    // Dispose any prior instance bound to this element (e.g. on re-render).
    if (editors[elementId]) {
        dispose(elementId);
    }

    const quill = new window.Quill(el, {
        theme: "snow",
        modules: { toolbar }
    });

    if (initialHtml) {
        quill.clipboard.dangerouslyPasteHTML(initialHtml);
    }

    editors[elementId] = quill;
    return true;
}

// Returns the current HTML content of the editor.
export function getHtml(elementId) {
    const quill = editors[elementId];
    if (!quill) return null;
    const html = quill.root.innerHTML;
    // Treat an empty editor as empty string rather than "<p><br></p>".
    return html === "<p><br></p>" ? "" : html;
}

// Replaces the editor content with the given HTML.
export function setHtml(elementId, html) {
    const quill = editors[elementId];
    if (!quill) return;
    if (html) {
        quill.clipboard.dangerouslyPasteHTML(html);
    } else {
        quill.setText("");
    }
}

// Disposes the editor and removes the Quill-injected toolbar/markup.
export function dispose(elementId) {
    const quill = editors[elementId];
    if (!quill) return;
    delete editors[elementId];
    const container = quill.container;
    if (container && container.parentNode) {
        // Remove the toolbar Quill inserts as a sibling before the container.
        const toolbarEl = container.previousElementSibling;
        if (toolbarEl && toolbarEl.classList.contains("ql-toolbar")) {
            toolbarEl.remove();
        }
        container.classList.remove("ql-container", "ql-snow");
        container.innerHTML = "";
    }
}
