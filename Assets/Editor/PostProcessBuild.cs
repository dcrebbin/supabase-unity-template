using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEngine;

public class PostProcessBuild : MonoBehaviour
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuildProject)
    {
        if (target == BuildTarget.iOS)
        {
            ModifyPlistForDeepLinking(pathToBuildProject);
        }
    }

    private static void ModifyPlistForDeepLinking(string pathToBuildProject)
    {
        // Path to the Info.plist file
        string plistPath = Path.Combine(pathToBuildProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Add a custom URL scheme (for deep linking)
        PlistElementDict rootDict = plist.root;
        PlistElementArray urlTypesArray;

        if (!rootDict.values.ContainsKey("CFBundleURLTypes"))
        {
            urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
        }
        else
        {
            urlTypesArray = rootDict["CFBundleURLTypes"].AsArray();
        }

        // Add a new URL scheme
        PlistElementDict urlDict = urlTypesArray.AddDict();
        urlDict.SetString("CFBundleURLName", "com.dcrebbin.supabaseunity");
        PlistElementArray urlSchemesArray = urlDict.CreateArray("CFBundleURLSchemes");
        urlSchemesArray.AddString("supabaseunity"); // Replace with your deep link scheme

        // Save the changes back to the Info.plist
        plist.WriteToFile(plistPath);

        UnityEngine.Debug.Log("Info.plist modified successfully!");
    }
}