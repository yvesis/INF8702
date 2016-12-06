using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using Common;

namespace Projet_INF8702
{
    class ShadowMap : RendererBase
    {
        private Texture2D texture;
        private DepthStencilView depthMap;
        private Viewport viewport;
        private ShaderResourceView depthSRV;
        private SamplerState sampler;
        private RasterizerState rasterizer;
        private VertexShader vShader;
        private RenderStatesManager DXstates = new RenderStatesManager();

        public ShaderResourceView DepthMapResource 
        {
            get { return depthSRV; }
        }
        public Size2 Size { get; private set; }
        public ShadowMap(uint width, uint height)
        {
            Size = new Size2((int)width, (int)height);
        }
        void Bind(DeviceContext context)
        {
            RenderTargetView rtv = null;
            context.OutputMerger.SetRenderTargets(depthMap, rtv);
            context.ClearDepthStencilView(depthMap, DepthStencilClearFlags.Depth, 1f, 0);
        }
        public void Update(DeviceContext context, Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DynamicCubeMap> renderScene)
        {
            //var rs = DXstates.Pop(context);
            //rs = null;
            Bind(context);
            var fov = (float)Math.PI/6.0f;
            var s = (float)(Math.Cos(fov) / Math.Sin(fov));
            var Q = 100f/(100f-0.1f);
            var projection = new Matrix(s, 0, 0, 0,
                                        0, s, 0, 0,
                                        0, 0, Q, 1,
                                        0, 0, -.1f * Q, 0);
            Matrix lightMat;
            CreateLightSpaceMatrix(out lightMat);
            renderScene(context, projection, lightMat, null, depthMap, null);

        }
        protected override void DoRender(DeviceContext context)
        {
            //base.DoRender(context);

            //var rs = new RenderState(depthMap, null, viewport, rasterizer, vShader, null, null);
            //var rs = DXstates.Pop(context);
            //rs = null;


        }
        protected override void DoRender()
        {
            throw new NotImplementedException();
        }
        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();
            RemoveAndDispose(ref depthMap);
            RemoveAndDispose(ref depthSRV);
            RemoveAndDispose(ref sampler);
            RemoveAndDispose(ref vShader);

            viewport = new Viewport(0, 0, Size.Width, Size.Height);
            viewport.MinDepth = .0f;
            viewport.MaxDepth = 1f;

            var textDesc = new Texture2DDescription
            {
                Width = Size.Width,
                Height = Size.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R24G8_Typeless,
                SampleDescription = new SharpDX.DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },

                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None

            };

            var device = DeviceManager.Direct3DDevice;
            texture = ToDispose(new Texture2D(device, textDesc));

            DepthStencilViewDescription desc = new DepthStencilViewDescription
            {
                Flags = DepthStencilViewFlags.None,
                Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                Dimension = DepthStencilViewDimension.Texture2D,
            };
            desc.Texture2D.MipSlice = 0;

            depthMap = ToDispose(new DepthStencilView(device, texture, desc));
            var descSRV = new ShaderResourceViewDescription
            {
                Format = SharpDX.DXGI.Format.R24_UNorm_X8_Typeless,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
            };
            descSRV.Texture2D.MipLevels = textDesc.MipLevels;
            descSRV.Texture2D.MostDetailedMip = 0;
            depthSRV = ToDispose(new ShaderResourceView(device, texture, descSRV));
            texture.Dispose();

            sampler = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new Color4(1),
                ComparisonFunction = Comparison.Less,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f

            }));

            //rasterizer = ToDispose(new RasterizerState(device, new RasterizerStateDescription
            //{
            //    //DepthBias
            //}));

            //var rs = new RenderState(depthMap, null, viewport, rasterizer, vShader, null, null);
            //DXstates.Push(rs);
        }
        private void CreateLightSpaceMatrix(out Matrix LMat)
        {
            var L = this.lightDir;
            var rnd = new Random();
            var min = new Vector3(-1);
            var max = new Vector3(1);

            var rv = Vector3.Up;
            do
            {
                var v = rnd.NextVector3(min, max);
                rv = Vector3.Cross(L, v);
            }
            while (rv.LengthSquared() == 0);

            rv.Normalize();
            var R = Vector3.Cross(L, rv);
            var U = Vector3.Cross(L, R);
            var D = L;

            LMat = new Matrix(R.X, R.Y, R.Z, 0f,
                                  U.X, U.Y, U.Z, 0f,
                                  D.X, D.Y, D.Z, 0f,
                                  0f,  0f,  0f,  0f);

            var P = Vector3.Transform(L, LMat);

            LMat = new Matrix(R.X, R.Y, R.Z, 0f,
                              U.X, U.Y, U.Z, 0f,
                              D.X, D.Y, D.Z, 0f,
                              P.X, P.Y, P.Z, 0f);
        }
        Vector3 lightDir = Vector3.Zero;
        public void SetLightDirection(Vector3 light)
        {
            light.Normalize();
            this.lightDir = light; 
        }
        public static implicit operator bool(ShadowMap s)
        {
            return s != null;
        }
    }
}
