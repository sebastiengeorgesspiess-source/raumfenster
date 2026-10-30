using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using Unity.XR.XREAL;

public static class SpatialBuild
{
    public static void Configure()
    {
        PlayerSettings.companyName = "Sebastien";
        PlayerSettings.productName = "Raumfenster";
        PlayerSettings.bundleVersion = "0.6.1";
        PlayerSettings.Android.bundleVersionCode = 10;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "ch.sebastienspiess.raumfenster");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel31;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        QualitySettings.vSyncCount = 0;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
        var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = serialized.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 2; serialized.ApplyModifiedProperties(); }
        Directory.CreateDirectory("Assets/XR");
        var perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/Settings.asset");
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/Settings.asset");
        }
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (general == null)
        {
            general = ScriptableObject.CreateInstance<XRGeneralSettings>();
            AssetDatabase.AddObjectToAsset(general, perTarget);
            perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, general);
        }
        if (general.Manager == null)
        {
            general.Manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            AssetDatabase.AddObjectToAsset(general.Manager, perTarget);
        }
        general.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, "Unity.XR.XREAL.XREALXRLoader", BuildTargetGroup.Android))
            throw new Exception("XREAL loader assignment failed");
        var settings = XREALSettings.GetSettings();
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<XREALSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/XR/XREALSettings.asset");
            EditorBuildSettings.AddConfigObject(XREALSettings.k_SettingsKey, settings, true);
        }
#if UNITY_ANDROID
        var controllerGuids = AssetDatabase.FindAssets("XREALVirtualController t:Prefab", new[] { "Packages/com.xreal.xr" });
        if (controllerGuids.Length == 0) throw new Exception("XREAL controller prefab missing");
        settings.VirtualController = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(controllerGuids[0]));
#endif
        settings.InitialTrackingType = TrackingType.MODE_6DOF;
#if UNITY_ANDROID
        settings.SupportMultiResume = true;
        settings.AddtionalPermissions.Remove("android.permission.CAMERA");
        if(!settings.AddtionalPermissions.Contains("CAMERA"))settings.AddtionalPermissions.Add("CAMERA");
        settings.AddtionalPermissions.Remove("android.permission.RECORD_AUDIO");
        settings.AddtionalPermissions.Remove("RECORD_AUDIO");
#endif
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(general.Manager);
        EditorUtility.SetDirty(perTarget);
        var shader = Shader.Find("Raumfenster/Photo");
        if (shader == null) throw new Exception("Photo shader missing");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PhotoMaterial.mat");
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, "Assets/Resources/PhotoMaterial.mat"); }
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/WorldMaterial.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/Resources/WorldMaterial.mat");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/StarMaterial.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")), "Assets/Resources/StarMaterial.mat");
        var panoramaShader=Shader.Find("Raumfenster/ImmersivePanorama");
        if(panoramaShader==null)throw new Exception("Immersive panorama shader missing");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PanoramaMaterial.mat")==null)
            AssetDatabase.CreateAsset(new Material(panoramaShader),"Assets/Resources/PanoramaMaterial.mat");
        foreach(string file in new[]{"cyberpunk.png"})
        {
            var path="Assets/Resources/ImmersiveData/"+file;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null||texture.width!=texture.height*2)throw new Exception("Invalid panorama: "+file);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
        var forestImporter = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/WorldData/forest.jpg");
        if (forestImporter == null) throw new Exception("Forest panorama missing");
        forestImporter.maxTextureSize = 4096; forestImporter.mipmapEnabled = true;
        forestImporter.textureCompression = TextureImporterCompression.Compressed;
        forestImporter.npotScale = TextureImporterNPOTScale.None;
        forestImporter.wrapMode = TextureWrapMode.Repeat; forestImporter.SaveAndReimport();
        foreach (string name in new[] { "earth", "moon", "mars", "jupiter", "stars_milky_way" })
        {
            var path = "Assets/Resources/SpaceData/" + name + ".jpg";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width < 1024) throw new Exception("Missing demo texture: " + name);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
        }
        foreach (string texturePath in new[] { "Assets/Resources/PhotoData/left.jpg", "Assets/Resources/PhotoData/stereo.jpg" }) { var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath); importer.maxTextureSize = 4096; importer.mipmapEnabled = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport(); }
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/AtmosphereMaterial.mat")==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Raumfenster/Atmosphere")),"Assets/Resources/AtmosphereMaterial.mat");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SkinCameraMaterial.mat")==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Raumfenster/SkinCamera")),"Assets/Resources/SkinCameraMaterial.mat");
        MedicalBuild.Prepare();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Raumfenster").AddComponent<SpatialWindow>();
        EditorSceneManager.SaveScene(scene, "Assets/Raumfenster.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Raumfenster.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("RAUMFENSTER_CONFIGURATION_OK");
    }

    public static void Build()
    {
        Configure();
        Directory.CreateDirectory("../builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Raumfenster.unity" }, target = BuildTarget.Android,
            locationPathName = "../builds/Raumfenster.apk", options = BuildOptions.None });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Raumfenster APK build failed");
        Debug.Log("RAUMFENSTER_APK_OK");
    }
}

