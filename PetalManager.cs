using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Maths;

namespace GrafikaSzeminarium
{
    public class PetalManager
    {
        private List<SakuraPetal> petals;
        private Random random;
        private float gardenLevel;
        private float spawnRadius;
        private int maxPetals;

        public PetalManager(float gardenLevel, float spawnRadius = 50f, int maxPetals = 15)
        {
            this.gardenLevel = gardenLevel;
            this.spawnRadius = spawnRadius;
            this.maxPetals = maxPetals;
            this.random = new Random();
            this.petals = new List<SakuraPetal>();

            SpawnInitialPetals();
        }

        private void SpawnInitialPetals()
        {
            for (int i = 0; i < maxPetals; i++)
            {
                petals.Add(CreatePetal(GenerateRandomPosition()));
            }
        }

        private SakuraPetal CreatePetal(Vector3 spawnPosition)
        {
            float driftSpeed = 0.1f + (float)random.NextDouble() * 0.6f;
            float swayAmplitude = 0.2f + (float)random.NextDouble() * 0.5f;
            float swayFrequency = 0.2f + (float)random.NextDouble() * 0.4f;
            float spinSpeed = ((float)random.NextDouble() - 0.5f) * 0.8f;
            float floatPhase = (float)(random.NextDouble() * Math.PI * 2);
            float floatAmplitude = 0.1f + (float)random.NextDouble() * 0.3f;
            float floatSpeed = 0.3f + (float)random.NextDouble() * 0.8f;
            float circularRadius = 0.3f + (float)random.NextDouble() * 1.0f;
            float circularSpeed = 0.1f + (float)random.NextDouble() * 0.4f;
            Vector2 driftDir = GenerateRandomDirection();

            return new SakuraPetal(spawnPosition, driftSpeed, swayAmplitude, swayFrequency,
                spinSpeed, floatPhase, floatAmplitude, floatSpeed,
                circularRadius, circularSpeed, driftDir);
        }

        private Vector2 GenerateRandomDirection()
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        private Vector3 GenerateRandomPosition()
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = 10f + (float)random.NextDouble() * (spawnRadius - 10f);
            float x = (float)Math.Cos(angle) * distance;
            float z = (float)Math.Sin(angle) * distance;
            return new Vector3(x, gardenLevel, z);
        }

        private Vector3 GenerateRandomPositionNearBunny(Vector3 bunnyPosition)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = 15f + (float)random.NextDouble() * (spawnRadius - 15f);
            float x = bunnyPosition.X + (float)Math.Cos(angle) * distance;
            float z = bunnyPosition.Z + (float)Math.Sin(angle) * distance;
            return new Vector3(x, gardenLevel, z);
        }

        public void Update(double deltaTime, Vector3 bunnyPosition)
        {
            foreach (var petalPetal in petals)
            {
                petalPetal.Update(deltaTime);

                float distanceToBunny = Vector3.Distance(petalPetal.Position, bunnyPosition);
                if (distanceToBunny > spawnRadius * 1.5f)
                {
                    petalPetal.SetBasePosition(GenerateRandomPositionNearBunny(bunnyPosition));
                }
            }
        }

        public List<Matrix4X4<float>> GetPetalMatrices(float scale = 0.3f)
        {
            var matrices = new List<Matrix4X4<float>>();
            foreach (var petalPetal in petals)
            {
                var scaleMatrix = Matrix4X4.CreateScale(scale);
                var rotationMatrix = Matrix4X4.CreateRotationY(petalPetal.SpinAngle);
                var translationMatrix = Matrix4X4.CreateTranslation(
                    petalPetal.Position.X, petalPetal.Position.Y, petalPetal.Position.Z);
                matrices.Add(scaleMatrix * rotationMatrix * translationMatrix);
            }
            return matrices;
        }

        public bool CheckBunnyCollision(Vector3 bunnyPosition, float collisionRadius = 2f)
        {
            for (int i = petals.Count - 1; i >= 0; i--)
            {
                float distance = Vector3.Distance(petals[i].Position, bunnyPosition);
                if (distance < collisionRadius)
                {
                    petals.RemoveAt(i);
                    petals.Add(CreatePetal(GenerateRandomPosition()));
                    return true;
                }
            }
            return false;
        }

        public int PetalCount => petals.Count;

        public List<SakuraPetal> GetPetals() => petals;
    }

    public class SakuraPetal
    {
        public Vector3 Position { get; set; }
        public float SpinAngle { get; private set; }

        private Vector3 basePosition;
        private float driftSpeed;
        private float swayAmplitude;
        private float swayFrequency;
        private float spinSpeed;
        private float floatPhase;
        private float floatAmplitude;
        private float floatSpeed;
        private float circularRadius;
        private float circularSpeed;
        private Vector2 driftDirection;
        private Vector3 circularCenter;
        private double swayTimeOffset;
        private double timeAccumulator;

        public SakuraPetal(Vector3 position, float driftSpeed, float swayAmplitude, float swayFrequency,
            float spinSpeed, float floatPhase, float floatAmplitude, float floatSpeed,
            float circularRadius, float circularSpeed, Vector2 driftDirection)
        {
            Position = position;
            basePosition = position;
            circularCenter = position;
            this.driftSpeed = driftSpeed;
            this.swayAmplitude = swayAmplitude;
            this.swayFrequency = swayFrequency;
            this.spinSpeed = spinSpeed;
            this.floatPhase = floatPhase;
            this.floatAmplitude = floatAmplitude;
            this.floatSpeed = floatSpeed;
            this.circularRadius = circularRadius;
            this.circularSpeed = circularSpeed;
            this.driftDirection = driftDirection;
            this.swayTimeOffset = new Random().NextDouble() * Math.PI * 2;
            this.timeAccumulator = 0;
            this.SpinAngle = 0f;
        }

        public void Update(double deltaTime)
        {
            timeAccumulator += deltaTime;
            float dt = (float)deltaTime;

            // Sodródás
            Vector3 drift = new Vector3(
                driftDirection.X * driftSpeed * dt,
                0,
                driftDirection.Y * driftSpeed * dt);

            // Körkörös mozgás (levél sodródik a szélben)
            float circAngle = (float)(timeAccumulator * circularSpeed);
            Vector3 circOffset = new Vector3(
                (float)Math.Cos(circAngle) * circularRadius,
                0,
                (float)Math.Sin(circAngle) * circularRadius);

            // Hintázás (oldalirányú)
            float sway = (float)Math.Sin(timeAccumulator * swayFrequency + swayTimeOffset) * swayAmplitude;
            Vector3 swayMovement = new Vector3(
                sway * driftDirection.Y,
                0,
                -sway * driftDirection.X);

            // Lebegés fel-le
            float floatOffset = (float)Math.Sin(timeAccumulator * floatSpeed + floatPhase) * floatAmplitude;
            float secondaryFloat = (float)Math.Sin(timeAccumulator * floatSpeed * 1.7f + floatPhase * 0.3f) * (floatAmplitude * 0.2f);

            Vector3 newPos = basePosition + drift + circOffset + swayMovement;
            newPos.Y = basePosition.Y + floatOffset + secondaryFloat;
            Position = newPos;

            // Pörgés
            float spinWobble = (float)Math.Sin(timeAccumulator * 2.0f) * 0.05f;
            SpinAngle += (spinSpeed + spinWobble) * dt;
            if (SpinAngle > Math.PI * 2) SpinAngle -= (float)(Math.PI * 2);
            else if (SpinAngle < 0) SpinAngle += (float)(Math.PI * 2);

            basePosition += drift;
            circularCenter += drift;
        }

        public void SetBasePosition(Vector3 newPosition)
        {
            Vector3 offset = newPosition - basePosition;
            Position = newPosition;
            basePosition = newPosition;
            circularCenter += offset;
        }
    }
}