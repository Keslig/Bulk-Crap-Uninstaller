using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Klocman.Extensions;
using Klocman.Tools;

namespace UninstallTools.Junk
{
    public class WerScanner : JunkCreatorBase
    {
        private const string CrashLabel = "AppCrash_";
        private static readonly ICollection<string> Archives;
        private ICollection<string> _werReportPaths;

        static WerScanner()
        {
            Archives = new[]
            {
                WindowsTools.GetEnvironmentPath(Klocman.Native.CSIDL.CSIDL_COMMON_APPDATA),
                WindowsTools.GetEnvironmentPath(Klocman.Native.CSIDL.CSIDL_LOCAL_APPDATA)
            }.SelectMany(x =>new[]
            {
                Path.Combine(x, @"Microsoft\Windows\WER\ReportArchive"),
                Path.Combine(x, @"Microsoft\Windows\WER\ReportQueue")
            }).Where(Directory.Exists)
            .ToArray();
        }

        public override void Setup(ICollection<ApplicationUninstallerEntry> allUninstallers)
        {
            base.Setup(allUninstallers);

            _werReportPaths = Archives.Attempt(Directory.GetDirectories).SelectMany(x => x).ToArray();
        }

        public override IEnumerable<IJunkResult> FindJunk(ApplicationUninstallerEntry target)
        {
            if (target.SortedExecutables == null || target.SortedExecutables.Length == 0)
                yield break;

            var appExecutables = target.SortedExecutables.Attempt(Path.GetFileName).ToList();

            foreach (var reportPath in _werReportPaths)
            {
                var startIndex = reportPath.LastIndexOf(CrashLabel, StringComparison.InvariantCultureIgnoreCase);
                if (startIndex <= 0) continue;
                startIndex = startIndex + CrashLabel.Length;

                var count = reportPath.IndexOf('_', startIndex) - startIndex;
                if (count <= 1) continue;

                var filename = reportPath.Substring(startIndex, count);

                if (appExecutables.Any(x => x.StartsWith(filename, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var node = new FileSystemJunk(new DirectoryInfo(reportPath),target, this);
                    node.Confidence.Add(ConfidenceRecord.ExplicitConnection);
                    yield return node;
                }
            }
        }
    }
}