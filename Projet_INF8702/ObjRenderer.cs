using Common;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Projet_INF8702
{
    public class ObjRenderer: Common.RendererBase, I3Dobject
    {
        struct Face
        {
            public readonly uint PosId;
            public readonly uint TexId;
            public readonly uint NormId;
            public readonly string[] Data;

            public Face(string f)
            {
                Data = f.Split('/');
                uint.TryParse(Data[0], out PosId);
                uint.TryParse(Data[1], out TexId);
                uint.TryParse(Data[2], out NormId);

            }
            public static void Parse(string[] data, ref List<Face> faces)
            {
                foreach(var d in data)
                {
                    if (d.Length >= 3)
                        faces.Add(new Face(d));
                }
            }
        }
        // Vertex buffer
        List<Buffer> vertexBuffers = new List<Buffer>();
        // Index Buffer
        List<Buffer> indexBuffers = new List<Buffer>();
        VertexBufferBinding vertexBinding_;

        private List<float[]> vertices = new List<float[]>();
        private List<float[]> normals = new List<float[]>();
        private List<float[]> texCoords = new List<float[]>();
        private List<ushort> indices = new List<ushort>();
        private List<Face> faces = new List<Face>();

        public readonly string FileName;
        private RasterizerState frontState;

        public DynamicCubeMap EnvironmentMap { get; set; }
        public Buffer PerMaterialBuffer { get; set; }

        public Buffer PerArmatureBuffer { get; set; }

        public Buffer PerObjectBuffer { get; set; }
        public Common.Mesh.Animation? CurrentAnimation { get; set; }

        public bool PlayOnce { get; set; }
        // Loaded mesh

        Common.Mesh mesh = new Common.Mesh();
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

        public ObjRenderer(string filename)
        {
            this.FileName = filename;
        }
        public override void Initialize(Common.D3DApplicationBase app)
        {
            ParseObjFile();
            base.Initialize(app);

        }
        private void ParseObjFile()
        {
            using (var sr = new StreamReader(FileName))
            {
                var culture = new CultureInfo("en-US");
                while (sr.Peek() > -1)
                {
                    string line = sr.ReadLine();
                    var strData = line.Split(new[] { "v ", "vt ", "vn " , " "}, StringSplitOptions.RemoveEmptyEntries);
                    var data = strData.Select(s =>
                    {

                        float v = 0;
                        float.TryParse(s, NumberStyles.AllowLeadingSign| NumberStyles.AllowDecimalPoint, culture, out v);
                        return v;

                    }).ToList();
                    //data.RemoveAt(0);
                    //Vertex vert = new Vertex();

                    if (line.StartsWith("vn"))
                        normals.Add(data.ToArray());
                    else if (line.StartsWith("vt"))
                        texCoords.Add(data.Take(2).ToArray());
                    else if (line.StartsWith("v "))
                        vertices.Add(data.ToArray());
                    else if (line.StartsWith("f "))
                    {
                        Face.Parse(strData, ref faces);

                    }

                }
            }
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();
            // release resources
            vertexBuffers.ForEach(b => RemoveAndDispose(ref b));
            vertexBuffers.Clear();
            indexBuffers.ForEach(b => RemoveAndDispose(ref b));
            indexBuffers.Clear();

            RemoveAndDispose(ref frontState);

            var device = DeviceManager.Direct3DDevice;
            // Create the vertex buffers
            Vertex[] verts = new Vertex[faces.Count];

            for (int i = 0; i < faces.Count; i++)
            {
                // create vertex
                var f = faces[i];
                verts[i] = new Vertex(new Vector3(vertices[(int)f.PosId-1]), new Vector3(normals[(int)f.NormId-1]), Color.Gray);
                indices.Add((ushort)f.PosId);

            }
            vertexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, verts.ToArray())));
            vertexBinding_ = new VertexBufferBinding(vertexBuffers.First(), Utilities.SizeOf<Vertex>(), 0);
             
            // Create the index buffers
            indexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, indices.ToArray())));

            frontState = ToDispose(new RasterizerState(DeviceManager.Direct3DDevice, new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Front,
                IsFrontCounterClockwise = false
            }));

            var max = verts.Max().Position;
            var min = verts.Min().Position;
            var center = (max + min) * .5f;

            meshExtent = new Mesh.MeshExtent
            {
                Min = min,
                Max = max,
                Radius = 0.5f,
                Center = center
            };

        }
        protected override void DoRender()
        {
            DoRender(RenderContext);
        }
        protected override void DoRender(DeviceContext context)
        {
            var state = context.Rasterizer.State;
            context.Rasterizer.State = frontState;

            //var perObject = new ConstantBuffers.PerObject();
            //perObject.M = World * Scene.Model;
            //perObject.N = Matrix.Transpose(Matrix.Invert(perObject.M));
            //perObject.MVP = perObject.M * Scene.ViewProjection;
            //perObject.Transpose();
            //context.UpdateSubresource(ref perObject, Scene.PerObjectBuffer);
            var perMaterial = new ConstantBuffers.PerMaterial
            {
                Ambient = Color.Gray,
                Diffuse = Color.Gray,
                Emissive = Color.Black,
                Specular = Color.Gray,
                SpecularPower = 10f,
                HasTexture = 0,
                UVTransform = Matrix.Identity
            };
            if (EnvironmentMap != null)
            {
                perMaterial.IsReflective = 1;
                perMaterial.ReflectionAmount = 0.4f;
                context.PixelShader.SetShaderResource(1, EnvironmentMap.EnvMapSRV);
            }
            context.UpdateSubresource(ref perMaterial, PerMaterialBuffer);
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.SetIndexBuffer(indexBuffers.First(), SharpDX.DXGI.Format.R32_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding_);
            context.Draw(indices.Count, 0);

            context.Rasterizer.State = state;

            if (EnvironmentMap != null)
                context.PixelShader.SetShaderResource(1, null);				

        }
    }
}
