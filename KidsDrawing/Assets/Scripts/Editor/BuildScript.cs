using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace KidsDrawing.Editor
{
    // iOS用のXcodeプロジェクトを Builds/iOS に出力する。
    // Unity -batchmode -quit -projectPath KidsDrawing -buildTarget iOS -executeMethod KidsDrawing.Editor.BuildScript.BuildIOS [-development]
    public static class BuildScript
    {
        const string OutputPath = "Builds/iOS";

        [MenuItem("KidsDrawing/Build iOS (Xcode project)")]
        public static void BuildIOS()
        {
            var options = BuildOptions.None;
            if (Environment.GetCommandLineArgs().Contains("-development"))
                options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                options = options,
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"iOS build failed: {report.summary.result}");
        }
    }
}
