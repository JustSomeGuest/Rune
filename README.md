<p align="center">
  <img src="UI/Assets/logo/rune.svg" alt="Rune" width="120" />
</p>

<h1 align="center">Rune</h1>

<p align="center">
  A fast, modern code editor that just works.<br/>
  No accounts. No sign-in. No unnecessary stuff.
</p>

---

## Why Rune?

A lot of modern editors ask you to sign in with Microsoft, Google, or another account before you can even start coding.

Rune keeps things simple.

Download it, open it, and start writing code. No account, no sign-in, and no setup process getting in your way.

## Features

* **No account required** - No Microsoft account, Google account, or sign-in of any kind.
* **Fast and responsive** - Debounced syntax highlighting and an optimized input pipeline keep typing smooth and responsive.
* **Single `.exe`** - Everything is bundled into one executable. UI assets are extracted to AppData automatically when Rune first starts.
* **Built-in installer** - A simple setup wizard lets you choose the install location and optionally create Desktop and Start Menu shortcuts.
* **Syntax highlighting** - Supports 39+ languages out of the box, including C, C++, C#, Python, Rust, Go, TypeScript, JavaScript, HTML, CSS, JSON, YAML, Lua, Ruby, PHP, Swift, Kotlin, and more.
* **File explorer** - Browse your projects with a sidebar folder tree, SVG language icons, and useful context menus.
* **Tabs** - Work with multiple files at once with a simple tab bar and dirty file indicators.
* **Find and replace** - Quickly search through the current file and replace text when needed.
* **Command palette** - Press `Ctrl+Shift+P` to quickly find and run commands.
* **Quick open** - Press `Ctrl+P` to quickly find and open files in your workspace.
* **Themes** - Choose from Dark, Midnight, Dracula, Monokai, Light, and High Contrast.
* **UI styles** - Switch between Modern and Classic layouts.
* **Accent colors** - Pick an accent color that fits your setup.
* **Zen mode** - Press `F11` for a distraction-free editing experience.
* **Context menus** - Right-click files, tabs, and the editor to access relevant actions.
* **Custom titlebar** - A custom window titlebar provides native-feeling controls, dragging, and resizing.
* **Persistent settings** - Rune remembers your theme, workspace, and preferences between sessions.
* **Recent workspaces** - Quickly get back to projects you've worked on recently.
* **Binary file detection** - Rune detects files such as `.zip`, `.exe`, images, and other binary formats and shows a clean error instead of trying to open them. Minified and obfuscated source code still opens normally.
* **WebView2 powered** - The interface is built on WebView2, giving Rune a modern Chromium-based rendering engine.

## Getting Started

1. Download the latest `Rune.exe` from [Releases](https://github.com/JustSomeGuest/Rune/releases/latest).
2. Run it. The setup wizard will appear the first time.
3. Choose your install folder and decide whether you want Desktop or Start Menu shortcuts.
4. Click **Install**.
5. Open Rune and start coding.

No account required.

## Keyboard Shortcuts

| Shortcut       | Action          |
| -------------- | --------------- |
| `Ctrl+N`       | New file        |
| `Ctrl+O`       | Open file       |
| `Ctrl+S`       | Save            |
| `Ctrl+Shift+S` | Save as         |
| `Ctrl+P`       | Quick open      |
| `Ctrl+Shift+P` | Command palette |
| `Ctrl+F`       | Find            |
| `Ctrl+H`       | Replace         |
| `Ctrl+B`       | Toggle sidebar  |
| `Ctrl+,`       | Preferences     |
| `F11`          | Zen mode        |

## Coming Soon

### GitHub Integration

GitHub support is in the works.

The goal is to let you clone repositories, commit changes, push updates, and manage pull requests directly from Rune.

## Tech Stack

Rune is built with a small and straightforward stack:

* **.NET 8 + WPF** - Native Windows application.
* **WebView2** - Powers the modern editor interface.
* **Vanilla JavaScript** - Handles the UI without a large frontend framework.

## Building from Source

Clone the repository:

```bash
git clone https://github.com/JustSomeGuest/Rune.git
cd Rune
dotnet build
```

To create a self-contained single-file executable:

```bash
dotnet publish Rune.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The resulting executable will be placed in the `publish` directory.
Ship the whole `publish` folder (or at least `Rune.exe`) — older releases
that uploaded only `Rune.exe` without bundled native libraries crashed
silently on launch.

## License

Rune is licensed under the MIT License.

```text
MIT License

Copyright (c) JustSomeGuest

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

**In simple terms:** You can modify, use, and sell Rune or your own versions of it. Just keep the original license and credit **JustSomeGuest**, the original creator.

---

<p align="center">
  Built for developers who just want to code.
</p>
