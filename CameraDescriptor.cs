using Silk.NET.Maths;

namespace GrafikaSzeminarium
{
    internal class CameraDescriptor
    {
        private Vector3D<float> position;

        public double HorizontalAngle { get; set; } = -90;
        public double VerticalAngle { get; set; } = 0;

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
            get { return position + ComputeFrontVector(); }
        }

        public Vector3D<float> UpVector
        {
            get { return new Vector3D<float>(0f, 1f, 0f); }
        }

        private Vector3D<float> ComputeFrontVector()
        {
            float radH = (float)(HorizontalAngle * MathF.PI / 180f);
            float radV = (float)(VerticalAngle * MathF.PI / 180f);

            Vector3D<float> frontVec = new Vector3D<float>
            {
                X = MathF.Cos(radH) * MathF.Cos(radV),
                Y = MathF.Sin(radV),
                Z = MathF.Sin(radH) * MathF.Cos(radV)
            };

            return Vector3D.Normalize(frontVec);
        }
    }
}