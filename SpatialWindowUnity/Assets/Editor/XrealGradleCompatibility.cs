using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;

// XREAL 3.1.0 ships nr_common and nr_loader under the same nrsdk.pack namespace.
// AGP 9 changed this from allowed to an error. Preserve the documented AGP 8
// compatibility setting until XREAL publishes libraries with distinct namespaces.
// https://developer.android.com/build/releases/agp-9-0-0-release-notes
public class XrealGradleCompatibility : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string properties = Path.Combine(Directory.GetParent(path).FullName, "gradle.properties");
        const string setting = "android.uniquePackageNames=false";
        if (!File.ReadAllText(properties).Contains(setting))
            File.AppendAllText(properties, "\n# XREAL SDK 3.1 namespace compatibility\n" + setting + "\n");
        // Android 11+ requires explicit visibility for the system TTS service.
        string manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        var document = XDocument.Load(manifest);
        XNamespace android = "http://schemas.android.com/apk/res/android";
        var queries = document.Root.Element("queries");
        if (queries == null) { queries = new XElement("queries"); document.Root.Add(queries); }
        bool hasTts = false;
        foreach (var action in queries.Descendants("action"))
            hasTts |= (string)action.Attribute(android + "name") == "android.intent.action.TTS_SERVICE";
        if (!hasTts) queries.Add(new XElement("intent", new XElement("action",
            new XAttribute(android + "name", "android.intent.action.TTS_SERVICE"))));
        document.Save(manifest);
    }
}
