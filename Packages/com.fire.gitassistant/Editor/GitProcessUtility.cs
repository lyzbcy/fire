using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fire.GitAssistant
{
    internal static class GitProcessUtility
    {
        private static readonly Encoding ConsoleEncoding = new UTF8Encoding(false, true);

        public static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static GitProcessResult Run(string arguments, bool logOnError = true)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = ConsoleEncoding,
                    StandardErrorEncoding = ConsoleEncoding,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                var result = new GitProcessResult(
                    process.ExitCode == 0,
                    outputBuilder.ToString().Trim(),
                    errorBuilder.ToString().Trim());

                if (!result.Success && logOnError)
                {
                    UnityEngine.Debug.LogError($"[Git 助手] git {arguments}\n{result.Error}");
                }

                return result;
            }
            catch (Exception ex)
            {
                if (logOnError)
                {
                    UnityEngine.Debug.LogError($"[Git 助手] 执行 git {arguments} 失败: {ex}");
                }
                return new GitProcessResult(false, string.Empty, ex.Message);
            }
        }

        public static IReadOnlyList<GitStatusEntry> GetStatusEntries()
        {
            var result = Run("status --porcelain");
            if (!result.Success)
            {
                return Array.Empty<GitStatusEntry>();
            }

            var entries = new List<GitStatusEntry>();
            using var reader = new StringReader(result.Output);
            while (reader.ReadLine() is { } line)
            {
                if (line.Length < 3)
                {
                    continue;
                }

                var status = line[..2];
                var path = line[3..].Trim();
                entries.Add(new GitStatusEntry(path.Replace('\\', '/'), status.Trim()));
            }

            return entries;
        }

        public static string GetCurrentBranch()
        {
            var result = Run("rev-parse --abbrev-ref HEAD");
            return result.Success ? result.Output : string.Empty;
        }

        public static string GetRecentLog(int count = 12)
        {
            var arguments = $"log --graph --oneline -n {count}";
            var result = Run(arguments, logOnError: false);
            return result.Success ? result.Output : GitLocalization.Tr("log.empty");
        }

        public static IReadOnlyList<string> GetRemoteNames()
        {
            var result = Run("remote", logOnError: false);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            {
                return Array.Empty<string>();
            }

            return result.Output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .ToArray();
        }
    }

    internal readonly struct GitProcessResult
    {
        public bool Success { get; }
        public string Output { get; }
        public string Error { get; }

        public GitProcessResult(bool success, string output, string error)
        {
            Success = success;
            Output = output;
            Error = error;
        }
    }

    internal enum GitChangeKind
    {
        Untracked,
        Added,
        Modified,
        Deleted,
        Renamed,
        Unknown
    }

    internal readonly struct GitStatusEntry
    {
        public string Path { get; }
        public string Status { get; }

        public GitStatusEntry(string path, string status)
        {
            Path = path;
            Status = status;
        }

        public GitChangeKind Kind => Status switch
        {
            "??" => GitChangeKind.Untracked,
            "A" or "A " or " A" => GitChangeKind.Added,
            "M" or "MM" or " M" or "M " => GitChangeKind.Modified,
            "D" or " D" or "D " => GitChangeKind.Deleted,
            "R" or "R " or " R" => GitChangeKind.Renamed,
            _ => GitChangeKind.Unknown
        };

        public string StatusLabel => GitLocalization.GetStatusLabel(Kind);
    }
}

