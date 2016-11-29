using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace LoadMeshes.Application
{
    class Scene:Common.RendererBase
    {
        public static Matrix View { get; set; }
        public static Matrix ViewProjection { get; set; }
        public static Matrix Model { get; set; }
        public static Vector3 CameraPosition { get; set; }


        public static Buffer PerObjectBuffer { get; set; }
        public static Buffer PerFrameBuffer { get; set; }
        public static Buffer PerMaterialBuffer { get; set; }

        private List<Common.RendererBase> renderers = new List<Common.RendererBase>();

        private RasterizerState normalState;

        public static List<AbstractLight> Lights { get; private set; }
        public Scene()
        {
            View = ViewProjection = Model = Matrix.Identity;
            Lights = new List<AbstractLight>();
            Lights.Add(new DirectionalLight { Direction = new Vector3(1f, -1f, -1f), Color = Color.Gray, IsOn= true });
            Lights.Add(new PointLight { Direction = new Vector3(1f, -1f, -1f), Position = new Vector3(), Color = Color.Gray, IsOn=true });
            Lights.Add(new SpotLight { Direction = new Vector3(1f, -1f, -1f), Position = new Vector3(), SpotDirection = new Vector3(), SpotExponent = 0.0f, Color = Color.Gold, IsOn= true });

        }
        public override void Initialize(Common.D3DApplicationBase app)
        {
            base.Initialize(app);
            foreach (var obj in Objects)
            {
                obj.Initialize(app);
            }
        }
        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();
            RemoveAndDispose(ref normalState);

            normalState = ToDispose(new RasterizerState(DeviceManager.Direct3DDevice, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = false
            }));


        }
        public void AddObject(Common.RendererBase renderer)
        {
            renderers.Add(renderer);
        }
        public void AddObjects(IEnumerable<Common.RendererBase> renderers)
        {
            this.renderers.AddRange(renderers);
        }

        public void RemoveObject(Common.RendererBase renderer)
        {
            renderers.Remove(renderer);
        }
        public void Clear()
        {
            renderers.Clear();
        }
        public IEnumerable<Common.RendererBase> Objects
        {
            get
            {
                foreach (var obj in renderers)
                    yield return obj;
            }
        }
        protected override void DoRender()
        {
            DoRender(this.RenderContext);
        }
        protected override void DoRender(DeviceContext context)
        {
            UpdateScene(context);
            CoRoutine.StartCoRoutine(RenderRoutine(context));
            context.Rasterizer.State = normalState;
        }
        private IEnumerator RenderRoutine(DeviceContext context)
        {
            foreach(var obj in Objects)
            {
                obj.Render(context);
                yield return null;
            }
            
        }
        private void UpdateScene(DeviceContext context)
        {
            var perFrame = new ConstantBuffer.PerFrame {

                CameraPosition = Scene.CameraPosition
            };
            LightScene(ref perFrame);
            context.UpdateSubresource(ref perFrame, PerFrameBuffer);
        }
        private void LightScene(ref ConstantBuffer.PerFrame perFrame )
        {
            var dirLight = Lights.Where(l => l is DirectionalLight).FirstOrDefault();
            var pointLight = Lights.Where(l => l is PointLight).FirstOrDefault();
            var spotLight = Lights.Where(l => l is SpotLight).FirstOrDefault();

            var ligthMat = Matrix.RotationY(360 * Scene.Time / 1000);
            var lightDir0 = Vector3.Transform(dirLight.Direction, Scene.Model * (dirLight.IsDynamic ? ligthMat : Matrix.Identity));
            var lightDir1 = Vector3.Transform(pointLight.Direction, Scene.Model * (pointLight.IsDynamic ? ligthMat : Matrix.Identity));
            var lightDir2 = Vector3.Transform(spotLight.Direction, Scene.Model * (spotLight.IsDynamic ? ligthMat : Matrix.Identity));

            perFrame.Light0.Direction = new Vector3(lightDir0.X, lightDir0.Y, lightDir0.Z);
            perFrame.Light1.Direction = new Vector3(lightDir1.X, lightDir1.Y, lightDir1.Z);
            perFrame.Light2.Direction = new Vector3(lightDir2.X, lightDir2.Y, lightDir2.Z);

            perFrame.Light0.On = dirLight.IsOn ? 1u : 0;
            perFrame.Light1.On = pointLight.IsOn ? 1u : 0;
            perFrame.Light2.On = spotLight.IsOn ? 1u : 0;

            perFrame.Light0.Color = dirLight.Color;
            perFrame.Light1.Color = pointLight.Color;
            perFrame.Light2.Color = spotLight.Color;


        }

        public static float Time { get; set; }

        public static Matrix Projection { get; set; }
    }

    public class AbstractLight
    {
        public virtual Vector3 Direction
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public virtual Vector3 Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public virtual Vector3 SpotDirection
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public virtual float SpotExponent
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public Color Color { get; set; }
        public bool IsOn { get; set; }
        public bool IsDynamic { get; set; }
        protected AbstractLight()
        {
            Color = Color.White;
            IsOn = false;
            IsDynamic = false;
        }
    }
    public class DirectionalLight: AbstractLight
    {
        public override Vector3 Direction
        {
            get;
            set;
        }
    }
    public class SpotLight : AbstractLight
    {
        public override Vector3 Direction
        {
            get;
            set;
        }

        public override Vector3 Position
        {
            get;
            set;
        }

        public override Vector3 SpotDirection
        {
            get;
            set;
        }

        public override float SpotExponent
        {
            get;
            set;
        }
    }

    public class PointLight: AbstractLight
    {
        public override Vector3 Direction
        {
            get;
            set;
        }

        public override Vector3 Position
        {
            get;
            set;
        }

 
    }
}
