using Silk.NET.Maths;

namespace GrafikaSzeminarium
{
    internal class MovingObstacle
    {
        public Vector3D<float> Position { get; private set; }

        private Vector3D<float> startPosition;

        private float movementRange;

        private float movementSpeed;

        private bool movingRight = true;

        public MovingObstacle(
            Vector3D<float> position,
            float range,
            float speed)
        {
            Position = position;
            startPosition = position;
            movementRange = range;
            movementSpeed = speed;
        }

        public void Update(float deltaTime)
        {
            if (movingRight)
            {
                Position += new Vector3D<float>(
                    movementSpeed * deltaTime,
                    0f,
                    0f
                );

                if (Position.X >= startPosition.X + movementRange)
                    movingRight = false;
            }
            else
            {
                Position -= new Vector3D<float>(
                    movementSpeed * deltaTime,
                    0f,
                    0f
                );

                if (Position.X <= startPosition.X - movementRange)
                    movingRight = true;
            }
        }

        public bool CollidesWith(Vector3D<float> playerPosition, float radius)
        {
            float dx = Position.X - playerPosition.X;
            float dy = Position.Y - playerPosition.Y;
            float dz = Position.Z - playerPosition.Z;

            float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            return distance < radius;
        }
    }
}