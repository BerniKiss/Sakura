using Silk.NET.Maths;
using Silk.NET.OpenGL;

using StbImageSharp;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GrafikaSzeminarium
{

	struct ObjFace {
		public int coordsIndex;
		public int textureCoordsIndex;
		public int normalsIndex;
	}
	internal class ObjectResourceReader {

		private static bool voltTextura = false;
		private static bool voltNormalis = false;
		private static int osszesFaceDrb = 0;
		public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName) {
			List<float[]> objVertices = new List<float[]>();
			List<int[]> objFaces = new List<int[]>();
			List<float[]> objNormalVectors = new List<float[]>();
			List<float[]> objTextureCoords = new List<float[]>();

			string fullResourceName = "GrafikaSzeminarium.Resources." + resourceName;
			using (var objStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
			using (var objReader = new StreamReader(objStream)) {
				while (!objReader.EndOfStream) {
					var line = objReader.ReadLine();

					if (string.IsNullOrWhiteSpace(line) || line.Length == 1)
						continue;

					var lineClassifier = line.Substring(0, line.IndexOf(' '));
					var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');

					switch (lineClassifier) {
						case "v":
							float[] vertex = new float[3];
							for (int i = 0; i < vertex.Length; ++i)
								vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
							objVertices.Add(vertex);
							break;
						case "f":
							int[] face = new int[3];
							if (line.Contains("//") || line.Contains("/")) {
								for (int i = 0; i < face.Length; ++i) {
									var data = lineData[i].Trim().Split(new string[] { "//", "/" }, StringSplitOptions.RemoveEmptyEntries);
									face[i] = int.Parse(data[0], CultureInfo.InvariantCulture);
								}
							} else {
								for (int i = 0; i < face.Length; ++i)
									face[i] = int.Parse(lineData[i], CultureInfo.InvariantCulture);
							}
							objFaces.Add(face);
							break;
						case "vn":
							float[] normalVektorok = new float[3];
							for (int i = 0; i < normalVektorok.Length; ++i)
								normalVektorok[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
							objNormalVectors.Add(normalVektorok);
							break;
						case "vt":
							float[] texCoord = new float[2]; // Feltételezve, hogy csak U és V koordinátákat tartalmaz
							for (int i = 0; i < texCoord.Length; ++i)
								texCoord[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
							objTextureCoords.Add(texCoord);
							break;
						default:
							continue;
							// throw new Exception("Unhandled obj structure.");
					}
				}
			}

			List<ObjVertexTransformationData> vertexTransformations = new List<ObjVertexTransformationData>();
			if (objNormalVectors.Count > 0) {
				for (int i = 0; i < objVertices.Count; ++i) {
					vertexTransformations.Add(new ObjVertexTransformationData(
						new Vector3D<float>(objVertices[i][0], objVertices[i][1], objVertices[i][2]),
						new Vector3D<float>(objNormalVectors[i][0], objNormalVectors[i][1], objNormalVectors[i][2]),
						Vector2D<float>.Zero,
						0
						));
				}
			} else {
				foreach (var objVertex in objVertices) {
					vertexTransformations.Add(new ObjVertexTransformationData(
						new Vector3D<float>(objVertex[0], objVertex[1], objVertex[2]),
						Vector3D<float>.Zero,
						Vector2D<float>.Zero,
						0
						));
				}
			}

			foreach (var objFace in objFaces) {
				var a = vertexTransformations[objFace[0] - 1];
				var b = vertexTransformations[objFace[1] - 1];
				var c = vertexTransformations[objFace[2] - 1];

				var normal = Vector3D.Normalize(Vector3D.Cross(b.Coordinates - a.Coordinates, c.Coordinates - a.Coordinates));

				a.UpdateNormalWithContributionFromAFace(normal);
				b.UpdateNormalWithContributionFromAFace(normal);
				c.UpdateNormalWithContributionFromAFace(normal);
			}

			List<float> glVertices = new List<float>();
			List<float> glColors = new List<float>();
			foreach (var vertexTransformation in vertexTransformations) {
				glVertices.Add(vertexTransformation.Coordinates.X);
				glVertices.Add(vertexTransformation.Coordinates.Y);
				glVertices.Add(vertexTransformation.Coordinates.Z);

				glVertices.Add(vertexTransformation.Normal.X);
				glVertices.Add(vertexTransformation.Normal.Y);
				glVertices.Add(vertexTransformation.Normal.Z);

				// Feltételezve, hogy minden vertexhez van textúrakoordináta
				//glVertices.Add(vertexTransformation.TextureCoords.X);  // u  koordinatak igazabol
				//glVertices.Add(vertexTransformation.TextureCoords.Y);  // v

				glColors.AddRange([1.0f, 0.0f, 0.0f, 1.0f]);
			}

			List<uint> glIndexArray = new List<uint>();
			foreach (var objFace in objFaces) {
				glIndexArray.Add((uint)(objFace[0] - 1));
				glIndexArray.Add((uint)(objFace[1] - 1));
				glIndexArray.Add((uint)(objFace[2] - 1));
			}

			uint vao = Gl.GenVertexArray();
			Gl.BindVertexArray(vao);

			uint offsetPos = 0;
			uint offsetNormals = offsetPos + 3 * sizeof(float);
			uint vertexSize = offsetNormals + 3 * sizeof(float);

			uint vertices = Gl.GenBuffer();
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
			Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
			Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
			Gl.EnableVertexAttribArray(0);
			Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
			Gl.EnableVertexAttribArray(2);

			uint colors = Gl.GenBuffer();
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
			Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
			Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
			Gl.EnableVertexAttribArray(1);

			uint indices = Gl.GenBuffer();
			Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
			Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

			// make sure to unbind array buffer
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

			uint indexArrayLength = (uint)glIndexArray.Count;

			Gl.BindVertexArray(0);

			return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
		}

		public static unsafe GlObject CreateObjectWithTextureFromResource(GL Gl, string resourceName, string textureName, float[]? szin = null) {
			voltNormalis = false; // ide bekellett tegyem, mert valamiert ha mar meg volt hivva ez a fuggveny akkor true-n maradt az erteke
			voltTextura = false;
			List<float[]> objVertices = new List<float[]>();
			List<float[]> objNormalVectors = new List<float[]>();
			List<float[]> objTextureCoords = new List<float[]>();
			// az f (Facek) hez kell
			List<ObjFace[]> objFaces = new List<ObjFace[]>();

			string fullResourceName = "GrafikaSzeminarium.Resources." + resourceName;
			using (var objStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
			using (var objReader = new StreamReader(objStream)) {
				while (!objReader.EndOfStream) {
					var line = objReader.ReadLine();

					if (string.IsNullOrWhiteSpace(line) || line.Length == 1)
						continue;

					var lineClassifier = line.Substring(0, line.IndexOf(' '));
					var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');

					switch (lineClassifier) {
						case "v":
							float[] vertex = new float[3];
							for (int i = 0; i < vertex.Length; ++i)
								vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
							objVertices.Add(vertex);
							break;
						case "f":
							List<ObjFace> face = new List<ObjFace>();
							if (line.Contains("//") || line.Contains("/")) {
								for (int i = 0; i < lineData.Length; ++i) {
									var data = lineData[i].Trim().Split(new string[] { "//", "/" }, StringSplitOptions.RemoveEmptyEntries);
									ObjFace facedata = new();
									switch (data.Length) {
										case 1:
											facedata.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
											break;
										case 2:
											facedata.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
											facedata.textureCoordsIndex = int.Parse(data[1], CultureInfo.InvariantCulture);
											break;
										case 3:
											facedata.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
											facedata.textureCoordsIndex = int.Parse(data[1], CultureInfo.InvariantCulture);
											facedata.normalsIndex = int.Parse(data[2], CultureInfo.InvariantCulture);
											break;
									}
									face.Add(facedata);
								}
							} else {
								ObjFace facedata = new();
								for (int i = 0; i < lineData.Length; ++i) {
									facedata.coordsIndex = int.Parse(lineData[i], CultureInfo.InvariantCulture);
									face.Add(facedata);
								}
							}
							// ez a resz azert kell hogyha a facekbol tobb van mint 3 akkor szetbontjuk 3 szogekre
							/*
							if (face.Count == 4) {
								osszesFaceDrb += 4;
								ObjFace[] haromszog_1 = new ObjFace[3];
								ObjFace[] haromszog_2 = new ObjFace[3];

								haromszog_1[0] = face[0];
								haromszog_1[1] = face[1];
								haromszog_1[2] = face[2];

								haromszog_2[0] = face[0];
								haromszog_2[1] = face[2];
								haromszog_2[2] = face[3];
								objFaces.Add(haromszog_1);
								objFaces.Add(haromszog_2);
							}
							if (face.Count == 3) {
								osszesFaceDrb += 3;
								ObjFace[] haromszog_1 = new ObjFace[3];
								haromszog_1[0] = face[0];
								haromszog_1[1] = face[1];
								haromszog_1[2] = face[2];
								objFaces.Add(haromszog_1);
							}
							*/
							osszesFaceDrb += face.Count;
							for (int i = 0; i < face.Count - 2; i++) {
								ObjFace[] haromszog = new ObjFace[3];
								haromszog[0] = face[0];
								haromszog[1] = face[i + 1];
								haromszog[2] = face[i + 2];
								objFaces.Add(haromszog);
							}

							break;
						case "vn":
							voltNormalis = true;
							float[] normalVektorok = new float[3];
							for (int i = 0; i < normalVektorok.Length; ++i)
								normalVektorok[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
							objNormalVectors.Add(normalVektorok);
							break;
						case "vt":
							voltTextura = true;
							float[] texCoord = new float[2]; // Feltételezve, hogy csak U és V koordinátákat tartalmaz
							for (int i = 0; i < texCoord.Length; ++i)
								texCoord[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
							objTextureCoords.Add(texCoord);
							break;
						default:
							continue;
							// throw new Exception("Unhandled obj structure.");
					}
				}
			}

			List<float> glVertices = new List<float>();
			List<float> glColors = new List<float>();
			foreach (ObjFace[] faces in objFaces) {
				foreach (ObjFace face in faces) {
					// csucs indexek
					glVertices.Add(objVertices[face.coordsIndex - 1][0]); // mindegyikbol kikell vonni -1 egy mert a facek 1-tol
					glVertices.Add(objVertices[face.coordsIndex - 1][1]); // vannak indexelve
					glVertices.Add(objVertices[face.coordsIndex - 1][2]);

					// normalisok indexei
					if (voltNormalis) {
						glVertices.Add(objNormalVectors[face.normalsIndex - 1][0]);
						glVertices.Add(objNormalVectors[face.normalsIndex - 1][1]);
						glVertices.Add(objNormalVectors[face.normalsIndex - 1][2]);
					}

					// texturak
					if (voltTextura) {
						glVertices.Add(objTextureCoords[face.textureCoordsIndex - 1][0]);  // u  koordinatak igazabol
						glVertices.Add(objTextureCoords[face.textureCoordsIndex - 1][1]);  // v
					}

					if (szin != null) {
						glColors.AddRange(szin);
					} else {
						glColors.AddRange([1f, 0f, 0f, 1f]);
					}
				}
			}
			List<uint> glIndexArray = new List<uint>();
			// ez itt eleg bonyolult es elter a megszokottol de egy kicsi csel van benne, ami egy napomba telt amig rajottem
			// fontebb egyel mikor glVertices.Add() osszerakom a csuszokat, normalisokat, texturat, ezeket mar ahogy az indexArrayben
			// szerepelne a sorrend olyan sorrendbe rakom bele tehat ha az indexArrayben 5 szerepel akkor az bekerul kezdesbol a 0-ik
			// helyre es en ha az indexArraybe meg hozzaadok egy 5-ik indexelest akkor mar nem az az elem lesz ott, ezert kell az
			// indexek sorba menjenek, kicsit erthetetlen de igy megy jol
			// azert kell az indexeket so
			for (int i = 0; i < osszesFaceDrb; i++) {
				glIndexArray.Add((uint)(i));
			}

			uint vao = Gl.GenVertexArray();
			Gl.BindVertexArray(vao);

			uint offsetPos = 0;
			uint offsetNormals = 0;
			if (voltNormalis) {
				offsetNormals = offsetPos + 3 * sizeof(float);
			}
			uint offsetTexture = offsetNormals + (3 * sizeof(float));
			uint vertexSize = offsetTexture + (2 * sizeof(float));

			uint vertices = Gl.GenBuffer();
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
			Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
			Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
			Gl.EnableVertexAttribArray(0);
			if (voltNormalis) {
				Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
				Gl.EnableVertexAttribArray(2);
			}

			if (szin != null) {
				uint colors = Gl.GenBuffer();
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
				Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
				Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
				Gl.EnableVertexAttribArray(1);

				uint indices = Gl.GenBuffer();
				Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
				Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

				// make sure to unbind array buffer
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

				uint indexArrayLength = (uint)glIndexArray.Count;

				Gl.BindVertexArray(0);

				return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);

			} else {
				uint colors = Gl.GenBuffer();
				// set texture
				// create texture
				uint texture = Gl.GenTexture();
				// activate texture 0
				Gl.ActiveTexture(TextureUnit.Texture0);
				// bind texture
				Gl.BindTexture(TextureTarget.Texture2D, texture);

				var skyboxImageResult = ReadTextureImage(textureName);
				var textureBytes = (ReadOnlySpan<byte>)skyboxImageResult.Data.AsSpan();
				Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)skyboxImageResult.Width,
					(uint)skyboxImageResult.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureBytes);
				Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
				Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				// unbinde texture
				Gl.BindTexture(TextureTarget.Texture2D, 0);

				Gl.EnableVertexAttribArray(3);
				Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexture);

				uint indices = Gl.GenBuffer();
				Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
				Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

				// make sure to unbind array buffer
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

				uint indexArrayLength = (uint)glIndexArray.Count;

				Gl.BindVertexArray(0);

				return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl, texture);
			}
		}

        private static unsafe ImageResult ReadTextureImage(string textureResource)
        {
            string fullResourceName = "GrafikaSzeminarium.Resources." + textureResource;

            using Stream skyboxStream =
                typeof(GlCube).Assembly.GetManifestResourceStream(fullResourceName);

            if (skyboxStream == null)
                throw new Exception("Nem találom ezt a resource-t: " + fullResourceName);

            return ImageResult.FromStream(skyboxStream, ColorComponents.RedGreenBlueAlpha);
        }
    }
}
