using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

using System.Reflection;
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

        private static SpaceBox skyBox;
        private static GlObject player;
        private static GlObject asteroid;
        private static Star starObstacle;

        private static Vector3D<float> playerPosition = new Vector3D<float>(0f, 0f, 0f);
        private static float playerRotationY = MathF.PI;
        private static float playerSpeed = 0.45f;

        private const int AsteroidCount = 40;
        private static Vector3D<float>[] asteroidPositions = new Vector3D<float>[AsteroidCount];
        private static float[] asteroidSpeeds = new float[AsteroidCount];
        private static bool[] asteroidDestroyed = new bool[AsteroidCount];

        private static List<MovingObstacle> obstacles = new();
        private static List<Vector3D<float>> bulletPositions = new List<Vector3D<float>>();
        private static List<Vector3D<float>> bulletDirections = new List<Vector3D<float>>();

        private static float bulletSpeed = 45f;
        private static float bulletMaxDistance = 170f;
        private static float bulletHitRadius = 10f;

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
            windowOptions.PreferredDepthBufferBits = 24;

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

            if (key == Key.Space)
                ShootBullet();
        }

        private static void Window_Update(double deltaTime)
        {
            var keyboard = inputContext.Keyboards[0];

            MovePlayer(keyboard);
            UpdateAsteroids((float)deltaTime);
            UpdateBullets((float)deltaTime);
            CheckPlayerAsteroidCollision();
            UpdateCamera();

            foreach (var obstacle in obstacles)
            {
                obstacle.Update((float)deltaTime);

                if (obstacle.CollidesWith(playerPosition, 5f))
                {
                    // reset player
                    playerPosition = new Vector3D<float>(0f, 0f, 0f);

                    // reset score
                    destroyedCount = 0;

                    // reset bullets
                    bulletPositions.Clear();
                    bulletDirections.Clear();

                    // reset asteroids
                    InitializeAsteroids();
                }
            }

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
            DrawBullets();
            DrawPlayer();
            DrawObstacles();
            DrawGui();

            controller.Render();
        }

        private static void MovePlayer(IKeyboard keyboard)
        {
            Vector3D<float> movement = Vector3D<float>.Zero;

            // W/S: előre-hátra
            if (keyboard.IsKeyPressed(Key.W))
                movement.Z -= playerSpeed;

            if (keyboard.IsKeyPressed(Key.S))
                movement.Z += playerSpeed;

            // A/D: oldalirányú mozgás (nem forgatjuk)
            if (keyboard.IsKeyPressed(Key.A))
                movement.X -= playerSpeed;

            if (keyboard.IsKeyPressed(Key.D))
                movement.X += playerSpeed;

            // Q/E: fel-le
            if (keyboard.IsKeyPressed(Key.Q))
                movement.Y += playerSpeed;

            if (keyboard.IsKeyPressed(Key.E))
                movement.Y -= playerSpeed;

            if (movement.X != 0f || movement.Y != 0f || movement.Z != 0f)
            {
                playerPosition += movement;

                playerPosition.X = Math.Clamp(playerPosition.X, -85f, 85f);
                playerPosition.Y = Math.Clamp(playerPosition.Y, -35f, 35f);
                playerPosition.Z = Math.Clamp(playerPosition.Z, -90f, 30f);
            }

            // rskewta forgatas
            if (keyboard.IsKeyPressed(Key.R))
            {
                playerRotationY += 0.05f;
            }
        }
        //private static void MovePlayer(IKeyboard keyboard)
        //{
        //    Vector3D<float> movement = Vector3D<float>.Zero;

        //    if (keyboard.IsKeyPressed(Key.W)) movement.Z -= playerSpeed;
        //    if (keyboard.IsKeyPressed(Key.S)) movement.Z += playerSpeed;
        //    if (keyboard.IsKeyPressed(Key.A)) movement.X -= playerSpeed;
        //    if (keyboard.IsKeyPressed(Key.D)) movement.X += playerSpeed;

        //    if (movement.X != 0f || movement.Z != 0f)
        //    {
        //        playerPosition += movement;

        //        playerPosition.X = Math.Clamp(playerPosition.X, -85f, 85f);
        //        playerPosition.Y = Math.Clamp(playerPosition.Y, -35f, 35f);
        //        playerPosition.Z = Math.Clamp(playerPosition.Z, -90f, 30f);

        //        playerRotationY = MathF.Atan2(movement.X, movement.Z);
        //    }
        //}

        private static void ShootBullet()
        {
            Vector3D<float> direction = new Vector3D<float>(
                MathF.Sin(playerRotationY),
                0f,
                MathF.Cos(playerRotationY)
            );

            Vector3D<float> startPosition =
                playerPosition + direction * 5f + new Vector3D<float>(0f, 0.3f, 0f);

            bulletPositions.Add(startPosition);
            bulletDirections.Add(direction);

            PlayShootSound();
        }

        private static void UpdateBullets(float deltaTime)
        {
            for (int i = bulletPositions.Count - 1; i >= 0; i--)
            {
                Vector3D<float> oldPosition = bulletPositions[i];
                Vector3D<float> newPosition = oldPosition + bulletDirections[i] * bulletSpeed * deltaTime;

                bulletPositions[i] = newPosition;

                if (CalculateDistance(playerPosition, bulletPositions[i]) > bulletMaxDistance)
                {
                    bulletPositions.RemoveAt(i);
                    bulletDirections.RemoveAt(i);
                    continue;
                }

                for (int j = 0; j < AsteroidCount; j++)
                {
                    if (asteroidDestroyed[j]) continue;

                    float distance = DistancePointToSegment(
                        asteroidPositions[j],
                        oldPosition,
                        newPosition
                    );

                    if (distance < bulletHitRadius)
                    {
                        destroyedCount++;
                        ResetAsteroid(j);

                        PlayExplosionSound();

                        bulletPositions.RemoveAt(i);
                        bulletDirections.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private static float DistancePointToSegment(
    Vector3D<float> point,
    Vector3D<float> segmentStart,
    Vector3D<float> segmentEnd)
        {
            Vector3D<float> segment = segmentEnd - segmentStart;
            Vector3D<float> toPoint = point - segmentStart;

            float segmentLengthSquared =
                segment.X * segment.X +
                segment.Y * segment.Y +
                segment.Z * segment.Z;

            if (segmentLengthSquared == 0f)
                return CalculateDistance(point, segmentStart);

            float t =
                (toPoint.X * segment.X +
                 toPoint.Y * segment.Y +
                 toPoint.Z * segment.Z) / segmentLengthSquared;

            t = Math.Clamp(t, 0f, 1f);

            Vector3D<float> closestPoint =
                segmentStart + segment * t;

            return CalculateDistance(point, closestPoint);
        }

        private static void UpdateCamera()
        {
            if (firstPersonView)
            {
                // kamera kisse hatrebb es feljebb
                Vector3D<float> offset = new Vector3D<float>(0f, 3f, -6f);
                Vector3D<float> eye = playerPosition + offset;

                cameraDescriptor.setCameraPosition(eye);

                // A  forgas
                cameraDescriptor.HorizontalAngle = - 90f;
                cameraDescriptor.VerticalAngle = -18f;
            }
            else
            {
                // harmadi kszemely kamera mogul es fentrol
                Vector3D<float> eye = playerPosition + new Vector3D<float>(0f, 15f, 40f);
                cameraDescriptor.setCameraPosition(eye);

                cameraDescriptor.HorizontalAngle = -90f;
                cameraDescriptor.VerticalAngle = -20f;
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

                if (asteroidPositions[i].Z > 35f)
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

            asteroidSpeeds[index] = 1f + (float)random.NextDouble() * 3f;
            asteroidDestroyed[index] = false;
        }

        private static void CheckPlayerAsteroidCollision()
        {
            for (int i = 0; i < AsteroidCount; i++)
            {
                if (asteroidDestroyed[i]) continue;

                float distance = CalculateDistance(playerPosition, asteroidPositions[i]);

                if (distance < 6f)
                {
                    ResetAsteroid(i);
                }
            }
        }

        private static float CalculateDistance(Vector3D<float> a, Vector3D<float> b)
        {
            float x = a.X - b.X;
            float y = a.Y - b.Y;
            float z = a.Z - b.Z;

            return MathF.Sqrt(x * x + y * y + z * z);
        }

        private static unsafe void DrawPlayer()
        {
            Matrix4X4<float> model =
                Matrix4X4.CreateScale(1.5f) *
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
                    Matrix4X4.CreateRotationX(sceneTime * 0.6f + i) *
                    Matrix4X4.CreateTranslation(asteroidPositions[i]);

                DrawTexturedObject(asteroid, model);
            }
        }

        // ne felejtsd el
        private static unsafe void DrawBullets()
        {
            for (int i = 0; i < bulletPositions.Count; i++)
            {
                float bulletRotationY = MathF.Atan2(bulletDirections[i].X, bulletDirections[i].Z);

                Matrix4X4<float> model =
                    Matrix4X4.CreateScale(0.35f) *
                    Matrix4X4.CreateRotationY(bulletRotationY) *
                    Matrix4X4.CreateTranslation(bulletPositions[i]);

                DrawTexturedObject(player, model);
            }
        }

        private static unsafe void DrawObstacles()
        {
            foreach (var obs in obstacles)
            {
                Matrix4X4<float> model =
                    Matrix4X4.CreateScale(3f) *
                    Matrix4X4.CreateTranslation(obs.Position);

                DrawTexturedObject(starObstacle, model);
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
            Gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private static void DrawGui()
        {
            ImGui.Begin("Space Shooter", ImGuiWindowFlags.AlwaysAutoResize);

            ImGui.Text($"Destroyed Asteroids: {destroyedCount}");
            ImGui.Text($"Bullets: {bulletPositions.Count}");
            ImGui.Separator();


            ImGui.Text("Controls:");
            ImGui.Separator();

            ImGui.BulletText("W / S - Forward / Backward");
            ImGui.BulletText("A / D - Left / Right");
            ImGui.BulletText("Q / E - Up / Down");
            ImGui.BulletText("R - Rotate spaceship");
            ImGui.BulletText("SPACE - Shoot");
            ImGui.BulletText("C - Switch camera");
            ImGui.Checkbox("Rocket camera", ref firstPersonView);
            ImGui.SliderFloat("Rocket speed", ref playerSpeed, 0.1f, 1.2f);

            ImGui.End();
        }

        // csak komment 
        private static unsafe void SetUpObjects()
        {
            skyBox = SpaceBox.CreateInteriorCube(Gl, "space.png");

            player = ObjReader.CreateObjectFromResource(
                Gl,
                "Fighter_01.obj"
            );

            asteroid = ObjReader.CreateObjectWithTextureFromResource(
                Gl,
                "Asteroid_1.obj",
                "Asteroid_1_Diffuse_1K.png"
            );

            for (int i = 0; i < 8; i++)
            {
                obstacles.Add(new MovingObstacle(
                    new Vector3D<float>(
                        random.Next(-70, 70),
                        random.Next(-25, 25),
                        random.Next(-100, -20)
                    ),
                    random.Next(10, 30),
                    random.Next(6, 16)
                ));
            }
            starObstacle = Star.CreateStar(Gl);
        }

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
                Matrix4X4.CreateLookAt(
                    cameraDescriptor.Position,
                    cameraDescriptor.Target,
                    cameraDescriptor.UpVector
                );

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

            using Stream shaderStream =
                typeof(Program).Assembly.GetManifestResourceStream(fullResourceName);

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

        private static void PlayShootSound()
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("GrafikaSzeminarium.Resources.shoot.wav");

            if (stream != null)
            {
                SoundPlayer player = new SoundPlayer(stream);
                player.Play();
            }
        }

        private static void PlayExplosionSound()
        {
            var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("GrafikaSzeminarium.Resources.explosion.wav");

            if (stream != null)
            {
                SoundPlayer player = new SoundPlayer(stream);
                player.Play();
            }
        }
    }
}