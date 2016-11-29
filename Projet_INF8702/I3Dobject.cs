using Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Projet_INF8702
{
    public struct MeshExtent
    {
        public Vector3 Min;
        public Vector3 Max;
        public Vector3 Center;
        public float Radius;

        public static implicit operator MeshExtent(Mesh.MeshExtent e)
        {
            return new MeshExtent
            {
                Min = e.Min,
                Max = e.Max,
                Center = e.Center,
                Radius = e.Radius
            };
        }
    }
    public interface I3Dobject: IDisposable
    {
        Common.Mesh Mesh { get; }
        MeshExtent MeshExtent { get; }
        DynamicCubeMap EnvironmentMap { get; set; }
        System.Diagnostics.Stopwatch Clock
        {
            get;
            set;
        }
        Common.Mesh.Animation? CurrentAnimation { get; set; }
        bool PlayOnce { get; set; }
        Buffer PerMaterialBuffer { get; set; }
        Buffer PerArmatureBuffer { get; set; }
        Buffer PerObjectBuffer { get; set; }
        Matrix World { get; set; }
        void Initialize(D3DApplicationBase app);
        void Render();
        void Render(SharpDX.Direct3D11.DeviceContext device);
    }
}
