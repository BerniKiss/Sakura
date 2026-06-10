using Silk.NET.OpenGL;

namespace GrafikaSzeminarium
{
	internal class GlObject {

		public uint? Texture { get; private set; }
		public uint Vao { get; }
		public uint Vertices { get; }
		public uint Colors { get; }
		public uint Indices { get; }
		public uint IndexArrayLength { get; }

		private GL Gl;

		public GlObject(uint vao, uint vertices, uint colors, uint indeces, uint indexArrayLength, GL gl, uint texture = 0) {
			this.Vao = vao;
			this.Vertices = vertices;
			this.Colors = colors;
			this.Indices = indeces;
			this.IndexArrayLength = indexArrayLength;
			this.Gl = gl;
			this.Texture = texture;
		}

		internal void ReleaseGlObject() {
			// always unbound the vertex buffer first, so no halfway results are displayed by accident
			Gl.DeleteBuffer(Vertices);
			Gl.DeleteBuffer(Colors);
			Gl.DeleteBuffer(Indices);
			Gl.DeleteVertexArray(Vao);
		}
	}
}
