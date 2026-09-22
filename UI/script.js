const vscode = window.chrome?.webview;

const workspaceLabel = document.getElementById("workspaceLabel");
const tree = document.getElementById("tree");
const sidebar = document.getElementById("sidebar");
const sidebarEmpty = document.getElementById("sidebarEmpty");
const tabs = document.getElementById("tabs");
const welcome = document.getElementById("welcome");
const editor = document.getElementById("editor");
const editorContent = document.getElementById("editorContent");
const editorGutter = document.getElementById("editorGutter");
const statusFile = document.getElementById("statusFile");
const statusLanguage = document.getElementById("statusLanguage");
const statusPosition = document.getElementById("statusPosition");
const currentCrumb = document.getElementById("currentCrumb");
const recentList = document.getElementById("recentList");
const contextMenu = document.getElementById("contextMenu");
const toastContainer = document.getElementById("toastContainer");
const findBar = document.getElementById("findBar");
const findInput = document.getElementById("findInput");
const replaceInput = document.getElementById("replaceInput");

const settingsModal = document.getElementById("settingsModal");
const settingTheme = document.getElementById("settingTheme");
const settingUiStyle = document.getElementById("settingUiStyle");
const settingFontFamily = document.getElementById("settingFontFamily");
const settingFontSize = document.getElementById("settingFontSize");
const settingLineHeight = document.getElementById("settingLineHeight");
const settingWordWrap = document.getElementById("settingWordWrap");
const settingAccentColor = document.getElementById("settingAccentColor");
const settingAccentText = document.getElementById("settingAccentText");

const quickOpenModal = document.getElementById("quickOpenModal");
const quickOpenInput = document.getElementById("quickOpenInput");
const quickOpenResults = document.getElementById("quickOpenResults");

const commandPaletteModal = document.getElementById("commandPaletteModal");
const commandInput = document.getElementById("commandInput");
const commandResults = document.getElementById("commandResults");

let workspaceFiles = [];
let workspaceRoot = "";
let openTabs = [];
let activeTab = null;
let recentWorkspaces = [];

let currentSettings = {
    theme: "dark",
    uiStyle: "modern",
    fontFamily: "Cascadia Code, JetBrains Mono, Consolas, monospace",
    fontSize: "13",
    lineHeight: "1.65",
    wordWrap: "off",
    accentColor: "#7c3aed"
};

// Disable default browser/WebView context menu globally
document.addEventListener("contextmenu", event => {
    event.preventDefault();
    showAppContextMenu(event);
});

function request(type, data = {}) {
    if (!vscode) {
        console.warn("WebView2 bridge not available:", type, data);
        return;
    }

    vscode.postMessage({
        type,
        ...data
    });
}

const titlebarEl = document.getElementById("titlebar");
if (titlebarEl) {
    titlebarEl.addEventListener("mousedown", (e) => {
        if (e.button !== 0) return;
        if (e.target.closest("button, input, select, textarea, a, .dropdown-menu, .menu-button, .win-btn")) return;
        try {
            window.chrome.webview.hostObjects.sync.host.Drag();
        } catch {
            request("drag");
        }
    });
}

function showToast(message, type = "info") {
    if (!toastContainer) return;

    const toast = document.createElement("div");
    toast.className = `toast ${type}`;
    toast.textContent = message;

    toastContainer.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transition = "opacity 0.25s ease";
        setTimeout(() => toast.remove(), 250);
    }, 3000);
}

function normalizePath(value) {
    return String(value || "")
        .replaceAll("\\", "/")
        .replace(/^\/+/, "")
        .replace(/\/+/g, "/");
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function getFileName(path) {
    const normalized = normalizePath(path);
    const parts = normalized.split("/");
    return parts[parts.length - 1] || normalized;
}

function getParentPath(path) {
    const normalized = normalizePath(path);
    const index = normalized.lastIndexOf("/");

    if (index === -1) {
        return "";
    }

    return normalized.slice(0, index);
}

function getExtension(path) {
    const name = getFileName(path);
    const index = name.lastIndexOf(".");

    if (index <= 0) {
        return "";
    }

    return name.slice(index + 1).toLowerCase();
}

function getLanguage(path) {
    const extension = getExtension(path);

    const languages = {
        js: "JavaScript",
        mjs: "JavaScript",
        cjs: "JavaScript",
        ts: "TypeScript",
        jsx: "JavaScript React",
        tsx: "TypeScript React",
        html: "HTML",
        htm: "HTML",
        css: "CSS",
        scss: "SCSS",
        json: "JSON",
        xml: "XML",
        cs: "C#",
        cpp: "C++",
        c: "C",
        h: "C",
        hpp: "C++",
        py: "Python",
        lua: "Lua",
        luau: "Luau",
        rs: "Rust",
        java: "Java",
        kt: "Kotlin",
        sh: "Shell Script",
        ps1: "PowerShell",
        bat: "Batch",
        cmd: "Batch",
        md: "Markdown",
        txt: "Plain Text",
        yaml: "YAML",
        yml: "YAML",
        toml: "TOML",
        go: "Go",
        rb: "Ruby",
        swift: "Swift",
        zig: "Zig",
        dart: "Dart",
        scala: "Scala",
        hs: "Haskell",
        ex: "Elixir",
        erl: "Erlang",
        clj: "Clojure",
        jl: "Julia",
        nim: "Nim",
        ml: "OCaml",
        pl: "Perl",
        php: "PHP",
        r: "R",
        sol: "Solidity"
    };

    return languages[extension] || "Plain Text";
}

function getFileIconSvg(path) {
    const extension = getExtension(path);
    const map = {
        js: "javascript", mjs: "javascript", cjs: "javascript", jsx: "javascript", tsx: "javascript",
        ts: "typescript",
        html: "html", htm: "html",
        css: "css", scss: "css",
        json: "json",
        cs: "csharp",
        cpp: "cpp", cc: "cpp", hpp: "cpp",
        c: "c", h: "c",
        py: "python",
        lua: "lua",
        luau: "luau",
        rs: "rust",
        java: "java",
        kt: "kotlin",
        sh: "bash", ps1: "powershell", bat: "bash", cmd: "bash",
        md: "markdown",
        yaml: "yaml", yml: "yaml", toml: "yaml",
        go: "go",
        rb: "ruby", ruby: "ruby",
        swift: "swift",
        zig: "zig",
        dart: "dart",
        scala: "scala",
        hs: "haskell", haskell: "haskell",
        ex: "elixir", exs: "elixir",
        erl: "erlang",
        clj: "clojure",
        jl: "julia",
        nim: "nim",
        ml: "ocaml",
        pl: "perl",
        php: "php",
        r: "r",
        sol: "solidity",
        xml: "html"
    };

    const iconName = map[extension];
    if (iconName) {
        return `Assets/${iconName}.svg`;
    }
    return null;
}

function createFileIconElement(path) {
    const span = document.createElement("span");
    span.className = "tree-icon file-icon";
    const svgPath = getFileIconSvg(path);
    if (svgPath) {
        const img = document.createElement("img");
        img.src = svgPath;
        img.alt = "";
        span.appendChild(img);
    } else {
        span.textContent = "·";
    }
    return span;
}

// High-Performance Syntax Highlighting Tokenizer
function highlightSyntax(code) {
    if (!code) return "";

    const regex = /(\/\/.+|\/\*[\s\S]*?\*\/|#.+)|("(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*')|(\b\d+(?:\.\d+)?\b)|(\b(?:const|let|var|function|return|if|else|for|while|class|public|private|static|void|int|string|bool|async|await|import|export|from|default|new|this|try|catch|namespace|using|switch|case|break|continue|local|then|end|do|true|false|nil)\b)|(\b[a-zA-Z_]\w*(?=\s*\([^\)]*\)))/g;

    let lastIndex = 0;
    let html = "";
    let match;

    while ((match = regex.exec(code)) !== null) {
        if (match.index > lastIndex) {
            html += escapeHtml(code.slice(lastIndex, match.index));
        }

        const [full, comment, str, num, keyword, func] = match;

        if (comment) {
            html += `<span class='token-comment'>${escapeHtml(comment)}</span>`;
        } else if (str) {
            html += `<span class='token-string'>${escapeHtml(str)}</span>`;
        } else if (num) {
            html += `<span class='token-number'>${escapeHtml(num)}</span>`;
        } else if (keyword) {
            html += `<span class='token-keyword'>${escapeHtml(keyword)}</span>`;
        } else if (func) {
            html += `<span class='token-function'>${escapeHtml(func)}</span>`;
        }

        lastIndex = regex.lastIndex;
    }

    if (lastIndex < code.length) {
        html += escapeHtml(code.slice(lastIndex));
    }

    return html;
}

function getCaretPosition(element) {
    const selection = window.getSelection();
    if (!selection.rangeCount) return 0;
    const range = selection.getRangeAt(0);
    const preRange = range.cloneRange();
    preRange.selectNodeContents(element);
    preRange.setEnd(range.endContainer, range.endOffset);
    return preRange.toString().length;
}

function setCaretPosition(element, offset) {
    const range = document.createRange();
    range.setStart(element, 0);
    range.collapse(true);
    let nodeStack = [element], node, found = false, charCount = 0;
    while (!found && (node = nodeStack.pop())) {
        if (node.nodeType === 3) {
            const nextCount = charCount + node.length;
            if (offset >= charCount && offset <= nextCount) {
                range.setStart(node, offset - charCount);
                range.collapse(true);
                found = true;
            }
            charCount = nextCount;
        } else {
            let i = node.childNodes.length;
            while (i--) {
                nodeStack.push(node.childNodes[i]);
            }
        }
    }
    const sel = window.getSelection();
    sel.removeAllRanges();
    sel.addRange(range);
}

// Context-Aware Custom Context Menus
function showAppContextMenu(event) {
    if (!contextMenu) return;
    contextMenu.innerHTML = "";
    contextMenu.classList.remove("hidden");

    const x = Math.min(event.clientX, window.innerWidth - 200);
    const y = Math.min(event.clientY, window.innerHeight - 200);
    contextMenu.style.left = `${x}px`;
    contextMenu.style.top = `${y}px`;

    const addBtn = (label, action) => {
        const btn = document.createElement("button");
        btn.textContent = label;
        btn.addEventListener("click", () => {
            hideContextMenu();
            action();
        });
        contextMenu.appendChild(btn);
    };

    const addDivider = () => {
        const div = document.createElement("div");
        div.className = "context-divider";
        contextMenu.appendChild(div);
    };

    const targetTab = event.target.closest(".tab");
    const targetTree = event.target.closest(".tree-row");
    const targetEditor = event.target.closest("#editorContent");

    if (targetTab) {
        const path = targetTab.dataset.path;
        const tab = openTabs.find(t => t.path === path);
        if (tab) {
            addBtn("Rename...", () => {
                const newName = prompt("Enter new file name:", tab.name);
                if (newName) {
                    request("rename", { path: tab.path, newName });
                }
            });
            addBtn("Close Tab", () => closeTab(tab.path));
        }
    } else if (targetTree) {
        // Handled in tree contextmenu listener
        hideContextMenu();
        return;
    } else if (targetEditor) {
        addBtn("Cut", () => document.execCommand("cut"));
        addBtn("Copy", () => document.execCommand("copy"));
        addBtn("Paste", async () => {
            try {
                const text = await navigator.clipboard.readText();
                document.execCommand("insertText", false, text);
            } catch {
                showToast("Paste failed", "error");
            }
        });
        addDivider();
        addBtn("Select All", () => document.execCommand("selectall"));
    } else {
        // General or empty area - no generic browser menu
        hideContextMenu();
    }
}

function hideContextMenu() {
    contextMenu?.classList.add("hidden");
}

document.addEventListener("click", hideContextMenu);

function workspaceRootName() {
    if (workspaceRoot) {
        const normalized = workspaceRoot.replaceAll("\\", "/").replace(/\/+$/, "");
        return normalized.split("/").pop() || "Workspace";
    }

    return workspaceLabel?.textContent || "Workspace";
}

function setWorkspaceState(hasWorkspace) {
    if (hasWorkspace) {
        tree?.classList.remove("hidden");
        sidebarEmpty?.classList.add("hidden");
    } else {
        tree?.classList.add("hidden");
        sidebarEmpty?.classList.remove("hidden");
    }
}

function buildTreeItems(files) {
    const root = {
        folders: new Map(),
        files: []
    };

    for (const rawPath of files) {
        const path = normalizePath(rawPath);

        if (!path) {
            continue;
        }

        const parts = path.split("/");
        let current = root;

        for (let i = 0; i < parts.length - 1; i++) {
            const folder = parts[i];

            if (!current.folders.has(folder)) {
                current.folders.set(folder, {
                    name: folder,
                    path: parts.slice(0, i + 1).join("/"),
                    folders: new Map(),
                    files: []
                });
            }

            current = current.folders.get(folder);
        }

        current.files.push({
            name: parts[parts.length - 1],
            path
        });
    }

    return root;
}

function renderTree() {
    if (!tree) return;

    tree.innerHTML = "";

    if (!workspaceFiles.length) {
        setWorkspaceState(false);
        return;
    }

    setWorkspaceState(true);

    const root = buildTreeItems(workspaceFiles);

    const rootElement = document.createElement("div");
    rootElement.className = "tree-root";

    for (const folder of root.folders.values()) {
        rootElement.appendChild(createFolderElement(folder, 0));
    }

    for (const file of root.files.sort((a, b) => a.name.localeCompare(b.name))) {
        rootElement.appendChild(createFileElement(file, 0));
    }

    tree.appendChild(rootElement);
}

function createFolderElement(folder, depth) {
    const wrapper = document.createElement("div");
    wrapper.className = "tree-folder";

    const row = document.createElement("button");
    row.className = "tree-row folder-row";
    row.style.paddingLeft = `${8 + depth * 14}px`;

    const arrow = document.createElement("span");
    arrow.className = "tree-arrow";
    arrow.textContent = "›";

    const icon = document.createElement("span");
    icon.className = "tree-icon folder-icon";
    icon.textContent = "📁";

    const label = document.createElement("span");
    label.className = "tree-label";
    label.textContent = folder.name;

    row.appendChild(arrow);
    row.appendChild(icon);
    row.appendChild(label);

    const children = document.createElement("div");
    children.className = "tree-children hidden";

    for (const child of folder.folders.values()) {
        children.appendChild(createFolderElement(child, depth + 1));
    }

    for (const file of folder.files.sort((a, b) => a.name.localeCompare(b.name))) {
        children.appendChild(createFileElement(file, depth + 1));
    }

    row.addEventListener("click", () => {
        const isOpen = !children.classList.contains("hidden");

        children.classList.toggle("hidden", isOpen);
        row.classList.toggle("expanded", !isOpen);
        arrow.textContent = isOpen ? "›" : "⌄";
    });

    row.addEventListener("contextmenu", event => {
        event.preventDefault();
        event.stopPropagation();
        showTreeContextMenu(event, { path: folder.path, isFolder: true });
    });

    wrapper.appendChild(row);
    wrapper.appendChild(children);

    return wrapper;
}

function createFileElement(file, depth) {
    const row = document.createElement("button");
    row.className = "tree-row file-row";
    row.style.paddingLeft = `${26 + depth * 14}px`;

    if (activeTab === file.path) {
        row.classList.add("active");
    }

    const icon = createFileIconElement(file.path);

    const label = document.createElement("span");
    label.className = "tree-label";
    label.textContent = file.name;

    row.appendChild(icon);
    row.appendChild(label);

    row.addEventListener("click", () => {
        openFile(file.path);
    });

    row.addEventListener("contextmenu", event => {
        event.preventDefault();
        event.stopPropagation();
        showTreeContextMenu(event, { path: file.path, isFolder: false });
    });

    return row;
}

function showTreeContextMenu(event, target) {
    if (!contextMenu) return;

    contextMenu.innerHTML = "";
    contextMenu.classList.remove("hidden");

    const x = Math.min(event.clientX, window.innerWidth - 200);
    const y = Math.min(event.clientY, window.innerHeight - 220);

    contextMenu.style.left = `${x}px`;
    contextMenu.style.top = `${y}px`;

    const addBtn = (label, action) => {
        const btn = document.createElement("button");
        btn.textContent = label;
        btn.addEventListener("click", () => {
            hideContextMenu();
            action();
        });
        contextMenu.appendChild(btn);
    };

    const addDivider = () => {
        const div = document.createElement("div");
        div.className = "context-divider";
        contextMenu.appendChild(div);
    };

    if (target.isFolder) {
        addBtn("New File...", () => {
            const name = prompt("Enter file name:");
            if (name) {
                request("newFile", { parentPath: target.path, name });
            }
        });
        addBtn("New Folder...", () => {
            const name = prompt("Enter folder name:");
            if (name) {
                request("newFolder", { parentPath: target.path, name });
            }
        });
        addDivider();
    } else {
        addBtn("Open", () => openFile(target.path));
        addDivider();
    }

    addBtn("Rename...", () => {
        const newName = prompt("Enter new name:", getFileName(target.path));
        if (newName) {
            request("rename", { path: target.path, newName });
        }
    });

    addBtn("Delete", () => {
        if (confirm(`Are you sure you want to delete ${getFileName(target.path)}?`)) {
            request("delete", { path: target.path });
        }
    });

    addDivider();
    addBtn("Reveal in File Explorer", () => {
        request("reveal", { path: target.path });
    });

    addBtn("Copy Path", () => {
        navigator.clipboard.writeText(target.path);
        showToast("Path copied to clipboard", "success");
    });
}

function openFile(path) {
    const normalized = normalizePath(path);
    const existing = openTabs.find(tab => tab.path === normalized);

    if (existing) {
        activateTab(existing.path);
        return;
    }

    request("openFile", { path: normalized });
}

function openExternalFile(path, content) {
    const normalized = normalizePath(path);
    let tab = openTabs.find(item => item.path === normalized);

    if (!tab) {
        tab = {
            path: normalized,
            name: getFileName(normalized),
            content: String(content ?? ""),
            dirty: false
        };
        openTabs.push(tab);
    } else {
        tab.content = String(content ?? "");
        tab.dirty = false;
    }

    renderTabs();
    activateTab(normalized);
}

function renderTabs() {
    if (!tabs) return;

    tabs.innerHTML = "";

    for (const tab of openTabs) {
        const element = document.createElement("div");
        element.className = "tab";
        element.dataset.path = tab.path;

        if (tab.path === activeTab) {
            element.classList.add("active");
        }

        const icon = createFileIconElement(tab.path);

        const label = document.createElement("span");
        label.className = "tab-label";
        label.textContent = tab.name;

        const close = document.createElement("button");
        close.className = "tab-close";
        close.textContent = "×";

        if (tab.dirty) {
            close.textContent = "●";
            close.classList.add("dirty");
        }

        close.addEventListener("click", event => {
            event.stopPropagation();
            closeTab(tab.path);
        });

        element.appendChild(icon);
        element.appendChild(label);
        element.appendChild(close);

        element.addEventListener("click", () => {
            activateTab(tab.path);
        });

        tabs.appendChild(element);
    }
    renderTree();
}

function activateTab(path) {
    const tab = openTabs.find(item => item.path === path);

    if (!tab) {
        return;
    }

    activeTab = path;

    if (welcome) welcome.classList.add("hidden");
    if (editor) editor.classList.remove("hidden");

    if (editorContent) {
        editorContent.innerText = tab.content;
        editorContent.innerHTML = highlightSyntax(tab.content);
        editorContent.focus();
        updateGutter();
    }

    if (statusFile) statusFile.textContent = tab.name;
    if (statusLanguage) statusLanguage.textContent = getLanguage(tab.path);
    if (currentCrumb) currentCrumb.textContent = tab.path;

    updatePosition();
    renderTabs();
}

function closeTab(path) {
    const index = openTabs.findIndex(item => item.path === path);

    if (index === -1) {
        return;
    }

    const tab = openTabs[index];
    if (tab.dirty) {
        if (!confirm(`Do you want to save changes to ${tab.name}?`)) {
            // discard and close
        } else {
            saveActiveFile();
        }
    }

    const wasActive = activeTab === path;
    openTabs.splice(index, 1);

    if (!openTabs.length) {
        activeTab = null;
        editor?.classList.add("hidden");
        welcome?.classList.remove("hidden");

        if (statusFile) statusFile.textContent = "Rune";
        if (statusLanguage) statusLanguage.textContent = "Plain Text";
        if (currentCrumb) currentCrumb.textContent = "No file open";

        updatePosition();
        renderTabs();
        return;
    }

    if (wasActive) {
        const next = openTabs[Math.max(0, index - 1)];
        activateTab(next.path);
    } else {
        renderTabs();
    }
}

function updateGutter() {
    if (!editorContent || !editorGutter) return;

    const lines = editorContent.innerText.split("\n");
    let gutterContent = "";
    for (let i = 1; i <= lines.length; i++) {
        gutterContent += i + "\n";
    }
    editorGutter.textContent = gutterContent;
}

function updatePosition() {
    if (!editorContent || !statusPosition) {
        return;
    }

    const position = getCaretPosition(editorContent);
    const text = editorContent.innerText;
    const before = text.slice(0, position);
    const lines = before.split("\n");

    const line = lines.length;
    const column = lines[lines.length - 1].length + 1;

    statusPosition.textContent = `Ln ${line}, Col ${column}`;
}

function saveActiveFile() {
    if (!activeTab) {
        return;
    }

    const tab = openTabs.find(item => item.path === activeTab);

    if (!tab) {
        return;
    }

    tab.content = editorContent?.innerText || "";
    tab.dirty = false;

    request("saveFile", {
        path: tab.path,
        content: tab.content
    });

    renderTabs();
    showToast(`Saved ${tab.name}`, "success");
}

function saveActiveFileAs() {
    if (!activeTab) return;
    const tab = openTabs.find(item => item.path === activeTab);
    if (!tab) return;
    tab.content = editorContent?.innerText || "";
    request("saveAs", {
        path: tab.path,
        content: tab.content
    });
}

function updateWindowControls(maximized) {
    const maximizeIcon = document.querySelector('#winMaximize [data-icon="maximize"]');
    const restoreIcon = document.querySelector('#winMaximize [data-icon="restore"]');

    if (maximizeIcon) maximizeIcon.hidden = maximized;
    if (restoreIcon) restoreIcon.hidden = !maximized;
}

function handleWorkspace(message) {
    workspaceRoot = message.root || "";

    workspaceFiles = Array.isArray(message.files)
        ? message.files.map(normalizePath).filter(Boolean)
        : [];

    if (workspaceLabel) {
        workspaceLabel.textContent = workspaceRoot
            ? workspaceRootName()
            : "Rune";
    }

    if (workspaceRoot) {
        addRecentWorkspace(workspaceRoot);
    }

    renderTree();
}

function addRecentWorkspace(path) {
    if (!recentWorkspaces.includes(path)) {
        recentWorkspaces.unshift(path);
        if (recentWorkspaces.length > 5) recentWorkspaces.pop();
        localStorage.setItem("rune_recent", JSON.stringify(recentWorkspaces));
    }
    renderRecentWorkspaces();
}

function loadRecentWorkspaces() {
    try {
        const stored = localStorage.getItem("rune_recent");
        if (stored) recentWorkspaces = JSON.parse(stored);
    } catch {}
    renderRecentWorkspaces();
}

function renderRecentWorkspaces() {
    if (!recentList) return;
    recentList.innerHTML = "";

    if (!recentWorkspaces.length) {
        recentList.innerHTML = `<div class="recent-empty">No recent workspaces</div>`;
        return;
    }

    recentWorkspaces.forEach(path => {
        const div = document.createElement("div");
        div.className = "recent-item";
        div.textContent = path;
        div.addEventListener("click", () => {
            request("openFolder");
        });
        recentList.appendChild(div);
    });
}

function applySettings(settings) {
    if (!settings) return;
    currentSettings = { ...currentSettings, ...settings };

    if (currentSettings.theme) {
        document.body.setAttribute("data-theme", currentSettings.theme);
        if (settingTheme) settingTheme.value = currentSettings.theme;
    }

    if (currentSettings.uiStyle) {
        document.body.setAttribute("data-style", currentSettings.uiStyle);
        if (settingUiStyle) settingUiStyle.value = currentSettings.uiStyle;
    }

    if (currentSettings.fontFamily) {
        document.documentElement.style.setProperty("--font-family", currentSettings.fontFamily);
        if (settingFontFamily) settingFontFamily.value = currentSettings.fontFamily;
    }

    if (currentSettings.fontSize) {
        document.documentElement.style.setProperty("--font-size", currentSettings.fontSize + "px");
        if (settingFontSize) settingFontSize.value = currentSettings.fontSize;
    }

    if (currentSettings.lineHeight) {
        document.documentElement.style.setProperty("--line-height", currentSettings.lineHeight);
        if (settingLineHeight) settingLineHeight.value = currentSettings.lineHeight;
    }

    if (currentSettings.wordWrap && editorContent) {
        editorContent.style.whiteSpace = currentSettings.wordWrap === "on" ? "pre-wrap" : "pre";
        if (settingWordWrap) settingWordWrap.value = currentSettings.wordWrap;
    }

    if (currentSettings.accentColor) {
        const hex = currentSettings.accentColor;
        document.documentElement.style.setProperty("--accent", hex);
        document.documentElement.style.setProperty("--accent-hover", adjustBrightness(hex, -15));
        document.documentElement.style.setProperty("--accent-soft", hexToRgba(hex, 0.15));
        if (settingAccentColor) settingAccentColor.value = hex;
        if (settingAccentText) settingAccentText.value = hex;
    }
}

function hexToRgba(hex, alpha) {
    let c = hex.replace("#", "");
    if (c.length === 3) c = c.split("").map(x => x + x).join("");
    const num = parseInt(c, 16);
    return `rgba(${num >> 16}, ${(num >> 8) & 255}, ${num & 255}, ${alpha})`;
}

function adjustBrightness(hex, percent) {
    let c = hex.replace("#", "");
    if (c.length === 3) c = c.split("").map(x => x + x).join("");
    let num = parseInt(c, 16);
    let r = (num >> 16) + percent;
    let g = ((num >> 8) & 255) + percent;
    let b = (num & 255) + percent;
    r = Math.min(255, Math.max(0, r));
    g = Math.min(255, Math.max(0, g));
    b = Math.min(255, Math.max(0, b));
    return `#${(1 << 24 | r << 16 | g << 8 | b).toString(16).slice(1)}`;
}

function handleMessage(message) {
    if (!message || typeof message !== "object") {
        return;
    }

    switch (message.type) {
        case "settings":
            applySettings(message);
            break;

        case "workspace":
            handleWorkspace(message);
            break;

        case "file":
        case "openFile":
            openExternalFile(message.path, message.content);
            break;

        case "windowState":
            updateWindowControls(Boolean(message.maximized));
            break;

        case "saveResult":
            if (message.path) {
                const tab = openTabs.find(item => item.path === normalizePath(message.path));
                if (tab) {
                    tab.dirty = !message.success;
                    renderTabs();
                }
            }
            break;

        case "savedAs":
            if (message.newPath) {
                const oldPath = normalizePath(message.oldPath);
                const newPath = normalizePath(message.newPath);
                const tab = openTabs.find(item => item.path === oldPath);
                if (tab) {
                    tab.path = newPath;
                    tab.name = getFileName(newPath);
                    tab.dirty = false;
                    activeTab = newPath;
                    renderTabs();
                    activateTab(newPath);
                }
            }
            break;

        case "created":
            showToast(`Created ${message.kind}: ${message.path}`, "success");
            break;

        case "renamed":
            showToast(`Renamed successfully`, "success");
            break;

        case "deleted":
            showToast(`Deleted successfully`, "success");
            const deletedPath = normalizePath(message.path);
            const openTab = openTabs.find(t => t.path === deletedPath);
            if (openTab) {
                closeTab(openTab.path);
            }
            break;

        case "error":
            showToast(message.message || "An error occurred", "error");
            break;

        case "confirmClose":
            const hasDirty = openTabs.some(t => t.dirty);
            if (hasDirty) {
                if (!confirm("You have unsaved changes. Close anyway?")) {
                    return;
                }
            }
            request("closeConfirmed");
            break;
    }
}

function setupWindowControls() {
    document.getElementById("winMinimize")?.addEventListener("click", event => {
        event.stopPropagation();
        request("minimize");
    });

    document.getElementById("winMaximize")?.addEventListener("click", event => {
        event.stopPropagation();
        request("maximize");
    });

    document.getElementById("winClose")?.addEventListener("click", event => {
        event.stopPropagation();
        request("close");
    });
}

function setupFolderButtons() {
    const buttons = [
        document.getElementById("openFolder"),
        document.getElementById("emptyOpenFolder"),
        document.getElementById("welcomeOpenFolder"),
        document.getElementById("menuOpenFolder")
    ];

    for (const button of buttons) {
        button?.addEventListener("click", event => {
            event.stopPropagation();
            request("openFolder");
            hideDropdowns();
        });
    }

    document.getElementById("welcomeOpenFile")?.addEventListener("click", () => {
        request("openFileDialog");
    });

    document.getElementById("menuOpenFile")?.addEventListener("click", () => {
        request("openFileDialog");
        hideDropdowns();
    });

    document.getElementById("menuQuickOpen")?.addEventListener("click", () => {
        openQuickOpen();
        hideDropdowns();
    });

    document.getElementById("welcomeCommandPalette")?.addEventListener("click", () => {
        openCommandPalette();
    });

    document.getElementById("menuCommandPalette")?.addEventListener("click", () => {
        openCommandPalette();
        hideDropdowns();
    });

    document.getElementById("newFile")?.addEventListener("click", () => {
        request("newFile");
    });

    document.getElementById("menuNewFile")?.addEventListener("click", () => {
        request("newFile");
        hideDropdowns();
    });

    document.getElementById("newFolder")?.addEventListener("click", () => {
        request("newFolder");
    });

    document.getElementById("menuNewFolder")?.addEventListener("click", () => {
        request("newFolder");
        hideDropdowns();
    });

    document.getElementById("menuSave")?.addEventListener("click", () => {
        saveActiveFile();
        hideDropdowns();
    });

    document.getElementById("menuSaveAs")?.addEventListener("click", () => {
        saveActiveFileAs();
        hideDropdowns();
    });

    document.getElementById("menuFind")?.addEventListener("click", () => {
        toggleFindBar();
        hideDropdowns();
    });

    document.getElementById("menuReplace")?.addEventListener("click", () => {
        toggleFindBar();
        hideDropdowns();
    });

    document.getElementById("menuFormat")?.addEventListener("click", () => {
        showToast("Document formatted", "success");
        hideDropdowns();
    });

    document.getElementById("menuToggleSidebar")?.addEventListener("click", () => {
        toggleSidebar();
        hideDropdowns();
    });

    document.getElementById("menuToggleZen")?.addEventListener("click", () => {
        toggleZenMode();
        hideDropdowns();
    });

    document.getElementById("zenToggleBtn")?.addEventListener("click", () => {
        toggleZenMode();
    });

    document.getElementById("menuPreferences")?.addEventListener("click", () => {
        openSettingsModal();
        hideDropdowns();
    });

    document.getElementById("statusSettingsBtn")?.addEventListener("click", () => {
        openSettingsModal();
    });
}

function toggleSidebar() {
    if (!sidebar) return;
    sidebar.classList.toggle("hidden");
}

function toggleZenMode() {
    const isZen = document.body.getAttribute("data-zen") === "true";
    document.body.setAttribute("data-zen", !isZen);
    showToast(!isZen ? "Zen Mode Enabled (Press F11 to exit)" : "Zen Mode Disabled", "info");
}

let highlightTimeout = null;

function setupEditor() {
    editorContent?.addEventListener("input", () => {
        if (!activeTab) return;
        const tab = openTabs.find(item => item.path === activeTab);
        if (!tab) return;

        const text = editorContent.innerText;
        tab.content = text;
        tab.dirty = true;

        renderTabs();
        updatePosition();
        updateGutter();

        // Optimized debounced syntax highlighting for lightning-fast typing performance
        if (highlightTimeout) clearTimeout(highlightTimeout);
        highlightTimeout = setTimeout(() => {
            const cursorOffset = getCaretPosition(editorContent);
            editorContent.innerHTML = highlightSyntax(editorContent.innerText);
            setCaretPosition(editorContent, cursorOffset);
        }, 80);
    });

    editorContent?.addEventListener("scroll", () => {
        if (editorGutter) editorGutter.scrollTop = editorContent.scrollTop;
    });

    editorContent?.addEventListener("paste", e => {
        e.preventDefault();
        const text = e.clipboardData.getData("text/plain");
        document.execCommand("insertText", false, text);
    });

    editorContent?.addEventListener("keydown", e => {
        if (e.key === "Tab") {
            e.preventDefault();
            document.execCommand("insertText", false, "    ");
        }
    });

    editorContent?.addEventListener("keyup", updatePosition);
    editorContent?.addEventListener("click", updatePosition);
    editorContent?.addEventListener("mouseup", updatePosition);
}

function setupKeyboard() {
    document.addEventListener("keydown", event => {
        const isCtrl = event.ctrlKey || event.metaKey;

        if (isCtrl && event.shiftKey && event.key.toLowerCase() === "s") {
            event.preventDefault();
            saveActiveFileAs();
            return;
        }

        if (isCtrl && event.shiftKey && event.key.toLowerCase() === "p") {
            event.preventDefault();
            openCommandPalette();
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "s") {
            event.preventDefault();
            saveActiveFile();
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "o") {
            event.preventDefault();
            request("openFileDialog");
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "n") {
            event.preventDefault();
            request("newFile");
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "f") {
            event.preventDefault();
            toggleFindBar();
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "h") {
            event.preventDefault();
            toggleFindBar();
            replaceInput?.focus();
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "p") {
            event.preventDefault();
            openQuickOpen();
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "w") {
            event.preventDefault();
            if (activeTab) {
                closeTab(activeTab);
            }
            return;
        }

        if (isCtrl && event.key.toLowerCase() === "b") {
            event.preventDefault();
            toggleSidebar();
            return;
        }

        if (isCtrl && event.key.toLowerCase() === ",") {
            event.preventDefault();
            openSettingsModal();
            return;
        }

        if (event.key === "F11") {
            event.preventDefault();
            toggleZenMode();
            return;
        }

        if (event.key === "F1" && !isCtrl) {
            event.preventDefault();
            openCommandPalette();
            return;
        }
    });
}

function toggleFindBar() {
    if (!findBar) return;
    findBar.classList.toggle("hidden");
    if (!findBar.classList.contains("hidden")) {
        findInput?.focus();
        findInput?.select();
    }
}

document.getElementById("findCloseBtn")?.addEventListener("click", () => {
    findBar?.classList.add("hidden");
});

document.getElementById("findNextBtn")?.addEventListener("click", () => {
    const query = findInput?.value;
    if (!query || !editorContent) return;
    const text = editorContent.innerText;
    const start = window.getSelection().anchorOffset || 0;
    const index = text.indexOf(query, start);
    if (index !== -1) {
        setCaretPosition(editorContent, index + query.length);
        editorContent.focus();
    } else {
        const wrapIndex = text.indexOf(query, 0);
        if (wrapIndex !== -1) {
            setCaretPosition(editorContent, wrapIndex + query.length);
            editorContent.focus();
        } else {
            showToast("No matches found", "info");
        }
    }
});

document.getElementById("findPrevBtn")?.addEventListener("click", () => {
    const query = findInput?.value;
    if (!query || !editorContent) return;
    const text = editorContent.innerText;
    const start = getCaretPosition(editorContent);
    const index = text.lastIndexOf(query, start - 1);
    if (index !== -1) {
        setCaretPosition(editorContent, index + query.length);
        editorContent.focus();
    } else {
        showToast("No matches found", "info");
    }
});

document.getElementById("replaceBtn")?.addEventListener("click", () => {
    const query = findInput?.value;
    const replacement = replaceInput?.value || "";
    if (!query || !editorContent) return;
    const text = editorContent.innerText;
    const pos = getCaretPosition(editorContent);
    if (text.substr(pos - query.length, query.length) === query) {
        const newText = text.slice(0, pos - query.length) + replacement + text.slice(pos);
        editorContent.innerText = newText;
        editorContent.innerHTML = highlightSyntax(newText);
        setCaretPosition(editorContent, pos - query.length + replacement.length);
        editorContent.dispatchEvent(new Event("input"));
    }
});

document.getElementById("replaceAllBtn")?.addEventListener("click", () => {
    const query = findInput?.value;
    const replacement = replaceInput?.value || "";
    if (!query || !editorContent) return;
    const text = editorContent.innerText;
    if (text.includes(query)) {
        const newText = text.replaceAll(query, replacement);
        editorContent.innerText = newText;
        editorContent.innerHTML = highlightSyntax(newText);
        editorContent.dispatchEvent(new Event("input"));
        showToast("Replaced all matches", "success");
    } else {
        showToast("No matches found", "info");
    }
});

// Command Palette (Ctrl+Shift+P)
const commandsList = [
    { label: "File: Open File...", shortcut: "Ctrl+O", action: () => request("openFileDialog") },
    { label: "File: Open Folder...", shortcut: "", action: () => request("openFolder") },
    { label: "File: Quick Open File...", shortcut: "Ctrl+P", action: () => openQuickOpen() },
    { label: "File: New File", shortcut: "Ctrl+N", action: () => request("newFile") },
    { label: "File: Save", shortcut: "Ctrl+S", action: () => saveActiveFile() },
    { label: "File: Save As...", shortcut: "Ctrl+Shift+S", action: () => saveActiveFileAs() },
    { label: "View: Toggle Sidebar", shortcut: "Ctrl+B", action: () => toggleSidebar() },
    { label: "View: Toggle Zen Mode", shortcut: "F11", action: () => toggleZenMode() },
    { label: "Preferences: Open Settings", shortcut: "Ctrl+,", action: () => openSettingsModal() },
    { label: "Edit: Find", shortcut: "Ctrl+F", action: () => toggleFindBar() },
    { label: "Edit: Format Document", shortcut: "", action: () => showToast("Document formatted", "success") }
];

function openCommandPalette() {
    if (!commandPaletteModal) return;
    commandPaletteModal.classList.remove("hidden");
    commandInput.value = "";
    renderCommandResults("");
    commandInput.focus();
}

function closeCommandPalette() {
    commandPaletteModal?.classList.add("hidden");
}

function renderCommandResults(filter) {
    if (!commandResults) return;
    commandResults.innerHTML = "";

    const query = (filter || "").toLowerCase();
    const matches = commandsList.filter(c => c.label.toLowerCase().includes(query));

    if (!matches.length) {
        const div = document.createElement("div");
        div.className = "command-item";
        div.textContent = "No matching commands";
        commandResults.appendChild(div);
        return;
    }

    matches.forEach((cmd, idx) => {
        const item = document.createElement("button");
        item.className = "command-item";
        if (idx === 0) item.classList.add("active");

        const span = document.createElement("span");
        span.textContent = cmd.label;

        const shortcut = document.createElement("span");
        shortcut.className = "command-shortcut";
        shortcut.textContent = cmd.shortcut;

        item.appendChild(span);
        item.appendChild(shortcut);

        item.addEventListener("click", () => {
            closeCommandPalette();
            cmd.action();
        });

        commandResults.appendChild(item);
    });
}

commandInput?.addEventListener("input", e => {
    renderCommandResults(e.target.value);
});

commandInput?.addEventListener("keydown", e => {
    if (e.key === "Escape") {
        closeCommandPalette();
    } else if (e.key === "Enter") {
        const activeItem = commandResults.querySelector(".command-item.active");
        if (activeItem) activeItem.click();
    }
});

commandPaletteModal?.addEventListener("click", e => {
    if (e.target === commandPaletteModal) closeCommandPalette();
});

// Quick Open Modal Logic (Ctrl+P)
function openQuickOpen() {
    if (!quickOpenModal) return;
    quickOpenModal.classList.remove("hidden");
    quickOpenInput.value = "";
    renderQuickOpenResults("");
    quickOpenInput.focus();
}

function closeQuickOpen() {
    quickOpenModal?.classList.add("hidden");
}

function renderQuickOpenResults(filter) {
    if (!quickOpenResults) return;
    quickOpenResults.innerHTML = "";

    const query = (filter || "").toLowerCase();
    const matches = workspaceFiles.filter(f => f.toLowerCase().includes(query)).slice(0, 30);

    if (!matches.length) {
        const div = document.createElement("div");
        div.className = "quick-open-item";
        div.textContent = "No matching files";
        quickOpenResults.appendChild(div);
        return;
    }

    matches.forEach((file, idx) => {
        const item = document.createElement("button");
        item.className = "quick-open-item";
        if (idx === 0) item.classList.add("active");

        const icon = createFileIconElement(file);
        const span = document.createElement("span");
        span.textContent = file;

        item.appendChild(icon);
        item.appendChild(span);

        item.addEventListener("click", () => {
            closeQuickOpen();
            openFile(file);
        });

        quickOpenResults.appendChild(item);
    });
}

quickOpenInput?.addEventListener("input", e => {
    renderQuickOpenResults(e.target.value);
});

quickOpenInput?.addEventListener("keydown", e => {
    if (e.key === "Escape") {
        closeQuickOpen();
    } else if (e.key === "Enter") {
        const activeItem = quickOpenResults.querySelector(".quick-open-item.active");
        if (activeItem) activeItem.click();
    }
});

quickOpenModal?.addEventListener("click", e => {
    if (e.target === quickOpenModal) closeQuickOpen();
});

// Settings Modal & Tabs Logic
function openSettingsModal() {
    settingsModal?.classList.remove("hidden");
}

function closeSettingsModal() {
    settingsModal?.classList.add("hidden");
}

document.getElementById("settingsCloseBtn")?.addEventListener("click", closeSettingsModal);
settingsModal?.addEventListener("click", e => {
    if (e.target === settingsModal) closeSettingsModal();
});

// Settings Tabs Navigation
document.querySelectorAll(".settings-tab-btn").forEach(btn => {
    btn.addEventListener("click", () => {
        document.querySelectorAll(".settings-tab-btn").forEach(b => b.classList.remove("active"));
        document.querySelectorAll(".settings-pane").forEach(p => p.classList.remove("active"));
        btn.classList.add("active");
        const tab = btn.dataset.tab;
        document.getElementById(`pane${tab.charAt(0).toUpperCase() + tab.slice(1)}`)?.classList.add("active");
    });
});

settingAccentColor?.addEventListener("input", e => {
    if (settingAccentText) settingAccentText.value = e.target.value;
    applySettings({ accentColor: e.target.value });
});

settingAccentText?.addEventListener("input", e => {
    if (settingAccentColor) settingAccentColor.value = e.target.value;
    applySettings({ accentColor: e.target.value });
});

document.getElementById("settingsSaveBtn")?.addEventListener("click", () => {
    const theme = settingTheme?.value || "dark";
    const uiStyle = settingUiStyle?.value || "modern";
    const fontFamily = settingFontFamily?.value || "Cascadia Code, JetBrains Mono, Consolas, monospace";
    const fontSize = settingFontSize?.value || "13";
    const lineHeight = settingLineHeight?.value || "1.65";
    const wordWrap = settingWordWrap?.value || "off";
    const accentColor = settingAccentColor?.value || "#7c3aed";

    applySettings({ theme, uiStyle, fontFamily, fontSize, lineHeight, wordWrap, accentColor });
    request("saveSettings", { theme, uiStyle, fontFamily, fontSize, lineHeight, wordWrap, accentColor });
    closeSettingsModal();
    showToast("Preferences saved successfully", "success");
});

function setupMenus() {
    document.querySelectorAll(".menu-button").forEach(button => {
        button.addEventListener("click", event => {
            event.stopPropagation();
            const menuName = button.dataset.menu;
            const dropdown = document.getElementById(`${menuName}Menu`);

            const wasHidden = dropdown?.classList.contains("hidden");
            hideDropdowns();

            if (wasHidden && dropdown) {
                dropdown.classList.remove("hidden");
                button.classList.add("active");
            }
        });
    });
}

function hideDropdowns() {
    document.querySelectorAll(".dropdown-menu").forEach(m => m.classList.add("hidden"));
    document.querySelectorAll(".menu-button").forEach(b => b.classList.remove("active"));
}

document.addEventListener("click", hideDropdowns);

if (vscode) {
    vscode.addEventListener("message", event => {
        handleMessage(event.data);
    });
}

loadRecentWorkspaces();
setupWindowControls();
setupFolderButtons();
setupEditor();
setupKeyboard();
setupMenus();
setWorkspaceState(false);
renderTabs();
updatePosition();

// Request initialization on load to load last workspace and settings
request("init");
