using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class MedicalBuild
{
    public static void Prepare()
    {
        const string source="Assets/MedicalSource/Cardiovascular.fbx";
        var importer=AssetImporter.GetAtPath(source) as ModelImporter;
        if(importer==null){if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MedicalHeart.prefab")!=null){Debug.Log("Using attributed pre-extracted Z-Anatomy heart");return;}throw new Exception("Unpack assets.zip first: heart prefab missing");}
        if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source));
        var names=new[]{"Right atrium","Left atrium","Right ventricle","Left ventricle","Ascending aorta","Aortic arch","Pulmonary trunk"};
        var selected=root.GetComponentsInChildren<MeshFilter>(true).Where(x=>names.Contains(x.name)).ToArray();
        foreach(var name in names.Take(4))if(!selected.Any(x=>x.name==name))throw new Exception("Missing heart structure: "+name);
        Directory.CreateDirectory("Assets/Resources/MedicalData");
        var bounds=selected[0].GetComponent<Renderer>().bounds;
        foreach(var part in selected)bounds.Encapsulate(part.GetComponent<Renderer>().bounds);
        float scale=1/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
        var result=new GameObject("Z-Anatomy teaching heart");
        foreach(var sourcePart in selected)
        {
            var mesh=UnityEngine.Object.Instantiate(sourcePart.sharedMesh);
            var matrix=sourcePart.transform.localToWorldMatrix;var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)vertices[i]=(matrix.MultiplyPoint3x4(vertices[i])-bounds.center)*scale;
            mesh.vertices=vertices;
            var normals=mesh.normals;var normalMatrix=matrix.inverse.transpose;
            for(int i=0;i<normals.Length;i++)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;
            mesh.normals=normals;
            if(matrix.determinant<0){for(int sub=0;sub<mesh.subMeshCount;sub++){var tris=mesh.GetTriangles(sub);for(int i=0;i<tris.Length;i+=3){int swap=tris[i];tris[i]=tris[i+1];tris[i+1]=swap;}mesh.SetTriangles(tris,sub);}}
            mesh.RecalculateBounds();
            string path="Assets/Resources/MedicalData/"+sourcePart.name.Replace(' ','_')+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            var part=new GameObject(sourcePart.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(result.transform,false);part.GetComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Shader.Find("Standard"));material.color=sourcePart.name.StartsWith("Right")?new Color(.28f,.36f,.68f):new Color(.64f,.18f,.20f);material.SetFloat("_Glossiness",.28f);
            string matPath=path.Replace(".asset",".mat");var existingMat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(existingMat==null)AssetDatabase.CreateAsset(material,matPath);else{EditorUtility.CopySerialized(material,existingMat);UnityEngine.Object.DestroyImmediate(material);material=existingMat;}
            part.GetComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();
            Debug.Log("[Medical build] "+sourcePart.name+" vertices="+mesh.vertexCount);
        }
        PrefabUtility.SaveAsPrefabAsset(result,"Assets/Resources/MedicalHeart.prefab");
        UnityEngine.Object.DestroyImmediate(result);UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("MEDICAL_MODEL_OK parts="+selected.Length);
    }
}
