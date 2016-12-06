using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projet_INF8702
{
    class RenderState
    {
        public readonly DepthStencilView DepthStencilView;
        public readonly RenderTargetView RenderTargetView;
        public readonly Viewport? Viewport;
        public readonly RasterizerState RasterizerState;
        public readonly VertexShader VertexShader;
        public readonly PixelShader PixelShader;
        public readonly GeometryShader GeometryShader;

        public RenderState(DepthStencilView DepthStencilView, RenderTargetView RenderTargetView,
            Viewport? Viewport, RasterizerState RasterizerState, VertexShader VertexShader,
            PixelShader PixelShader, GeometryShader GeometryShader)
        {
            this.DepthStencilView = DepthStencilView;
            this.RenderTargetView = RenderTargetView;
            this.Viewport = Viewport;
            this.RasterizerState = RasterizerState;
            this.VertexShader = VertexShader;
            this.PixelShader = PixelShader;
            this.GeometryShader = GeometryShader;
        }
    }
    class RenderStatesManager:Stack<RenderState>
    {
        public RenderState Pop( DeviceContext context = null)
        {

            var rs = Count == 1 ? Peek() : Pop();
            if(context != null)
            {
                //if (rs.VertexShader != null)
                    context.VertexShader.Set(rs.VertexShader);

                //if (rs.PixelShader != null)
                    context.PixelShader.Set(rs.PixelShader);

                //if (rs.GeometryShader != null)
                    context.GeometryShader.Set(rs.GeometryShader);

                //if (rs.RasterizerState != null)
                {
                    context.Rasterizer.State = rs.RasterizerState;
                    if (rs.Viewport.HasValue)
                        context.Rasterizer.SetViewport(rs.Viewport.Value);
                }

                //if (rs.RenderTargetView != null && rs.DepthStencilView != null)
                    context.OutputMerger.SetRenderTargets(rs.DepthStencilView, rs.RenderTargetView);
            }
            return rs;
        }
    }
}
