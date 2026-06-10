using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
//using Szeminarium;

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
            new CameraDescriptor(new Vector3D<float>(0f, 6f, 18f));

        private static GlCube skyBox;

        private static GlObject bunny;
        private static GlObject sakuraTree;
        private static GlObject petal;
        private static GlObject ground;

        private static Vector3D<float> bunnyPosition = new Vector3D<float>(0f, 0f, 0f);
        private static float bunnyRotationY = 0f;
        private static float bunnySpeed = 0.25f;

        private const int TreeCount = 6;
        private static Vector3D<float>[] treePositions = new Vector3D<float>[TreeCount]
        {
            new Vector3D<float>(-12f, 0f, -10f),
            new Vector3D<float>(12f, 0f, -10f),
            new Vector3D<float>(-16f, 0f, 8f),
            new Vector3D<float>(16f, 0f, 8f),
            new Vector3D<float>(-6f, 0f, 16f),
            new Vector3D<float>(8f, 0f, 18f)
        };

        private const int PetalCount = 30;
        private static Vector3D<float>[] petalPositions = new Vector3D<float>[PetalCount];
        private static float[] petalFallSpeeds = new float[PetalCount];
        private static float[] petalDriftOffsets = new float[PetalCount];
        private static bool[] petalCollected = new bool[PetalCount];

        private static Random random = new Random();

        private static int collectedPetals = 0;
        private static bool bunnyFirstPersonView = false;

        private static float sceneTime = 0f;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static float Shininess = 0.4f;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Sakura Kert";
            windowOptions.Size = new Vector2D<int>(1800, 1500);
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
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            controller = new ImGuiController(Gl, window, inputContext);

            window.FramebufferResize += s =>
            {
                Gl.Viewport(s);
            };

            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();
            InitializePetals();
            LinkProgram();

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.C)
            {
                bunnyFirstPersonView = !bunnyFirstPersonView;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            var keyboard = inputContext.Keyboards[0];

            MoveBunny(keyboard);
            UpdatePetals((float)deltaTime);
            CheckPetalCollection();
            UpdateCamera();

            controller.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            sceneTime += (float)deltaTime;

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            //SetLightColor();
            //SetLightPosition();
            //SetViewerPosition();
            //SetShininess();

            DrawSkyBox();
            DrawGardenGround();
            DrawSakuraTrees();
            DrawPetals();
            DrawBunny();

            DrawGui();

            controller.Render();
        }

        private static void MoveBunny(IKeyboard keyboard)
        {
            Vector3D<float> movement = Vector3D<float>.Zero;

            if (keyboard.IsKeyPressed(Key.W))
                movement.Z -= bunnySpeed;

            if (keyboard.IsKeyPressed(Key.S))
                movement.Z += bunnySpeed;

            if (keyboard.IsKeyPressed(Key.A))
                movement.X -= bunnySpeed;

            if (keyboard.IsKeyPressed(Key.D))
                movement.X += bunnySpeed;

            if (movement.X != 0f || movement.Z != 0f)
            {
                bunnyPosition += movement;

                bunnyPosition.X = Math.Clamp(bunnyPosition.X, -22f, 22f);
                bunnyPosition.Z = Math.Clamp(bunnyPosition.Z, -22f, 22f);

                bunnyRotationY = MathF.Atan2(movement.X, movement.Z);
            }
        }

        private static void UpdateCamera()
        {
            if (bunnyFirstPersonView)
            {
                Vector3D<float> eyePosition =
                    bunnyPosition + new Vector3D<float>(0f, 2.2f, 0f);

                Vector3D<float> lookDirection =
                    new Vector3D<float>(
                        MathF.Sin(bunnyRotationY),
                        0f,
                        MathF.Cos(bunnyRotationY)
                    );

                cameraDescriptor.setCameraPosition(eyePosition);
                cameraDescriptor.Yaw = bunnyRotationY * 180f / MathF.PI - 90f;
                cameraDescriptor.Pitch = 0f;
            }
            else
            {
                Vector3D<float> thirdPersonPosition =
                    bunnyPosition + new Vector3D<float>(0f, 7f, 16f);

                cameraDescriptor.setCameraPosition(thirdPersonPosition);
                cameraDescriptor.Yaw = -90f;
                cameraDescriptor.Pitch = -22f;
            }
        }

        private static void InitializePetals()
        {
            for (int i = 0; i < PetalCount; i++)
            {
                ResetPetal(i);
            }
        }

        private static void ResetPetal(int index)
        {
            Vector3D<float> treePosition = treePositions[random.Next(TreeCount)];

            float randomX = ((float)random.NextDouble() - 0.5f) * 7f;
            float randomZ = ((float)random.NextDouble() - 0.5f) * 7f;
            float randomY = 7f + (float)random.NextDouble() * 8f;

            petalPositions[index] =
                new Vector3D<float>(
                    treePosition.X + randomX,
                    randomY,
                    treePosition.Z + randomZ
                );

            petalFallSpeeds[index] = 0.6f + (float)random.NextDouble() * 1.2f;
            petalDriftOffsets[index] = (float)random.NextDouble() * 10f;
            petalCollected[index] = false;
        }

        private static void UpdatePetals(float deltaTime)
        {
            for (int i = 0; i < PetalCount; i++)
            {
                if (petalCollected[i])
                    continue;

                Vector3D<float> position = petalPositions[i];

                position.Y -= petalFallSpeeds[i] * deltaTime;
                position.X += MathF.Sin(sceneTime + petalDriftOffsets[i]) * 0.015f;
                position.Z += MathF.Cos(sceneTime + petalDriftOffsets[i]) * 0.015f;

                if (position.Y < 0.2f)
                {
                    ResetPetal(i);
                }
                else
                {
                    petalPositions[i] = position;
                }
            }
        }

        private static void CheckPetalCollection()
        {
            for (int i = 0; i < PetalCount; i++)
            {
                if (petalCollected[i])
                    continue;

                float distance = CalculateDistance(bunnyPosition, petalPositions[i]);

                if (distance < 1.4f)
                {
                    collectedPetals++;
                    ResetPetal(i);
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

        private static unsafe void DrawBunny()
        {
            Matrix4X4<float> scale = Matrix4X4.CreateScale(3f);
            Matrix4X4<float> rotation = Matrix4X4.CreateRotationY(bunnyRotationY);
            Matrix4X4<float> translation = Matrix4X4.CreateTranslation(bunnyPosition);

            Matrix4X4<float> modelMatrix = scale * rotation * translation;

            DrawTexturedObject(bunny, modelMatrix);
        }

        private static unsafe void DrawSakuraTrees()
        {
            for (int i = 0; i < TreeCount; i++)
            {
                Matrix4X4<float> scale = Matrix4X4.CreateScale(1.8f);
                Matrix4X4<float> rotation = Matrix4X4.CreateRotationY(i * 0.6f);
                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(treePositions[i]);

                Matrix4X4<float> modelMatrix = scale * rotation * translation;

                DrawTexturedObject(sakuraTree, modelMatrix);
            }
        }

        private static unsafe void DrawPetals()
        {
            for (int i = 0; i < PetalCount; i++)
            {
                if (petalCollected[i])
                    continue;

                Matrix4X4<float> scale = Matrix4X4.CreateScale(0.25f);
                Matrix4X4<float> rotationY = Matrix4X4.CreateRotationY(sceneTime + i);
                Matrix4X4<float> rotationZ = Matrix4X4.CreateRotationZ(sceneTime * 1.5f + i);
                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(petalPositions[i]);

                Matrix4X4<float> modelMatrix = scale * rotationY * rotationZ * translation;

                DrawTexturedObject(petal, modelMatrix);
            }
        }

        private static unsafe void DrawGardenGround()
        {
            Matrix4X4<float> scale = Matrix4X4.CreateScale(1f);
            Matrix4X4<float> rotation = Matrix4X4.CreateRotationX(MathF.PI / 2f);
            Matrix4X4<float> translation = Matrix4X4.CreateTranslation(0f, -0.05f, 0f);

            Matrix4X4<float> modelMatrix = scale * rotation * translation;

            DrawTexturedObject(ground, modelMatrix);
        }

        /*
        private static unsafe void DrawGardenGround()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateTranslation(0f, -0.05f, 0f);

            DrawTexturedObject(ground, modelMatrix);
        }
        */

        private static unsafe void DrawTexturedObject(GlObject obj, Matrix4X4<float> modelMatrix)
        {
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(obj.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);

            if (textureLocation == -1)
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");

            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, obj.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, obj.IndexArrayLength, GLEnum.UnsignedInt, null);

            Gl.BindVertexArray(0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            CheckError();
        }

        private static unsafe void DrawSkyBox()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(3500f);
            SetModelMatrix(modelMatrix);

            Gl.BindVertexArray(skyBox.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);

            if (textureLocation == -1)
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");

            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);

            Gl.BindVertexArray(0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            CheckError();
        }

        private static void DrawGui()
        {
            ImGui.Begin("Sakura Kert",
                ImGuiWindowFlags.AlwaysAutoResize);

            ImGui.Text("Gyujtott szirmok: " + collectedPetals);
            ImGui.Separator();
            ImGui.Text("Iranyitas:");
            ImGui.Text("W A S D - nyuszi mozgatasa");
            ImGui.Text("C - kamera nezet valtasa");
            ImGui.Separator();

            ImGui.Checkbox("Nyuszi szemszog", ref bunnyFirstPersonView);
            ImGui.SliderFloat("Nyuszi sebesseg", ref bunnySpeed, 0.05f, 0.8f);

            ImGui.End();
        }

        private static unsafe void SetUpObjects()
        {
            skyBox = GlCube.CreateInteriorCube(Gl, "skybox.png");

            bunny = ObjectResourceReader.CreateObjectWithTextureFromResource(
                Gl,
                "bunny.obj",
                "fur.jpg"
            );

            sakuraTree = ObjectResourceReader.CreateObjectWithTextureFromResource(
                Gl,
                "sakura_tree.obj",
                "petal.png"
            );

            petal = ObjectResourceReader.CreateObjectWithTextureFromResource(
                Gl,
                "bunny.obj",
                "petal.png"
            );

            ground = ObjectResourceReader.CreateObjectWithTextureFromResource(
                Gl,
                "ground.obj",
                 "grass.jpg",
                new float[] { 1f, 1f, 1f }
            );
        }

        private static void Window_Closing()
        {
            skyBox.ReleaseGlObject();
            bunny.ReleaseGlObject();
            sakuraTree.ReleaseGlObject();
            petal.ReleaseGlObject();
        }

        private static unsafe void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);

            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);

            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);

            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();

            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);

            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);

            if (status == 0)
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");

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

            using StreamReader shaderReader = new StreamReader(shaderStream);

            return shaderReader.ReadToEnd();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);

            if (location == -1)
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);

            Matrix4X4<float> modelMatrixWithoutTranslation =
                new Matrix4X4<float>(
                    modelMatrix.Row1,
                    modelMatrix.Row2,
                    modelMatrix.Row3,
                    modelMatrix.Row4
                );

            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInverse;
            Matrix4X4.Invert(modelMatrixWithoutTranslation, out modelInverse);

            Matrix3X3<float> normalMatrix =
                new Matrix3X3<float>(Matrix4X4.Transpose(modelInverse));

            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);

            if (location == -1)
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);

            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            Matrix4X4<float> viewMatrix =
                Matrix4X4.CreateLookAt(
                    cameraDescriptor.PositionInWorld,
                    cameraDescriptor.TargetInWorld,
                    cameraDescriptor.UpVector
                );

            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);

            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {
            Matrix4X4<float> projectionMatrix =
                Matrix4X4.CreatePerspectiveFieldOfView<float>(
                    (float)Math.PI / 2f,
                    1024f / 768f,
                    0.1f,
                    5000f
                );

            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);

            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");

            Gl.Uniform3(location, 1f, 0.85f, 0.95f);

            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");

            Gl.Uniform3(location, 0f, 20f, 10f);

            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");

            Gl.Uniform3(
                location,
                cameraDescriptor.PositionInWorld.X,
                cameraDescriptor.PositionInWorld.Y,
                cameraDescriptor.PositionInWorld.Z
            );

            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");

            Gl.Uniform1(location, Shininess);

            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();

            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}