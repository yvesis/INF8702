using Common;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Projet_INF8702
{
    class Quad: RendererBase, I3Dobject
    {
        private static int instanceID = 0;
        Buffer vertexBuffer;
        Buffer indexBuffer;
        VertexBufferBinding vertexBinding;
        private RasterizerState skyBoxState;

        int totalVertexCount = 0;
        public DynamicCubeMap EnvironmentMap { get; set; }
        public Buffer PerMaterialBuffer { get; set; }

        public Buffer PerArmatureBuffer { get; set; }

        public Buffer PerObjectBuffer { get; set; }
        public Common.Mesh.Animation? CurrentAnimation { get; set; }

        public bool PlayOnce { get; set; }
        // Loaded mesh

        Common.Mesh mesh = new Mesh();
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
        public float ReflectionAmount { get; set; }

        public Quad()
        {
        }
        private void CreateVertexBinding()
        {
            var color = Color.White;
            var data = new[]
            {
                /*  Position: float x 3, Normal: Vector3, Color */
                new Vertex(new Vector3(-0.5f, 0.5f, -0.5f), Vector3.UnitZ, color),
                new Vertex(new Vector3(-0.5f, 0.5f, -0.5f), Vector3.UnitZ, color),
                new Vertex(new Vector3(0.5f,  0.5f, -0.5f), Vector3.UnitZ, color),
                new Vertex(new Vector3(0.5f,  0.5f, -0.5f), Vector3.UnitZ, color),

            };

            vertexBuffer = ToDispose(Buffer.Create(DeviceManager.Direct3DDevice, BindFlags.VertexBuffer, data));
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            // v0    v1
            // |-----|
            // | \ A |
            // | B \ |
            // |-----|
            // v3    v2
            indexBuffer = ToDispose(Buffer.Create(DeviceManager.Direct3DDevice, BindFlags.IndexBuffer, new ushort[] {
                2, 1, 0, // A
                0, 3, 2  // B
            }));
            PrimitiveCount = data.Length / 2;

            var max = new Vector3(0.5f, 0f, -0.5f);
            var min = new Vector3(-0.5f, 1f, -0.5f);
            var center = min + (max - min) * .5f;

            meshExtent = new Mesh.MeshExtent
            {
                Min = min,
                Max = max,
                Radius = 1f,
                Center = center
            };
        }
        protected override void CreateDeviceDependentResources()
        {
            // Call base implementation
            base.CreateDeviceDependentResources();
            // Remove first our own resources
            RemoveAndDispose(ref vertexBuffer);
            RemoveAndDispose(ref indexBuffer);
            RemoveAndDispose(ref skyBoxState);

            CreateVertexBinding();
            skyBoxState = ToDispose(new RasterizerState(DeviceManager.Direct3DDevice, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsFrontCounterClockwise = false
            }));
        }
        public int PrimitiveCount
        {
            get;
            private set;
        }
        public SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology
        {
            get { return SharpDX.Direct3D.PrimitiveTopology.TriangleList; }
        }
        protected override void DoRender()
        {
            DoRender(RenderContext);
        }
        protected override void DoRender(DeviceContext context)
        {
            var state = context.Rasterizer.State;
            context.Rasterizer.State = skyBoxState;

            // Tell the IA we are using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Set the index buffer
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);
            // Pass in the quad vertices (note: only 4 vertices)
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            // Draw the 6 vertices that make up the two triangles in the quad
            // using the vertex indices

            var perMaterial = new ConstantBuffers.PerMaterial
            {
                Ambient = Color.Black,
                Diffuse = Color.Black,
                Emissive = Color.Black,
                Specular = Color.Black,
                SpecularPower = 0,
                HasTexture = 0,
                UVTransform = Matrix.Identity
            };
            if (EnvironmentMap != null)
            {
                perMaterial.IsReflective = 1;
                perMaterial.ReflectionAmount = ReflectionAmount;
                context.PixelShader.SetShaderResource(1, EnvironmentMap.EnvMapSRV);
            }
            context.UpdateSubresource(ref perMaterial, PerMaterialBuffer);

            context.DrawIndexed(6, 0, 0);
            //context.Draw(12, 0);

            if (EnvironmentMap != null)
                context.PixelShader.SetShaderResource(1, null);

            context.Rasterizer.State = state;

        }

    }
}
