using Common;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;
namespace Projet_INF8702
{
    class SkyBox : Common.RendererBase, I3Dobject
    {
        Buffer indexBuffer;
        ShaderResourceView textureCube;
        SamplerState sampler;
        private RasterizerState skyBoxState;
        private Buffer perSkyBox;
        // Vertex buffer
        protected Buffer buffer_;
        // Binding structure to the vertex buffer
        private VertexBufferBinding vertexBinding_;

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
        private DepthStencilState depthStencilState;
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
        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            RemoveAndDispose(ref textureCube);
            RemoveAndDispose(ref sampler);
            RemoveAndDispose(ref skyBoxState);
            RemoveAndDispose(ref perSkyBox);
            RemoveAndDispose(ref depthStencilState);
            // Compile and create vs shader 
            var device = DeviceManager.Direct3DDevice;

            textureCube = ToDispose(ShaderResourceView.FromFile(device, "Textures/2.dds"));
            sampler = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Less,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f

            }));
            World = Matrix.Scaling(1f);

            perSkyBox = ToDispose(new Buffer(DeviceManager.Direct3DDevice, Utilities.SizeOf<ConstantBuffers.DrawSkyBox>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            skyBoxState = ToDispose(new RasterizerState(DeviceManager.Direct3DDevice, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsFrontCounterClockwise = false
            }));


            depthStencilState = ToDispose(new DepthStencilState(device, new DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                DepthComparison = Comparison.LessEqual,
                DepthWriteMask = DepthWriteMask.All,
                IsStencilEnabled = false,
                StencilReadMask = 0xff, // no mask
                StencilWriteMask = 0xff,
                // Face culling
                FrontFace = new DepthStencilOperationDescription
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment,
                    FailOperation = StencilOperation.Keep
                },

                BackFace = new DepthStencilOperationDescription
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement,
                    FailOperation = StencilOperation.Keep
                },


            }));
        }
        protected override void CreateSizeDependentResources()
        {
            base.CreateSizeDependentResources();

            // Dispose before creating
            RemoveAndDispose(ref buffer_);

            // Create buffer and binding
            CreateVertexBinding();

        }
        private void CreateVertexBinding()
        {
            RemoveAndDispose(ref indexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;
            var color = Color.SaddleBrown;
            var data = new Vertex[] {
                    /*  Vertex Position    Color */
            new Vertex(new Vector3(-1f, 1f, -.5f), color),  // 0-Top-left
            new Vertex(new Vector3(1f,  1f, -.5f), color),  // 1-Top-right
            new Vertex(new Vector3(1f, -1f, -.5f), color), // 2-Base-right
            new Vertex(new Vector3(-1f, -1f,-.5f), color), // 3-Base-left

            new Vertex(new Vector3(-1f, 1f, .5f),  color),  // 4-Top-left
            new Vertex(new Vector3(1f,  1f, .5f),  color),  // 5-Top-right
            new Vertex(new Vector3(1f, -1f, .5f),  color),  // 6-Base-right
            new Vertex(new Vector3(-1f, -1f,.5f),  color),  // 7-Base-left
            };

            // Create vertex buffer for cube
            buffer_ = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, data));
            vertexBinding_ = new VertexBufferBinding(buffer_, Utilities.SizeOf<Vertex>(), 0);

            // Front    Right    Top      Back     Left     Bottom  
            // v0    v1 v1    v5 v1    v0 v5    v4 v4    v0 v3    v2
            // |-----|  |-----|  |-----|  |-----|  |-----|  |-----|
            // | \ A |  | \ A |  | \ A |  | \ A |  | \ A |  | \ A |
            // | B \ |  | B \ |  | B \ |  | B \ |  | B \ |  | B \ |
            // |-----|  |-----|  |-----|  |-----|  |-----|  |-----|
            // v3    v2 v2    v6 v5    v4 v6    v7 v7    v3 v7    v6
            indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, new ushort[] {
                0, 1, 2, // Front A
                0, 2, 3, // Front B
                1, 5, 6, // Right A
                1, 6, 2, // Right B
                1, 0, 4, // Top A
                1, 4, 5, // Top B
                5, 4, 7, // Back A
                5, 7, 6, // Back B
                4, 0, 3, // Left A
                4, 3, 7, // Left B
                3, 2, 6, // Bottom A
                3, 6, 7, // Bottom B
            }));
            //indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, new ushort[] {
            //    2, 1, 0, // Front A
            //    0, 3, 2, // Front B
            //    6, 5, 1, // Right A
            //    1, 2, 6, // Right B
            //    4, 0, 1, // Top A
            //    1, 5, 4, // Top B
            //    7, 4, 5, // Back A
            //    5, 6, 7, // Back B
            //    3, 0, 4, // Left A
            //    4, 7, 3, // Left B
            //    6, 2, 3, // Bottom A
            //    3, 7, 6, // Bottom B
            //}));
            PrimitiveCount = Utilities.SizeOf<Vertex>();

            var max = data.Max().Position;
            var min = data.Min().Position;
            var center = (max - min) * .5f;

            meshExtent = new Mesh.MeshExtent
            {
                Min = min,
                Max = max,
                Radius = 0.5f,
                Center = center
            };
        }
        private int PrimitiveCount
        {
            get;
            set;
        }
        private SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology
        {
            get { return SharpDX.Direct3D.PrimitiveTopology.TriangleList; }
        }
        protected override void DoRender()
        {
            DoRender(RenderContext);
        }
        public static bool EnableSkyBoxState;
        protected override void DoRender(DeviceContext context)
        {
            RasterizerState state = null;
            DepthStencilState depthState = null;
            var enableState = EnableSkyBoxState;
            if (enableState)
            {
                state = context.Rasterizer.State;
                depthState = context.OutputMerger.DepthStencilState;
                context.Rasterizer.State = skyBoxState;
                context.OutputMerger.SetDepthStencilState(depthStencilState);
            }
            else return;
            var pos = Vector3.Transform(Vector3.Zero, Matrix.Invert(Scene.View));
            var translation = new Vector3(pos.X, pos.Y, pos.Z);
            Debug.WriteLine(translation);
            var W = Matrix.Scaling(256);

            var perObject = new ConstantBuffers.PerObject
            {
                WorldViewProjection = W * Scene.ViewProjection,
                World = W,
                WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(W)),
            };

            perObject.Transpose();
            context.UpdateSubresource(ref perObject, PerObjectBuffer);

            context.PixelShader.SetShaderResource(10, textureCube);
            context.PixelShader.SetSampler(0, sampler);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology;
            context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);

            context.VertexShader.SetConstantBuffer(4, perSkyBox);
            context.PixelShader.SetConstantBuffer(4, perSkyBox);

            var drawSkybox = new ConstantBuffers.DrawSkyBox { On = 1 };
            context.UpdateSubresource(ref drawSkybox, perSkyBox);

            context.DrawIndexed(36, 0, 0);

            drawSkybox.On = 0;
            context.UpdateSubresource(ref drawSkybox, perSkyBox);
            if (enableState)
            {
                context.Rasterizer.State = state;
                context.OutputMerger.SetDepthStencilState(depthState);
            }

        }
    }

}
