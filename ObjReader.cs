using Silk.NET.Maths;
using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GrafikaSzeminarium
{
    struct ObjFace
    {
        public int coordsIndex;
        public int textureCoordsIndex;
        public int normalsIndex;
    }

    internal class ObjReader
    {
        private static bool voltTextura = false;
        private static bool voltNormalis = false;
        private static int osszesFaceDrb = 0;

        public static unsafe GlObject CreateRocket(GL Gl, string resourceName)
        {
            List<float[]> objVertices = new List<float[]>();
            List<float[]> objNormalVectors = new List<float[]>();
            List<float[]> objTextureCoords = new List<float[]>();
            List<ObjFace[]> objFaces = new List<ObjFace[]>();

            string fullResourceName = "GrafikaSzeminarium.Resources." + resourceName;
            using (var objStream = typeof(ObjReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (var objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line.Length == 1)
                        continue;

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            objVertices.Add(Array.ConvertAll(lineData, s => float.Parse(s, CultureInfo.InvariantCulture)));
                            break;
                        case "vn":
                            objNormalVectors.Add(Array.ConvertAll(lineData, s => float.Parse(s, CultureInfo.InvariantCulture)));
                            break;
                        case "vt":
                            objTextureCoords.Add(Array.ConvertAll(lineData, s => float.Parse(s, CultureInfo.InvariantCulture)));
                            break;
                        case "f":
                            List<ObjFace> face = new List<ObjFace>();
                            for (int i = 0; i < lineData.Length; i++)
                            {
                                var data = lineData[i].Split(new string[] { "//", "/" }, StringSplitOptions.RemoveEmptyEntries);
                                ObjFace f = new ObjFace();
                                if (data.Length > 0) f.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture) - 1;
                                if (data.Length > 1) f.textureCoordsIndex = int.Parse(data[1], CultureInfo.InvariantCulture) - 1;
                                if (data.Length > 2) f.normalsIndex = int.Parse(data[2], CultureInfo.InvariantCulture) - 1;
                                face.Add(f);
                            }
                            for (int i = 1; i < face.Count - 1; i++)
                                objFaces.Add(new ObjFace[] { face[0], face[i], face[i + 1] });
                            break;
                    }
                }
            }

            // Vertex + normal + UV kombinacii
            List<ObjVertexTransformationData> vertexTransformations = new List<ObjVertexTransformationData>();
            Dictionary<string, int> vertexLookup = new Dictionary<string, int>();
            List<uint> indices = new List<uint>();

            foreach (var tri in objFaces)
            {
                foreach (var v in tri)
                {
                    string key = $"{v.coordsIndex}/{v.textureCoordsIndex}/{v.normalsIndex}";
                    if (!vertexLookup.TryGetValue(key, out int index))
                    {
                        var coords = new Vector3D<float>(
                            objVertices[v.coordsIndex][0],
                            objVertices[v.coordsIndex][1],
                            objVertices[v.coordsIndex][2]
                        );

                        var normal = v.normalsIndex >= 0
                            ? new Vector3D<float>(objNormalVectors[v.normalsIndex][0], objNormalVectors[v.normalsIndex][1], objNormalVectors[v.normalsIndex][2])
                            : Vector3D<float>.Zero;

                        var uv = v.textureCoordsIndex >= 0
                            ? new Vector2D<float>(objTextureCoords[v.textureCoordsIndex][0], objTextureCoords[v.textureCoordsIndex][1])
                            : Vector2D<float>.Zero;

                        vertexTransformations.Add(new ObjVertexTransformationData(coords, normal, uv, 0));
                        index = vertexTransformations.Count - 1;
                        vertexLookup[key] = index;
                    }
                    indices.Add((uint)index);
                }
            }

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();

            foreach (var vertex in vertexTransformations)
            {
                glVertices.Add(vertex.Coordinates.X);
                glVertices.Add(vertex.Coordinates.Y);
                glVertices.Add(vertex.Coordinates.Z);

                glVertices.Add(vertex.Normal.X);
                glVertices.Add(vertex.Normal.Y);
                glVertices.Add(vertex.Normal.Z);

                glVertices.Add(vertex.TextureCoords.X);
                glVertices.Add(vertex.TextureCoords.Y);

                glColors.AddRange(new float[] { 1f, 0f, 0f, 1f }); // piros szín
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint vertexSize = 3 * sizeof(float) + 3 * sizeof(float) + 2 * sizeof(float);

            uint verticesBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, verticesBuffer);
            Gl.BufferData<float>(
                BufferTargetARB.ArrayBuffer,
                glVertices.ToArray(),
                BufferUsageARB.StaticDraw
            );
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)(6 * sizeof(float)));
            Gl.EnableVertexAttribArray(3);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
            Gl.BufferData<float>(
                BufferTargetARB.ArrayBuffer,
                glColors.ToArray(),
                BufferUsageARB.StaticDraw
            );
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indicesBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indicesBuffer);
            Gl.BufferData<uint>(
                BufferTargetARB.ElementArrayBuffer,
                indices.ToArray(),
                BufferUsageARB.StaticDraw
            );
            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            return new GlObject(vao, verticesBuffer, colors, indicesBuffer, (uint)indices.Count, Gl);
        }

        public static unsafe GlObject CreateRocketWithTexture(GL Gl, string objResourceName, string fallbackTextureName, float[]? szin = null)
        {
            voltNormalis = false;
            voltTextura = false;

            List<float[]> objVertices = new List<float[]>();
            List<float[]> objNormalVectors = new List<float[]>();
            List<float[]> objTextureCoords = new List<float[]>();
            List<ObjFace[]> objFaces = new List<ObjFace[]>();

            string fullObjResourceName = "GrafikaSzeminarium.Resources." + objResourceName;
            using (var objStream = typeof(ObjReader).Assembly.GetManifestResourceStream(fullObjResourceName))
            using (var objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line.Length == 1)
                        continue;

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < 3; i++)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;

                        case "f":
                            List<ObjFace> face = new List<ObjFace>();
                            for (int i = 0; i < lineData.Length; i++)
                            {
                                var data = lineData[i].Split(new string[] { "//", "/" }, StringSplitOptions.RemoveEmptyEntries);
                                ObjFace f = new ObjFace();
                                if (data.Length > 0) f.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
                                if (data.Length > 1) f.textureCoordsIndex = int.Parse(data[1], CultureInfo.InvariantCulture);
                                if (data.Length > 2) f.normalsIndex = int.Parse(data[2], CultureInfo.InvariantCulture);
                                face.Add(f);
                            }
                            osszesFaceDrb += face.Count;
                            for (int i = 0; i < face.Count - 2; i++)
                            {
                                ObjFace[] tri = new ObjFace[3];
                                tri[0] = face[0];
                                tri[1] = face[i + 1];
                                tri[2] = face[i + 2];
                                objFaces.Add(tri);
                            }
                            break;

                        case "vn":
                            voltNormalis = true;
                            float[] normalVektorok = new float[3];
                            for (int i = 0; i < 3; i++)
                                normalVektorok[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormalVectors.Add(normalVektorok);
                            break;

                        case "vt":
                            voltTextura = true;
                            float[] texCoord = new float[2];
                            for (int i = 0; i < 2; i++)
                                texCoord[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objTextureCoords.Add(texCoord);
                            break;
                    }
                }
            }

            string textureFile = fallbackTextureName;
            ImageResult? imageResult = null;
            if (!string.IsNullOrEmpty(textureFile))
            {
                imageResult = ReadTextureImg(textureFile);
            }

            List<ObjVertexTransformationData> vertexTransformations = new List<ObjVertexTransformationData>();
            for (int i = 0; i < objVertices.Count; i++)
            {
                Vector3D<float> normal = (voltNormalis && i < objNormalVectors.Count)
                    ? new Vector3D<float>(objNormalVectors[i][0], objNormalVectors[i][1], objNormalVectors[i][2])
                    : Vector3D<float>.Zero;

                Vector2D<float> uv = (voltTextura && i < objTextureCoords.Count)
                    ? new Vector2D<float>(objTextureCoords[i][0], objTextureCoords[i][1])
                    : Vector2D<float>.Zero;

                vertexTransformations.Add(new ObjVertexTransformationData(
                    new Vector3D<float>(objVertices[i][0], objVertices[i][1], objVertices[i][2]),
                    normal,
                    uv,
                    0
                ));
            }

            foreach (var objFace in objFaces)
            {
                var a = vertexTransformations[objFace[0].coordsIndex - 1];
                var b = vertexTransformations[objFace[1].coordsIndex - 1];
                var c = vertexTransformations[objFace[2].coordsIndex - 1];

                var normal = Vector3D.Normalize(Vector3D.Cross(b.Coordinates - a.Coordinates, c.Coordinates - a.Coordinates));

                a.UpdateNormalWithContributionFromAFace(normal);
                b.UpdateNormalWithContributionFromAFace(normal);
                c.UpdateNormalWithContributionFromAFace(normal);
            }

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            foreach (ObjVertexTransformationData v in vertexTransformations)
            {
                glVertices.Add(v.Coordinates.X);
                glVertices.Add(v.Coordinates.Y);
                glVertices.Add(v.Coordinates.Z);

                glVertices.Add(v.Normal.X);
                glVertices.Add(v.Normal.Y);
                glVertices.Add(v.Normal.Z);

                glVertices.Add(v.TextureCoords.X);
                glVertices.Add(v.TextureCoords.Y);

                if (szin != null) glColors.AddRange(szin);
                else glColors.AddRange(new float[] { 1f, 0f, 0f, 1f });
            }

            List<uint> glIndexArray = new List<uint>();
            for (uint i = 0; i < (uint)vertexTransformations.Count; i++)
                glIndexArray.Add(i);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint vertexSize = 3 * sizeof(float) + 3 * sizeof(float) + 2 * sizeof(float);
            uint verticesBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, verticesBuffer);
            Gl.BufferData<float>(
                BufferTargetARB.ArrayBuffer,
                glVertices.ToArray(),
                BufferUsageARB.StaticDraw
            );
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)(6 * sizeof(float)));
            Gl.EnableVertexAttribArray(3);

            uint texture = Gl.GenTexture();
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, texture);

            if (imageResult != null)
            {
                var textureBytes = (ReadOnlySpan<byte>)imageResult.Data.AsSpan();

                Gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)imageResult.Width,
                    (uint)imageResult.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    textureBytes
                );
            }

            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            Gl.BufferData<uint>(
                BufferTargetARB.ElementArrayBuffer,
                (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(),
                BufferUsageARB.StaticDraw
            );
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            Gl.BindVertexArray(0);

            return new GlObject(vao, verticesBuffer, glColors.Count > 0 ? verticesBuffer : 0, indices, (uint)glIndexArray.Count, Gl, texture);
        }

        private static unsafe ImageResult ReadTextureImg(string textureResource)
        {
            string fullResourceName = "GrafikaSzeminarium.Resources." + textureResource;
            using Stream stream = typeof(ObjReader).Assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
                throw new Exception("Nem találom ezt a resource-t: " + fullResourceName);
            return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
    }

    internal class GlObject
    {
        public uint? Texture { get; private set; }
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }

        private GL Gl;

        public GlObject(uint vao, uint vertices, uint colors, uint indeces, uint indexArrayLength, GL gl, uint texture = 0)
        {
            Vao = vao;
            Vertices = vertices;
            Colors = colors;
            Indices = indeces;
            IndexArrayLength = indexArrayLength;
            Gl = gl;
            Texture = texture;
        }

        internal void ReleaseGlObject()
        {
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
        }
    }
}