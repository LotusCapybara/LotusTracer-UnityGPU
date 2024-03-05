using System.IO;
using CapyTracerCore.Core;
using UnityEditor;

public static class SceneExport_ExportDats
{
    public static void Export(string sceneName, bool ignoreCreateGeometry)
    {
        SerializedScene_Data sceneData = SceneExporter.s_sceneData;
        SerializedScene_Geometry sceneGeom = SceneExporter.s_sceneGeom;

        string pathData = SceneExporter.SCENES_PATH_BASE + $"{sceneName}.dat";
        string pathGeom = SceneExporter.SCENES_PATH_BASE + $"{sceneName}.geom";

        if (!ignoreCreateGeometry)
        {
            using (FileStream fileStream = new FileStream(pathGeom, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    // write scene bounds
                    sceneGeom.boundMin.WriteBinary(writer);
                    sceneGeom.boundMax.WriteBinary(writer);
                
                    // write all triangleIndices
                    writer.Write(SceneExport_GatherTriangles.s_gatheredTriangles.Length);
                    foreach (var triangle in SceneExport_GatherTriangles.s_gatheredTriangles)
                    {
                        triangle.WriteBinary(writer);
                    }
                
                    // ---
                    writer.Write(sceneGeom.qtyBVHNodes);
                    foreach (var bvhNode in sceneGeom.bvhNodes)
                    {
                        bvhNode.WriteBinary(writer);
                    }
                }
            }
        }

        using (FileStream fileStream = new FileStream(pathData, FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // write camera
                sceneData.camera.WriteBinary(writer);
                
                // write each Material
                writer.Write(sceneData.materials.Length);
                foreach (SerializedMaterial renderMaterial in sceneData.materials)
                {
                    renderMaterial.WriteBinary(writer);
                }

                
                // write lights
                writer.Write(sceneData.lights.Length);
                
                foreach (var renderLight in sceneData.lights)
                {
                    renderLight.WriteBinary(writer);   
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
