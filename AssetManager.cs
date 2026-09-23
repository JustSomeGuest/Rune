using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Rune
{
    public static class AssetManager
    {
        private const string GitHubBase = "https://raw.githubusercontent.com/JustSomeGuest/Rune/main/UI/Assets/";

        public static string AppDataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rune");

        public static string UiDir => Path.Combine(AppDataDir, "UI");
        public static string AssetsDir => Path.Combine(UiDir, "Assets");
        public static string LogoDir => Path.Combine(AssetsDir, "logo");
        public static string IndexHtmlPath => Path.Combine(UiDir, "index.html");
        public static string InstallMarker => Path.Combine(AppDataDir, ".installed");

        public static bool IsInstalled => File.Exists(InstallMarker);

        public static void MarkInstalled()
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(InstallMarker, DateTime.UtcNow.ToString("O"));
        }

        public static void EnsureExtracted()
        {
            Directory.CreateDirectory(UiDir);
            Directory.CreateDirectory(AssetsDir);
            Directory.CreateDirectory(LogoDir);

            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resources = asm.GetManifestResourceNames();

            foreach (string res in resources)
            {
                if (!res.StartsWith("rune.ui.", StringComparison.Ordinal))
                    continue;

                string relative = res.Substring("rune.ui.".Length);

                string destPath;
                if (relative.StartsWith("assets.logo.", StringComparison.Ordinal))
                    destPath = Path.Combine(LogoDir, relative.Substring("assets.logo.".Length));
                else if (relative.StartsWith("assets.", StringComparison.Ordinal))
                    destPath = Path.Combine(AssetsDir, relative.Substring("assets.".Length));
                else
                    destPath = Path.Combine(UiDir, relative);

                string? destParent = Path.GetDirectoryName(destPath);
                if (destParent != null)
                    Directory.CreateDirectory(destParent);

                using Stream? stream = asm.GetManifestResourceStream(res);
                if (stream == null)
                    continue;

                using FileStream fs = File.Create(destPath);
                stream.CopyTo(fs);
            }
        }

        public static async Task DownloadMissingAssetsAsync()
        {
            string[] iconNames =
            {
                "bash", "c", "clojure", "cpp", "csharp", "css", "dart", "elixir",
                "erlang", "fortran", "fsharp", "go", "haskell", "html", "java",
                "javascript", "json", "julia", "kotlin", "lua", "luau", "markdown",
                "nim", "o", "ocaml", "perl", "php", "powershell", "python", "r",
                "ruby", "rune", "rust", "scala", "solidity", "swift", "typescript",
                "yaml", "zig"
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            foreach (string name in iconNames)
            {
                string path = Path.Combine(AssetsDir, name + ".svg");
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                    continue;

                try
                {
                    byte[] data = await http.GetByteArrayAsync(GitHubBase + name + ".svg");
                    await File.WriteAllBytesAsync(path, data);
                }
                catch { }
            }

            string runeSvg = Path.Combine(AssetsDir, "rune.svg");
            if (!File.Exists(runeSvg) || new FileInfo(runeSvg).Length == 0)
            {
                try
                {
                    byte[] data = await http.GetByteArrayAsync(GitHubBase + "rune.svg");
                    await File.WriteAllBytesAsync(runeSvg, data);
                }
                catch { }
            }

            string logoDir = Path.Combine(AssetsDir, "logo");
            string[] logoFiles =
            {
                "favicon.ico", "favicon-16x16.png", "favicon-32x32.png",
                "favicon-48x48.png", "favicon-64x64.png", "favicon-96x96.png",
                "favicon-128x128.png", "favicon-180x180.png", "favicon-192x192.png",
                "favicon-256x256.png", "favicon-384x384.png", "favicon-512x512.png",
                "rune.svg"
            };

            foreach (string f in logoFiles)
            {
                string path = Path.Combine(logoDir, f);
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                    continue;

                try
                {
                    byte[] data = await http.GetByteArrayAsync(GitHubBase + "logo/" + f);
                    await File.WriteAllBytesAsync(path, data);
                }
                catch { }
            }
        }
    }
}
