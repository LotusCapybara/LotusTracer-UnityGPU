﻿
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class BuffersNames
{
    public const string CAMERA_RAYS = "_CameraRays";
    public const string TRIANGLES = "_Triangles";
    public const string BVH_TREE = "_AccelTree";
    public const string MATERIALS = "_Materials";
    public const string LIGHTS = "_Lights";
    
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
        
        // camera rays
        var bufferCameraRays = new ComputeBuffer(renderScene.totalPixels, Marshal.SizeOf<RenderRay>() );
        bufferCameraRays.SetData(renderScene.cameraRays);
        _buffers.Add(BuffersNames.CAMERA_RAYS, bufferCameraRays);
        
        // fast triangleIndices
        var bufferTriangles = new ComputeBuffer(renderScene.serializedScene.qtyTriangles, Marshal.SizeOf<FastTriangle>());
        bufferTriangles.SetData(renderScene.serializedScene.triangles);
        _buffers.Add(BuffersNames.TRIANGLES, bufferTriangles);
        
        // bvh tree
        var bufferBVH = new ComputeBuffer(renderScene.serializedScene.qtyBVHNodes, Marshal.SizeOf<StackBVH4Node>());
        bufferBVH.SetData(renderScene.serializedScene.bvhNodes);
        _buffers.Add(BuffersNames.BVH_TREE, bufferBVH);
        
        // materials
        var bufferMaterials = new ComputeBuffer(renderScene.serializedScene.qtyMaterials, Marshal.SizeOf<SerializedMaterial>());
        bufferMaterials.SetData(renderScene.serializedScene.materials);
        _buffers.Add(BuffersNames.MATERIALS, bufferMaterials);
        
        // lights
        var bufferLights = new ComputeBuffer(renderScene.serializedScene.qtyLights, Marshal.SizeOf<RenderLight>());
        bufferLights.SetData(renderScene.serializedScene.lights);
        _buffers.Add(BuffersNames.LIGHTS, bufferLights);
        
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