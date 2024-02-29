<!--
    This file is a part of Lotus Path Tracer open source project.
    Copyright (c) 2024+ by Leo Rinato (aka Ariel Arias or Lotus) - All rights reserved.

    This software was written for educational purposes and uses the MIT license.

    Feel free to reach me out at leo.rinato@gmail.com
-->

# LOTUS PATH TRACER

## Images
Example images. Assets were taken from Sketchfab, please follow these amazing creators.

![Image](https://i.gyazo.com/47511db2c23eed9a0bf2cece43392842.jpg)
Asset courtesy of [Adrian Carter](https://sketchfab.com/Adrian.Carter3D).

![Image](https://i.gyazo.com/01231894434f969b6f34f662abb5454f.jpg)
Asset courtesy of [JuanG3D](https://sketchfab.com/juang3d).


## About

Lotus Pathtracer is a personal project path-tracing renderer. It's made inside Unity and utilizes GPU compute shaders for the path tracer itself, and C# for surrounding things like User Experience, BVH generation, scene serialization and so on.
It is insipired by multiple existing open source projects, books and tutorials.

This is a solo and just-for-fun project so many features are simplified compared with professional grade solutions.

I plan to create some blog or website at some point, but it really depends on how much free time I have (which is not much!).

## Disclaimer not stable project

Since this is a just-for-fun project I would never consider using it for any actual development. I might also change the code drastically all the time since it's more like a playground to expriment for me, but I decided to still share it as open source.


## Features

  - Compute Shader Integrator (direct lighting, path tracing, etc)
  - Custom serialized scene format (written in C#)
  - Spatial acceleration structure. (BVH)
  - BXDF:
    - Diffuse: Lambert / Oran Nayar, pending Disney
    - Reflection and other: Microfacet GGX, pending others     
  - Volumetric/Subsurface Scattering but it's clunky and I hope to improve it soon.

## Planned Features or Improvements
 - More lighting model implemetations 
 - Better volumetric scattering
 - Post Processing support: bloom, color grading, etc
 - dof 
 - denoising
 - more and better direct light support
 - some other stuff since I'm experimenting with this now and then