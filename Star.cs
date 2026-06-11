using Silk.NET.OpenGL;

namespace GrafikaSzeminarium
{
    internal class Star : GlObject
    {
        private Star(
            uint vao,
            uint vertices,
            uint colors,
            uint indices,
            uint indexArrayLength,
            GL gl)
            : base(vao, vertices, colors, indices, indexArrayLength, gl)
        {
        }

        public static unsafe Star CreateStar(GL Gl)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray =
            {
                // elülső csillag forma, z = 0.1
                 0f,  1f,  0.1f,   0f, 0f, 1f,
                 0.25f, 0.25f, 0.1f,   0f, 0f, 1f,
                 1f,  0.2f,  0.1f,   0f, 0f, 1f,
                 0.4f, -0.1f, 0.1f,   0f, 0f, 1f,
                 0.6f, -0.9f, 0.1f,   0f, 0f, 1f,
                 0f, -0.45f, 0.1f,   0f, 0f, 1f,
                -0.6f, -0.9f, 0.1f,   0f, 0f, 1f,
                -0.4f, -0.1f, 0.1f,   0f, 0f, 1f,
                -1f, 0.2f, 0.1f,   0f, 0f, 1f,
                -0.25f, 0.25f, 0.1f,   0f, 0f, 1f,

                // hátulsó csillag forma, z = -0.1
                 0f,  1f, -0.1f,   0f, 0f, -1f,
                 0.25f, 0.25f, -0.1f,   0f, 0f, -1f,
                 1f,  0.2f, -0.1f,   0f, 0f, -1f,
                 0.4f, -0.1f, -0.1f,   0f, 0f, -1f,
                 0.6f, -0.9f, -0.1f,   0f, 0f, -1f,
                 0f, -0.45f, -0.1f,   0f, 0f, -1f,
                -0.6f, -0.9f, -0.1f,   0f, 0f, -1f,
                -0.4f, -0.1f, -0.1f,   0f, 0f, -1f,
                -1f, 0.2f, -0.1f,   0f, 0f, -1f,
                -0.25f, 0.25f, -0.1f,   0f, 0f, -1f,
            };

            float[] colorArray = new float[20 * 4];

            for (int i = 0; i < 20; i++)
            {
                colorArray[i * 4 + 0] = 1f;
                colorArray[i * 4 + 1] = 0.85f;
                colorArray[i * 4 + 2] = 0f;
                colorArray[i * 4 + 3] = 1f;
            }

            uint[] indexArray =
            {
                // elülső oldal
                0, 1, 9,
                1, 2, 3,
                1, 3, 5,
                3, 4, 5,
                5, 6, 7,
                5, 7, 9,
                7, 8, 9,
                1, 5, 9,

                // hátulsó oldal
                10, 19, 11,
                11, 13, 12,
                11, 15, 13,
                13, 15, 14,
                15, 17, 16,
                15, 19, 17,
                17, 19, 18,
                11, 19, 15,

                // oldalfalak
                0, 10, 1,
                1, 10, 11,
                1, 11, 2,
                2, 11, 12,
                2, 12, 3,
                3, 12, 13,
                3, 13, 4,
                4, 13, 14,
                4, 14, 5,
                5, 14, 15,
                5, 15, 6,
                6, 15, 16,
                6, 16, 7,
                7, 16, 17,
                7, 17, 8,
                8, 17, 18,
                8, 18, 9,
                9, 18, 19,
                9, 19, 0,
                0, 19, 10
            };

            uint offsetPos = 0;
            uint offsetNormal = 3 * sizeof(float);
            uint vertexSize = 6 * sizeof(float);

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertexArray, BufferUsageARB.StaticDraw);

            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            Gl.EnableVertexAttribArray(2);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
            Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, colorArray, BufferUsageARB.StaticDraw);

            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
            Gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indexArray, BufferUsageARB.StaticDraw);

            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            Gl.BindVertexArray(0);

            return new Star(vao, vertices, colors, indices, (uint)indexArray.Length, Gl);
        }
    }
}