using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SerializedCamera
    {
        public float3 position;
        public float3 forward;
        public float3 up;
        public float3 right;
        public float fov;
        public float horizontalSize;
    }
}