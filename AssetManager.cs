using System;
using System.IO;
using System.Reflection;

namespace Rune
{
    public static class AssetManager
    {
        public static string AppDataDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rune");

        public static string UiDir => Path.Combine(AppDataDir, "UI");

        public static string AssetsDir => Path.Combine(UiDir, "Assets");

        public static string LogoDir => Path.Combine(AssetsDir, "logo");

        public static string IndexHtmlPath => Path.Combine(UiDir, "index.html");

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
                {
                    string fileName = relative.Substring("assets.logo.".Length);
                    destPath = Path.Combine(AssetsDir, "logo", fileName);
                }
                else if (relative.StartsWith("assets.", StringComparison.Ordinal))
                {
                    string fileName = relative.Substring("assets.".Length);
                    destPath = Path.Combine(AssetsDir, fileName);
                }
                else
                {
                    destPath = Path.Combine(UiDir, relative);
                }

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
    }
}
