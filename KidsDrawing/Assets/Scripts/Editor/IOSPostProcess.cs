using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace KidsDrawing.Editor
{
    // 出力した Xcode プロジェクトの Info.plist に、写真アプリへ保存するための説明文を加える。
    public static class IOSPostProcess
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
#if UNITY_IOS
            if (target != BuildTarget.iOS)
                return;

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSPhotoLibraryAddUsageDescription", "描いた絵を写真アプリに保存します");
            plist.WriteToFile(plistPath);
#endif
        }
    }
}
