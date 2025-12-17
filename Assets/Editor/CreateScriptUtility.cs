using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using System.IO;

public static class CreateScriptUtility
{
    private const string TemplatePath = "Assets/Editor/ScriptTemplates/CustomScriptTemplate.txt";

    [MenuItem("Assets/Create/Custom/Create Empty C# Script %#x")] // Ctrl+Shift+X
    public static void CreateScript()
    {
        string targetPath = GetSelectedPathOrFallback();
        string fullPath = Path.Combine(targetPath, "NewScript.cs");
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            0,
            ScriptableObject.CreateInstance<DoCreateScriptAsset>(),
            fullPath,
            EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D,
            TemplatePath
        );
    }

    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (File.Exists(assetPath))
                return Path.GetDirectoryName(assetPath);
            else if (Directory.Exists(assetPath))
                return assetPath;
        }
        return path;
    }

    private class DoCreateScriptAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            string scriptText = File.ReadAllText(resourceFile);
            string fileName = Path.GetFileNameWithoutExtension(pathName);
            scriptText = scriptText.Replace("#SCRIPTNAME#", fileName);
            File.WriteAllText(pathName, scriptText);
            AssetDatabase.ImportAsset(pathName);
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(pathName);
            ProjectWindowUtil.ShowCreatedAsset(asset);
        }

        public override void Cancelled(int instanceId, string pathName, string resourceFile)
        {
            // Just in case Unity leaves a ghost asset — try cleanup
            if (File.Exists(pathName))
            {
                File.Delete(pathName);
                File.Delete(pathName + ".meta");
                AssetDatabase.Refresh();
            }
        }
    }
}
