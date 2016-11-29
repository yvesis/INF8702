﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using SharpDX;

namespace Projet_INF8702
{
    public static class ConstantBuffers
    {
        /// <summary>
        /// Per Object constant buffer (matrices)
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PerObject
        {
            // WorldViewProjection matrix
            public Matrix WorldViewProjection;

            // We need the world matrix so that we can
            // calculate the lighting in world space
            public Matrix World;

            // Inverse transpose of World
            public Matrix WorldInverseTranspose;

            /// <summary>
            /// Transpose the matrices so that they are in row major order for HLSL
            /// </summary>
            internal void Transpose()
            {
                this.World.Transpose();
                this.WorldInverseTranspose.Transpose();
                this.WorldViewProjection.Transpose();
            }
        }

        /// <summary>
        /// Directional light
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DirectionalLight
        {
            public SharpDX.Color4 Color;
            public SharpDX.Vector3 Direction;
            float _padding0;
        }

        /// <summary>
        /// Per frame constant buffer (camera position)
        /// </summary>        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerFrame
        {
            public DirectionalLight Light;
            public SharpDX.Vector3 CameraPosition;
            float _padding0;
        }

        /// <summary>
        /// Per material constant buffer
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerMaterial
        {
            public Color4 Ambient;
            public Color4 Diffuse;
            public Color4 Specular;
            public float SpecularPower;
            public uint HasTexture;     // Does the current material have a texture (0 false, 1 true)
            public uint IsReflective;   // Is the current material reflective (0 false, 1 true)
            public float ReflectionAmount; // If reflective, how much?
            public Color4 Emissive;
            public Matrix UVTransform;
        }

        /// <summary>
        /// Per armature/skeleton constant buffer
        /// </summary>
        public class PerArmature
        {
            // The maximum number of bones supported
            public const int MaxBones = 1024;
            public Matrix[] Bones;

            public PerArmature()
            {
                Bones = new Matrix[MaxBones];
            }

            public static int Size()
            {
                return Utilities.SizeOf<Matrix>() * MaxBones;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DrawSkyBox
        {
            public uint On;
            Vector3 padding0_;
        }
    }
}
