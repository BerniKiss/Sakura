using System;
using System.Numerics;
using Silk.NET.Input;

namespace GrafikaSzeminarium
{
    public class BunnyController
    {
        private Vector3 position;
        private float rotation; // Y tengely körüli forgatás radiánban
        private float speed = 4.0f;
        private float rotationSpeed = 2.0f;

        private bool isHoppingForward = false;
        private bool isHoppingBackward = false;
        private bool isTurningLeft = false;
        private bool isTurningRight = false;
        private bool isStrafingLeft = false;
        private bool isStrafingRight = false;

        public Vector3 Position => position;
        public float Rotation => rotation;

        public float Speed
        {
            get => speed;
            set => speed = Math.Max(0, value);
        }

        public float RotationSpeed
        {
            get => rotationSpeed;
            set => rotationSpeed = Math.Max(0, value);
        }

        public BunnyController(Vector3 initialPosition, float initialRotation = 0f)
        {
            position = new Vector3(initialPosition.X, initialPosition.Y, initialPosition.Z);
            rotation = initialRotation + (float)(Math.PI / 2);
        }

        public void HandleKeyDown(Key key)
        {
            switch (key)
            {
                case Key.A: isHoppingForward = true; break;
                case Key.D: isHoppingBackward = true; break;
                case Key.W: isStrafingLeft = true; break;
                case Key.S: isStrafingRight = true; break;
                case Key.J: isTurningLeft = true; break;
                case Key.L: isTurningRight = true; break;
            }
        }

        public void HandleKeyUp(Key key)
        {
            switch (key)
            {
                case Key.A: isHoppingForward = false; break;
                case Key.D: isHoppingBackward = false; break;
                case Key.W: isStrafingLeft = false; break;
                case Key.S: isStrafingRight = false; break;
                case Key.J: isTurningLeft = false; break;
                case Key.L: isTurningRight = false; break;
            }
        }

        public void Update(double deltaTime)
        {
            float dt = (float)deltaTime;

            if (isTurningLeft) rotation += rotationSpeed * dt;
            if (isTurningRight) rotation -= rotationSpeed * dt;

            rotation = rotation % (2 * MathF.PI);
            if (rotation < 0) rotation += 2 * MathF.PI;

            Vector3 hop = Vector3.Zero;
            float movementAngle = rotation + (float)(Math.PI / 2);

            if (isHoppingForward)
            {
                hop.X += MathF.Sin(movementAngle) * speed * dt;
                hop.Z += MathF.Cos(movementAngle) * speed * dt;
            }
            if (isHoppingBackward)
            {
                hop.X -= MathF.Sin(movementAngle) * speed * dt;
                hop.Z -= MathF.Cos(movementAngle) * speed * dt;
            }
            if (isStrafingLeft)
            {
                hop.X += MathF.Cos(movementAngle) * speed * dt;
                hop.Z -= MathF.Sin(movementAngle) * speed * dt;
            }
            if (isStrafingRight)
            {
                hop.X -= MathF.Cos(movementAngle) * speed * dt;
                hop.Z += MathF.Sin(movementAngle) * speed * dt;
            }

            // Y tengely nem változik – a nyuszi a talajon marad
            position = new Vector3(position.X + hop.X, position.Y, position.Z + hop.Z);
        }

        public Matrix4x4 GetModelMatrix(float scale = 0.5f)
        {
            float adjustedRotation = rotation + (float)(Math.PI / 2);
            return Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateRotationY(adjustedRotation) *
                   Matrix4x4.CreateTranslation(position);
        }

        public Vector3 GetForwardDirection()
        {
            return new Vector3(MathF.Sin(rotation), 0, MathF.Cos(rotation));
        }

        public Vector3 GetRightDirection()
        {
            return new Vector3(MathF.Cos(rotation), 0, -MathF.Sin(rotation));
        }

        public void SetPosition(Vector3 newPosition)
        {
            position = new Vector3(newPosition.X, position.Y, newPosition.Z);
        }

        public void SetRotation(float newRotation)
        {
            rotation = newRotation % (2 * MathF.PI);
            if (rotation < 0) rotation += 2 * MathF.PI;
        }

        public void Reset(Vector3 resetPosition, float resetRotation = 0f)
        {
            position = new Vector3(resetPosition.X, resetPosition.Y, resetPosition.Z);
            rotation = resetRotation;
            isHoppingForward = false;
            isHoppingBackward = false;
            isStrafingLeft = false;
            isStrafingRight = false;
            isTurningLeft = false;
            isTurningRight = false;
        }
    }
}