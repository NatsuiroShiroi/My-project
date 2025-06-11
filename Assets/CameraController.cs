using EnvDTE;
using EnvDTE80;
using LibGit2Sharp;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

// alias to disambiguate LibGit2Sharp.Commands
using GitCommands = LibGit2Sharp.Commands;

namespace VSIXProject1
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("VSIXProject1", "Auto-commit & push on save", "1.0.14")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("01234567-89AB-CDEF-0123-456789ABCDEF")]
    public sealed class VSIXProject1Package : AsyncPackage
    {
        private DocumentEvents _docEvents;

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            VsShellUtilities.ShowMessageBox(
                this,
                "VSIXProject1Package.InitializeAsync has run!",
                "VSIXProject1 Debug",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            var dte = (DTE2)await GetServiceAsync(typeof(DTE));
            var events = (Events2)dte.Events;
            _docEvents = events.DocumentEvents;
            _docEvents.DocumentSaved += OnDocumentSaved;
        }

        private void OnDocumentSaved(Document doc)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // 1) DEBUG
            VsShellUtilities.ShowMessageBox(
                this,
                $"OnDocumentSaved fired for: {doc.Name}",
                "VSIXProject1 Debug",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            var path = doc.FullName;
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return;

            var root = Repository.Discover(path);
            if (root == null) return;

            // use LibGit2Sharp for commit
            using (var repo = new Repository(root))
            {
                var workdir = repo.Info.WorkingDirectory;
                var relPath = path.Substring(workdir.Length);
                GitCommands.Stage(repo, relPath);

                var name = repo.Config.Get<string>("user.name")?.Value ?? "VS AutoSave";
                var email = repo.Config.Get<string>("user.email")?.Value ?? "autosave@local";
                var author = new Signature(name, email, DateTimeOffset.Now);
                repo.Commit($"Auto-save: {Path.GetFileName(path)}", author, author);
            }

            // ==== DEBUG PUSH VIA CLI ====
            try
            {
                // 2) pre-push popup
                VsShellUtilities.ShowMessageBox(
                    this,
                    "About to shell out: git push origin HEAD",
                    "VSIXProject1 Push Debug",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                // Run `git push origin HEAD` in the repo root
                var psi = new ProcessStartInfo("git", "push origin HEAD")
                {
                    WorkingDirectory = root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                        throw new Exception($"git push failed: {p.StandardError.ReadToEnd()}");
                }

                // 3) success popup
                VsShellUtilities.ShowMessageBox(
                    this,
                    "Push succeeded via CLI!",
                    "VSIXProject1 Push Debug",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                // 4) failure popup
                VsShellUtilities.ShowMessageBox(
                    this,
                    $"Push failed: {ex.Message}",
                    "VSIXProject1 Push Debug",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            // ==== END DEBUG PUSH VIA CLI ====
        }
    }
}
