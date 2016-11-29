// Copyright (c) 2013 Justin Stenning
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Projet_INF8702
{

    public class SphereRenderer : RendererBase, I3Dobject
    {
        private static int instanceID = 0;
        Buffer vertexBuffer;
        Buffer indexBuffer;
        VertexBufferBinding vertexBinding;

        int totalVertexCount = 0;
        public DynamicCubeMap EnvironmentMap { get; set; }
        public Buffer PerMaterialBuffer { get; set; }

        public Buffer PerArmatureBuffer { get; set; }

        public Buffer PerObjectBuffer { get; set; }
        public Common.Mesh.Animation? CurrentAnimation { get; set; }

        public bool PlayOnce { get; set; }
        // Loaded mesh

        Common.Mesh mesh;
        public Common.Mesh Mesh { get { return mesh; } }
        MeshExtent meshExtent;
        public MeshExtent MeshExtent { get { return meshExtent; } }

        // Create and allow access to a timer
        System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        public System.Diagnostics.Stopwatch Clock
        {
            get { return clock; }
            set { clock = value; }
        }
        Matrix I3Dobject.World
        {
            get { return World; }
            set { World = value; }
        }
        private int ID;
        private float reflectionAmount = .1f;
        public SphereRenderer()
        {
            color = Color.Gray;
            mesh = new Mesh();
            ID = instanceID;
        }
        Color color;
        public SphereRenderer(Color color):base()
        {
            this.color = color;
            var rnd = new Random();
            reflectionAmount =  (float)Math.Min(reflectionAmount + rnd.NextDouble(0, 1.0), .40);
            ID = ++instanceID;
            if (ID == 2) reflectionAmount = 1f;
            //if (ID == 3) reflectionAmount = 0f;

        }
   

    
        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref vertexBuffer);
            RemoveAndDispose(ref indexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            Vertex[] vertices;
            int[] indices;
            //GeometricPrimitives.GenerateSphere(out vertices, out indices, color);
            GeometricPrimitives.GenerateSphere(out vertices, out indices, color, 0.5f,64, false);

            vertexBuffer = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices));
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, indices));
            totalVertexCount = indices.Length;

            var max = vertices.Max().Position;
            var min = vertices.Min().Position;
            var center = (max - min) * .5f;

            meshExtent = new Mesh.MeshExtent
            {
                Min = min,
                Max = max,
                Radius = 0.5f,
                Center = center
            };
        }
        private void InitExtents()
        {
            
        }
        protected override void DoRender()
        {
            DoRender(RenderContext);
        }
        float time = .016f;
        protected override void DoRender(DeviceContext context)
        {

            // Tell the IA we are using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Set the index buffer
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            // Pass in the quad vertices (note: only 4 vertices)
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            // Draw the 36 vertices that make up the two triangles in the quad
            // using the vertex indices

            var perObject = new ConstantBuffers.PerObject();
            var angle = Math.PI * 2 * time * (ID % 2); // move only sphere with even IDs
            if (angle >= 2 * Math.PI) angle = 0;
            time += 0.016f / 30f;
            if (time >= 1f) time = 0;
            perObject.World = /*Matrix.RotationY((float)angle) */World*Matrix.RotationY((float)angle);// *Scene.Model;
            perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
            perObject.WorldViewProjection = perObject.World * Scene.ViewProjection;
            perObject.Transpose();
            context.UpdateSubresource(ref perObject, PerObjectBuffer);

            var perMaterial = new ConstantBuffers.PerMaterial
            {
                Ambient = Color.SaddleBrown,
                Diffuse = Color.White,
                Emissive = Color.Black,
                Specular = Color.White,
                SpecularPower = 100f,
                HasTexture = 0,
                UVTransform = Matrix.Identity
            };
            if (EnvironmentMap != null)
            {
                perMaterial.IsReflective = 1;
                perMaterial.ReflectionAmount = reflectionAmount;
                context.PixelShader.SetShaderResource(1,EnvironmentMap.EnvMapSRV);
            }

            context.UpdateSubresource(ref perMaterial, PerMaterialBuffer);
            context.DrawIndexed(totalVertexCount, 0, 0);
            if (EnvironmentMap != null)
                context.PixelShader.SetShaderResource(1, null);				

            // Note: we have called DrawIndexed so that the index buffer will be used
        }
    }

}