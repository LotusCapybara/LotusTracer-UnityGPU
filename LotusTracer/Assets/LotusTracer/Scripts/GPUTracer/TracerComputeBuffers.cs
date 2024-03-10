
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class BuffersNames
{
    public const string TRIANGLE_VERTICES = "_TriangleVertices";
    public const string TRIANGLE_DATAS = "_TriangleDatas";
    public const string BVH_TREE = "_AccelTree";
    public const string MATERIALS = "_Materials";
    public const string LIGHTS = "_Lights";
    
    public const string SCENE_BOUNDS = "_SceneBounds";
    
    public const string MAP_DATA_ALBEDO = "_MapDatasAlbedo";
    public const string MAP_DATA_NORMAL = "_MapDatasNormal";
    public const string MAP_DATA_ROUGHNESS = "_MapDatasRoughness";
    public const string MAP_DATA_METALLIC = "_MapDatasMetallic";
    public const string MAP_DATA_EMISSION = "_MapDatasEmission";
}

public class TracerComputeBuffers
{
    private readonly Dictionary<string, ComputeBuffer> _buffers;
    
    public TracerComputeBuffers(RenderScene renderScene)
    {
        _buffers = new Dictionary<string, ComputeBuffer>();
        
        // triangle vertices
        var bufferTriangleVertices = new ComputeBuffer(renderScene.sceneGeom.qtyTriangles, Marshal.SizeOf<RenderTriangle_Vertices>());
        bufferTriangleVertices.SetData(renderScene.sceneGeom.triangleVertices);
        _buffers.Add(BuffersNames.TRIANGLE_VERTICES, bufferTriangleVertices);
        
        // triangle datas
        var bufferTriangleDatas = new ComputeBuffer(renderScene.sceneGeom.qtyTriangles, Marshal.SizeOf<RenderTriangle_Data>());
        bufferTriangleDatas.SetData(renderScene.sceneGeom.triangleDatas);
        _buffers.Add(BuffersNames.TRIANGLE_DATAS, bufferTriangleDatas);
        
        // bvh tree
        var bufferBVH = new ComputeBuffer(renderScene.sceneGeom.qtyBVHNodes, Marshal.SizeOf<StackBVH4Node>());
        bufferBVH.SetData(renderScene.sceneGeom.bvhNodes);
        _buffers.Add(BuffersNames.BVH_TREE, bufferBVH);
        
        // materials
        var bufferMaterials = new ComputeBuffer(renderScene.sceneData.qtyMaterials, Marshal.SizeOf<SerializedMaterial>());
        bufferMaterials.SetData(renderScene.sceneData.materials);
        _buffers.Add(BuffersNames.MATERIALS, bufferMaterials);
        
        // lights
        var bufferLights = new ComputeBuffer(renderScene.sceneData.qtyLights, Marshal.SizeOf<RenderLight>());
        bufferLights.SetData(renderScene.sceneData.lights);
        _buffers.Add(BuffersNames.LIGHTS, bufferLights);
        
        // scene bounds
        var sceneBounds = new ComputeBuffer(1, Marshal.SizeOf<BoundsBox>());
        sceneBounds.SetData(new[]{ new BoundsBox(renderScene.sceneGeom.boundMin, renderScene.sceneGeom.boundMax)});
        _buffers.Add(BuffersNames.SCENE_BOUNDS, sceneBounds);
        
        // create all texture data buffers
        CreateAtlasDataBuffer(BuffersNames.MAP_DATA_ALBEDO, renderScene.textureDataAlbedo);
        CreateAtlasDataBuffer(BuffersNames.MAP_DATA_NORMAL, renderScene.textureDataNormal);
        CreateAtlasDataBuffer(BuffersNames.MAP_DATA_ROUGHNESS, renderScene.textureDataRoughness);
        CreateAtlasDataBuffer(BuffersNames.MAP_DATA_METALLIC, renderScene.textureDataMetallic);
        CreateAtlasDataBuffer(BuffersNames.MAP_DATA_EMISSION, renderScene.textureDataEmission);

    }

    public void CreateAtlasDataBuffer(string bufferName, TextureData[] textureDatas)
    {
        int len = math.max(textureDatas.Length, 1);
        if (textureDatas.Length == 0)
        {
            textureDatas = new TextureData[1] { new()}; 
        }
        
        var buffer = new ComputeBuffer(len, Marshal.SizeOf<TextureData>());
        buffer.SetData(textureDatas);
        _buffers.Add(bufferName, buffer);
    }
    
    public ComputeBuffer GetBuffer(string id)
    {
        return _buffers[id];
    }
    
    
    public void Dispose()
    {
        foreach (var kvp in _buffers)
        {
            kvp.Value?.Dispose();
        }
    }
}