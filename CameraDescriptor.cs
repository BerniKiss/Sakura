using Silk.NET.Maths;

namespace GrafikaSzeminarium
{
    internal class CameraDescriptor
    {
        private Vector3D<float> position;

        public double Yaw { get; set; } = -90;
        public double Pitch { get; set; } = 0;

        public CameraDescriptor(Vector3D<float> startPosition)
        {
            position = startPosition;
        }

        public void setCameraPosition(Vector3D<float> newPosition)
        {
            position = newPosition;
        }

        public Vector3D<float> Position
        {
            get { return position; }
        }

        public Vector3D<float> Target
        {
            get { return position + GetCameraFront(); }
        }

        public Vector3D<float> UpVector
        {
            get { return new Vector3D<float>(0f, 1f, 0f); }
        }

        private Vector3D<float> GetCameraFront()
        {
            Vector3D<float> front;

            front.X = MathF.Cos((float)Yaw * MathF.PI / 180f) *
                      MathF.Cos((float)Pitch * MathF.PI / 180f);

            front.Y = MathF.Sin((float)Pitch * MathF.PI / 180f);

            front.Z = MathF.Sin((float)Yaw * MathF.PI / 180f) *
                      MathF.Cos((float)Pitch * MathF.PI / 180f);

            return Vector3D.Normalize(front);
        }
    }
}