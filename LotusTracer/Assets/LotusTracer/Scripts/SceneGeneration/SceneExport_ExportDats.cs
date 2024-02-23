using System.IO;
using CapyTracerCore.Core;
using UnityEditor;

public static class SceneExport_ExportDats
{
    public static void Export(SerializedScene scene, string sceneName)
    {
        using (FileStream fileStream = new FileStream(SceneExporter.SCENES_PATH_BASE + $"{sceneName}.dat", FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // write scene bounds
                scene.boundMin.WriteBinary(writer);
                scene.boundMax.WriteBinary(writer);
                
                // write camera
                scene.camera.WriteBinary(writer);
                
                // write each Material
                writer.Write(scene.materials.Length);
                foreach (SerializedMaterial renderMaterial in scene.materials)
                {
                    renderMaterial.WriteBinary(writer);
                }
                
                // write all triangleIndices
                writer.Write(scene.triangles.Length);
                foreach (var triangle in scene.triangles)
                {
                    triangle.WriteBinary(writer);
                }
                
                // write lights
                writer.Write(scene.lights.Length);
                
                foreach (var renderLight in scene.lights)
                {
                    renderLight.WriteBinary(writer);   
                }
                
                // ---
                writer.Write(scene.qtyBVHNodes);
                foreach (var bvhNode in scene.bvhNodes)
                {
                    bvhNode.WriteBinary(writer);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
