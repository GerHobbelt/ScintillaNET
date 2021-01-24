using System;
using System.Collections.Generic;
using FlubuCore.Context;
using FlubuCore.Context.Attributes.BuildProperties;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Attributes;
using FlubuCore.Tasks.Versioning;

namespace build
{
    public class BuildScript : DefaultBuildScript
    {
        // 指定建置結果的輸出目錄。
        public FullPath OutputDirectory => RootDirectory.CombineWith("output");

        // 指定 .sln 檔案。這裡加上了 "source/"，是因為我把建置專案放在 repository 的跟目錄。
        [SolutionFileName]
        public string SolutionFileName => RootDirectory.CombineWith("src/ScintillaNET.sln");

        [BuildConfiguration]
        public string BuildConfiguration { get; set; } = "Release"; // Debug or Release        

        private List<string> _projectsToPack = new List<string>()
        {
            "src/ScintillaNET",
        };

        protected override void ConfigureTargets(ITaskContext context)
        {
            Console.WriteLine($"Output folder: {OutputDirectory}");

            var buildVersion = context.CreateTarget("buildVersion")
                        .SetAsHidden()
                        .SetDescription("Fetch version number from CHANGELOG.md file.")
                        .AddTask(x => x.FetchBuildVersionFromFileTask()
                            .ProjectVersionFileName("../CHANGELOG.md")
                            .RemovePrefix("## huanlin.ScintillaNET "));

            var clean = context.CreateTarget("clean")
                .SetDescription("Clean output folder.")
                .AddCoreTask(x => x.Clean()
                    .CleanOutputDir())
                .DependsOn(buildVersion);

            var compile = context.CreateTarget("compile")
                .SetDescription("Build solution。")
                .AddCoreTask(x => x.UpdateNetCoreVersionTask("src/ScintillaNET/ScintillaNET.csproj"))
                .AddCoreTask(x => x.Build())
                .DependsOn(clean);

            var pack = context.CreateTarget("pack")
                .SetDescription("Build nuget packages for publishing.")
                .ForEach(_projectsToPack, (project, target) =>
                {
                    target.AddCoreTask(x => x.Pack()
                    .Project(project)
                    .IncludeSymbols()
                    .OutputDirectory(OutputDirectory));
                })
                .DependsOn(compile);
        }
    }
}
