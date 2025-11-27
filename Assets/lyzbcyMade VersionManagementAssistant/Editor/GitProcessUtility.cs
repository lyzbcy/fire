using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Fire.VersionControlAssistant
{
    internal static class GitProcessUtility
    {
        private static readonly Encoding ConsoleEncoding = new UTF8Encoding(false, true);

        public static string ProjectRoot
        {
            get
            {
                try
                {
                    return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[Version Control Assistant] 无法获取项目根目录: {ex.Message}");
                    return Application.dataPath;
                }
            }
        }

        public static GitProcessResult Run(string arguments, bool logOnError = true)
        {
            try
            {
                // 验证项目根目录
                var projectRoot = ProjectRoot;
                if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
                {
                    var error = $"项目根目录无效或不存在: {projectRoot}";
                    if (logOnError)
                    {
                        UnityEngine.Debug.LogError($"[Version Control Assistant] {error}");
                    }
                    return new GitProcessResult(false, string.Empty, error);
                }

                // 跨平台 Git 命令查找
                var gitCommand = GetGitCommand();
                if (string.IsNullOrEmpty(gitCommand))
                {
                    var error = "未找到 Git 命令，请确保已安装 Git 并添加到系统 PATH";
                    if (logOnError)
                    {
                        UnityEngine.Debug.LogError($"[Version Control Assistant] {error}");
                    }
                    return new GitProcessResult(false, string.Empty, error);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = gitCommand,
                    Arguments = arguments,
                    WorkingDirectory = projectRoot,
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
                    UnityEngine.Debug.LogError($"[Version Control Assistant] git {arguments}\n{result.Error}");
                }

                return result;
            }
            catch (Exception ex)
            {
                if (logOnError)
                {
                    UnityEngine.Debug.LogError($"[Version Control Assistant] 执行 git {arguments} 失败: {ex}");
                }
                return new GitProcessResult(false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// 跨平台获取 Git 命令路径
        /// </summary>
        private static string GetGitCommand()
        {
            // Windows: 尝试 "git" 或 "git.exe"
            // macOS/Linux: 通常就是 "git"
            var candidates = new[] { "git", "git.exe" };
            
            foreach (var candidate in candidates)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit(1000); // 1秒超时
                        if (process.ExitCode == 0)
                        {
                            return candidate;
                        }
                    }
                }
                catch
                {
                    // 继续尝试下一个候选
                }
            }

            return null;
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

        public static IReadOnlyList<GitCommitEntry> GetRecentCommits(int count = 12)
        {
            var format = "%h%x1F%an%x1F%cr%x1F%s";
            var arguments = $"log -n {count} --pretty=format:\"{format}\"";
            var result = Run(arguments, logOnError: false);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            {
                return Array.Empty<GitCommitEntry>();
            }

            var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return Array.Empty<GitCommitEntry>();
            }

            var entries = new List<GitCommitEntry>(lines.Length);
            foreach (var line in lines)
            {
                var parts = line.Split('\x1F');
                if (parts.Length < 4)
                {
                    continue;
                }

                entries.Add(new GitCommitEntry(
                    parts[0].Trim(),
                    parts[1].Trim(),
                    parts[2].Trim(),
                    parts[3].Trim()));
            }

            return entries;
        }

        public static IReadOnlyList<GitCommitEntry> GetRemoteCommits(string remote, string branch, int count, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(remote) || string.IsNullOrWhiteSpace(branch))
            {
                error = GitLocalization.Tr("pullPicker.invalidTarget");
                return Array.Empty<GitCommitEntry>();
            }

            var format = "%h%x1F%an%x1F%cr%x1F%s";
            var arguments = $"log -n {count} --pretty=format:\"{format}\" {remote}/{branch}";
            var result = Run(arguments, logOnError: false);
            if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            {
                error = string.IsNullOrWhiteSpace(result.Error)
                    ? GitLocalization.Tr("pullPicker.empty")
                    : result.Error;
                return Array.Empty<GitCommitEntry>();
            }

            var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                error = GitLocalization.Tr("pullPicker.empty");
                return Array.Empty<GitCommitEntry>();
            }

            var entries = new List<GitCommitEntry>(lines.Length);
            foreach (var line in lines)
            {
                var parts = line.Split('\x1F');
                if (parts.Length < 4)
                {
                    continue;
                }

                entries.Add(new GitCommitEntry(
                    parts[0].Trim(),
                    parts[1].Trim(),
                    parts[2].Trim(),
                    parts[3].Trim()));
            }

            if (entries.Count == 0)
            {
                error = GitLocalization.Tr("pullPicker.empty");
            }

            return entries;
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

    internal readonly struct GitCommitEntry
    {
        public string Hash { get; }
        public string Author { get; }
        public string RelativeTime { get; }
        public string Message { get; }

        public GitCommitEntry(string hash, string author, string relativeTime, string message)
        {
            Hash = hash;
            Author = author;
            RelativeTime = relativeTime;
            Message = message;
        }
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

