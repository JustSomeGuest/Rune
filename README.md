<p align="center">
  <img src="UI/Assets/logo/rune.svg" alt="Rune" width="120" />
</p>

<h1 align="center">Rune</h1>

<p align="center">
  A fast, modern code editor that just works.<br/>
  No accounts. No sign-in. No nonsense.
</p>

---

## Why Rune?

Most editors want you to sign in with Microsoft, Google, or some other account before you can write a single line of code. **Rune doesn't.** Download it, open it, start coding. That's it.

## Features

- **No account required** — no Microsoft, no Google, no sign-in of any kind
- **Blazing fast** — debounced syntax highlighting and optimized input pipeline for zero-lag typing
- **Single `.exe`** — everything is embedded; UI assets extract to AppData on first run
- **Built-in installer wizard** — custom setup with optional desktop and Start Menu shortcuts
- **Syntax highlighting** — supports 39+ languages out of the box (C, C++, C#, Python, Rust, Go, TypeScript, JavaScript, HTML, CSS, JSON, YAML, Lua, Ruby, PHP, Swift, Kotlin, and many more)
- **File explorer** — sidebar with folder tree, SVG language icons, context menus
- **Tabs** — multiple files, drag-friendly tab bar, dirty indicators
- **Find & replace** — quick in-file search with replace support
- **Command palette** — `Ctrl+Shift+P` for quick access to all commands
- **Quick open** — `Ctrl+P` to jump to any file instantly
- **Themes** — Dark, Midnight, Dracula, Monokai, Light, High Contrast
- **UI styles** — Modern and Classic layouts
- **Accent colors** — customize the accent color to your liking
- **Zen mode** — distraction-free editing with `F11`
- **Context menus** — right-click everywhere: editor, tabs, file tree
- **Custom titlebar** — native-feel window chrome with proper drag and resize
- **Persistent settings** — theme, workspace, and preferences remembered across sessions
- **Recent workspaces** — jump back into your last project
- **Binary file detection** — clean error for `.zip`, `.exe`, images, and other binaries (minified/obfuscated code still opens fine)
- **WebView2 powered** — modern Chromium rendering engine under the hood

## Getting Started

1. Download the latest `Rune.exe` from [Releases](https://github.com/JustSomeGuest/Rune/releases/latest)
2. Run it — the setup wizard appears on first launch
3. Choose your install folder, pick your shortcuts, hit **Install**
4. Start coding — no account, no sign-in, no delays

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New file |
| `Ctrl+O` | Open file |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save as |
| `Ctrl+P` | Quick open |
| `Ctrl+Shift+P` | Command palette |
| `Ctrl+F` | Find |
| `Ctrl+H` | Replace |
| `Ctrl+B` | Toggle sidebar |
| `Ctrl+,` | Preferences |
| `F11` | Zen mode |

## Coming Soon

- **GitHub integration** — clone, commit, push, and manage PRs directly from Rune. GitHub support is coming soon!

## Tech Stack

- **.NET 8** + **WPF** — native Windows performance
- **WebView2** — Chromium-based UI rendering
- **Vanilla JS** — no frameworks, no bloat

## Building from Source

```bash
git clone https://github.com/JustSomeGuest/Rune.git
cd Rune
dotnet build
```

To publish a single-file executable:

```bash
dotnet publish Rune.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## License

MIT

---

<p align="center">
  Built for developers who just want to code.
</p>
