using System;
using CapyTracerCore.Core;
using Unity.Mathematics;

namespace CapyTracerCore.Tracer
{
    public class RenderCamera
    {
        public float3 position;
        public float3 forward;
        public float3 right;
        public float3 up;
        public float horizontalSize;
        public float fov;

        private float _botLeftCorner;
        private float _xPixelSize;
        private float _yPixelSize;
        private float _aspectRatio;
        private readonly int _width;
        private readonly int _height;

        public RenderCamera(int w, int h, SerializedCamera serializedCamera)
        {
            _width = w;
            _height = h;
            position = serializedCamera.position;
            forward = serializedCamera.forward;
            right = serializedCamera.right;
            up = serializedCamera.up;
            horizontalSize = serializedCamera.horizontalSize;
            fov = serializedCamera.fov;
            
            _aspectRatio = (float) _width / _height;

            float verticalSize = horizontalSize * _aspectRatio;

            _xPixelSize = horizontalSize / _width;
            _yPixelSize = verticalSize / _height;
        }

        // a good resource to check how to generate rays from camera pixels
        // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-generating-camera-rays/generating-camera-rays.html
        public RenderRay GetRay(int x, int y)
        {
            // Compute the normalized screen coordinates u and v, ranging from -1 to 1
            float u = (2f * x / _width - 1f) * _aspectRatio;
            float v = 1f - 2f * y / _height;

            // Calculate the tangent of the half field of view
            float tanHalfFov = (float)Math.Tan(fov * Math.PI / 180f * 0.5f);

            // Compute the direction of the ray
            float3 direction = forward + right * u * tanHalfFov - up * v * tanHalfFov;

            // Normalize the direction
            direction = math.normalize(direction);

            // Create the ray with the camera's position as the origin and the computed direction
            RenderRay ray = new RenderRay
            {
                origin = position,
                direction = direction
            };

            return ray;
        }
    }
}