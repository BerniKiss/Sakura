using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.IO;

namespace GrafikaSzeminarium
{
    internal static class Program
    {
        private static IWindow window;
        private static IInputContext inputContext;
        private static GL Gl;
        private static ImGuiController controller;
        private static uint program;

        private static CameraDescriptor cameraDescriptor =
            new CameraDescriptor(new Vector3D<float>(0f, 2f, 18f));

        private static GlCube skyBox;
        private static GlObject player;
        private static GlObject asteroid;

        private static Vector3D<float> playerPosition = new Vector3D<float>(0f, 0f, 0f);
        private static float playerRotationY = 0f;
        private static float playerSpeed = 0.3f;

        private const int AsteroidCount = 60;
        private static Vector3D<float>[] asteroidPositions = new Vector3D<float>[AsteroidCount];
        private static float[] asteroidSpeeds = new float[AsteroidCount];
        private static bool[] asteroidDestroyed = new bool[AsteroidCount];

        private static Random random = new Random();
        private static bool firstPersonView = false;
        private static float sceneTime = 0f;
        private static int destroyedCount = 0;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string TextureUniformVariableName = "uTexture";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Space Shooter";
            windowOptions.Size = new Vector2D<int>(1600, 900);

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
                keyboard.KeyDown += Keyboard_KeyDown;

            Gl = window.CreateOpenGL();
            controller = new ImGuiController(Gl, window, inputContext);

            window.FramebufferResize += s => Gl.Viewport(s);
            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();
            InitializeAsteroids();
            LinkProgram();

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.C)
                firstPersonView = !firstPersonView;
        }

        private static void Window_Update(double deltaTime)
        {
            var keyboard = inputContext.Keyboards[0];

            MovePlayer(keyboard, (float)deltaTime);
            UpdateAsteroids((float)deltaTime);
            CheckCollisions();
            UpdateCamera();

            controller.Update((float)deltaTime);
        }

        private static void Window_Render(double deltaTime)
        {
            sceneTime += (float)deltaTime;

            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawSkyBox();
            DrawAsteroids();
            DrawPlayer();
            DrawGui();

            controller.Render();
        }

        private static void MovePlayer(IKeyboard keyboard, float deltaTime)
        {
            Vector3D<float> movement = Vector3D<float>.Zero;

            if (keyboard.IsKeyPressed(Key.W)) movement.Z -= playerSpeed;
            if (keyboard.IsKeyPressed(Key.S)) movement.Z += playerSpeed;
            if (keyboard.IsKeyPressed(Key.A)) movement.X -= playerSpeed;
            if (keyboard.IsKeyPressed(Key.D)) movement.X += playerSpeed;

            if (movement.X != 0f || movement.Z != 0f)
            {
                playerPosition += movement;
                playerRotationY = MathF.Atan2(movement.X, movement.Z);
            }
        }

        private static void UpdateCamera()
        {
            if (firstPersonView)
            {
                Vector3D<float> eye = playerPosition + new Vector3D<float>(0f, 2f, 0f);
                Vector3D<float> look = new Vector3D<float>(MathF.Sin(playerRotationY), 0f, MathF.Cos(playerRotationY));

                cameraDescriptor.setCameraPosition(eye);
                cameraDescriptor.Yaw = playerRotationY * 180f / MathF.PI - 90f;
                cameraDescriptor.Pitch = 0f;
            }
            else
            {
                cameraDescriptor.setCameraPosition(new Vector3D<float>(0f, 12f, 35f));
                cameraDescriptor.Yaw = -90f;
                cameraDescriptor.Pitch = -20f;
            }
        }

        private static void InitializeAsteroids()
        {
            for (int i = 0; i < AsteroidCount; i++)
            {
                asteroidPositions[i] = new Vector3D<float>(
                    random.Next(-80, 80),
                    random.Next(-35, 35),
                    random.Next(-120, 20)
                );

                asteroidSpeeds[i] = 1f + (float)random.NextDouble() * 3f;
                asteroidDestroyed[i] = false;
            }
        }

        private static void UpdateAsteroids(float deltaTime)
        {
            for (int i = 0; i < AsteroidCount; i++)
            {
                if (asteroidDestroyed[i]) continue;

                asteroidPositions[i].Z += asteroidSpeeds[i] * deltaTime;

                if (asteroidPositions[i].Z > 5f)
                    ResetAsteroid(i);
            }
        }

        private static void ResetAsteroid(int index)
        {
            asteroidPositions[index] = new Vector3D<float>(
                random.Next(-80, 80),
                random.Next(-35, 35),
                random.Next(-120, -20)
            );

            asteroidDestroyed[index] = false;
        }

        private static void CheckCollisions()
        {
            for (int i = 0; i < AsteroidCount; i++)
            {
                if (asteroidDestroyed[i]) continue;

                float dx = playerPosition.X - asteroidPositions[i].X;
                float dz = playerPosition.Z - asteroidPositions[i].Z;
                float dist = MathF.Sqrt(dx * dx + dz * dz);

                if (dist < 5f)
                {
                    destroyedCount++;
                    asteroidDestroyed[i] = true;
                    ResetAsteroid(i);
                }
            }
        }

        private static unsafe void DrawPlayer()
        {
            Matrix4X4<float> model =
                Matrix4X4.CreateScale(3f) *
                Matrix4X4.CreateRotationY(playerRotationY) *
                Matrix4X4.CreateTranslation(playerPosition);

            DrawTexturedObject(player, model);
        }

        private static unsafe void DrawAsteroids()
        {
            for (int i = 0; i < AsteroidCount; i++)
            {
                if (asteroidDestroyed[i]) continue;

                Matrix4X4<float> model =
                    Matrix4X4.CreateScale(5f) *
                    Matrix4X4.CreateRotationY(sceneTime + i) *
                    Matrix4X4.CreateTranslation(asteroidPositions[i]);

                DrawTexturedObject(asteroid, model);
            }
        }

        private static unsafe void DrawSkyBox()
        {
            Matrix4X4<float> model = Matrix4X4.CreateScale(3500f);
            SetModelMatrix(model);

            Gl.BindVertexArray(skyBox.Vao);
            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            Gl.Uniform1(textureLocation, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);
            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static void DrawGui()
        {
            ImGui.Begin("Space Shooter", ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.Text($"Destroyed Asteroids: {destroyedCount}");
            ImGui.Text("Controls: W A S D - move | C - switch camera");
            ImGui.End();
        }

        private static unsafe void SetUpObjects()
        {
            skyBox = GlCube.CreateInteriorCube(Gl, "space.png");

            // player = ObjectResourceReader.CreateObjectWithTextureFromResource(
            //   Gl, "Fighter_01.obj", "fighter.png");
            player = ObjectResourceReader.CreateObjectFromResource(
               Gl,
               "Fighter_01.obj"
           );

            asteroid = ObjectResourceReader.CreateObjectWithTextureFromResource(
                Gl, "Asteroid_1.obj", "Asteroid_1_Diffuse_1K.png");
        }

        //private static unsafe void SetUpObjects()
        //{
        //    // Skybox
        //    skyBox = GlCube.CreateInteriorCube(Gl, "space.png");

        //    // Rakéta - középen, piros, textúra nélkül
        //    player = ObjectResourceReader.CreateObjectFromResource(
        //        Gl,
        //        "Fighter_01.obj"
        //    );
        //    player.Position = new Vector3D<float>(0f, 0f, 0f);
        //    player.Scale = 5f; // nagyobb méret a láthatósághoz

        //    // Aszteroidák - több darab, eltérő pozícióval és mérettel
        //    asteroid = ObjectResourceReader.CreateObjectWithTextureFromResource(
        //        Gl,
        //        "Asteroid_1.obj",
        //        "Asteroid_1_Diffuse_1K.png"
        //    );
        //    asteroid.Position = new Vector3D<float>(20f, 10f, -50f); // messzebb, de látható
        //    asteroid.Scale = 10f;

        //    // Ha több aszteroida kell
        //    asteroid2 = ObjectResourceReader.CreateObjectWithTextureFromResource(
        //        Gl,
        //        "Asteroid_1.obj",
        //        "Asteroid_1_Diffuse_1K.png"
        //    );
        //    asteroid2.Position = new Vector3D<float>(-30f, 5f, -70f);
        //    asteroid2.Scale = 8f;

        //    asteroid3 = ObjectResourceReader.CreateObjectWithTextureFromResource(
        //        Gl,
        //        "Asteroid_1.obj",
        //        "Asteroid_1_Diffuse_1K.png"
        //    );
        //    asteroid3.Position = new Vector3D<float>(15f, -20f, -60f);
        //    asteroid3.Scale = 12f;
        //}

        private static void Window_Closing()
        {
            skyBox.ReleaseGlObject();
            player.ReleaseGlObject();
            asteroid.ReleaseGlObject();
        }

        private static unsafe void DrawTexturedObject(GlObject obj, Matrix4X4<float> modelMatrix)
        {
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(obj.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, obj.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, obj.IndexArrayLength, GLEnum.UnsignedInt, null);

            Gl.BindVertexArray(0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            CheckError();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);

            // Normál mátrix egyszerűsített számítása: felső 3x3
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(
                modelMatrix.Row1.X, modelMatrix.Row1.Y, modelMatrix.Row1.Z,
                modelMatrix.Row2.X, modelMatrix.Row2.Y, modelMatrix.Row2.Z,
                modelMatrix.Row3.X, modelMatrix.Row3.Y, modelMatrix.Row3.Z
            );

            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);

            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            Matrix4X4<float> viewMatrix =
                Matrix4X4.CreateLookAt(cameraDescriptor.PositionInWorld,
                                       cameraDescriptor.TargetInWorld,
                                       cameraDescriptor.UpVector);

            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {
            Matrix4X4<float> projectionMatrix =
                Matrix4X4.CreatePerspectiveFieldOfView<float>(
                    (float)Math.PI / 2f,
                    1600f / 900f,
                    0.1f,
                    5000f
                );

            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out int status);
            if (status == 0)
                Console.WriteLine($"Error linking shader: {Gl.GetProgramInfoLog(program)}");

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static string ReadShader(string shaderFileName)
        {
            string fullResourceName = "GrafikaSzeminarium.Shaders." + shaderFileName;
            using Stream shaderStream = typeof(Program).Assembly.GetManifestResourceStream(fullResourceName);
            if (shaderStream == null)
                throw new Exception("Nem találom ezt a shader resource-t: " + fullResourceName);
            using StreamReader reader = new StreamReader(shaderStream);
            return reader.ReadToEnd();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}