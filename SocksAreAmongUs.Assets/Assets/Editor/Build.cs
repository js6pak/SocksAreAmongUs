#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BuiltInResourcesWindow : EditorWindow
{
    [MenuItem("AssetBundles/Make AssetBundle")]
    public static void MakeAssetBundle()
    {
        var assetBundleDirectory = Path.Combine("AssetBundles", "StandaloneWindows");
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        foreach (var file in Directory.GetFiles(assetBundleDirectory))
        {
            File.Delete(file);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows);

        foreach (var file in Directory.GetFiles(assetBundleDirectory))
        {
            if (Path.GetExtension(file) == string.Empty && Path.GetFileName(file) != Path.GetFileName(assetBundleDirectory))
            {
                var destination = Path.Combine("..", "SocksAreAmongUs", "Assets", Path.GetFileName(file) + ".bundle");
                File.Delete(destination);
                File.Copy(file, destination);
            }
        }
    }
}
#endif
