using Silk.NET.Maths;

using System.Numerics;

namespace GrafikaSzeminarium
{
    internal class CameraDescriptor
    {
        private double DistanceToOrigin = 10;

        private double EnabledDistanceFromOrigin = 1300;

        public float SpeedFactor = 0.1f;

        private double AngleToZYPlane = 0;

        private double AngleToZXPlane = 0;

        private const double DistanceScaleFactor = 1.01;

        private const double AngleChangeStepSize = Math.PI / 180;

        // a jarkalashoz
        private Vector3D<float> cameraPosition;
        private double yaw = -90;
        private double pitch = 0;

        public CameraDescriptor(Vector3D<float> pos)
        {
            cameraPosition = pos;
        }

        public void setCameraPosition(Vector3D<float> newPos)
        {
            cameraPosition = newPos;
        }

        // a kamera upvectora mindig felfele y-ba mutat
        public Vector3D<float> UpVector
        {
            get
            {
                return new Vector3D<float>(0f, 1f, 0f);
            }
        }


        // ezt akkor hasznaljuk ha bevan pipalva hogy a kamera az origoba nez
        public Vector3D<float> Position
        {
            get
            {
                return GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane);
            }
        }
        public Vector3D<float> Target
        {
            get
            {
                // For the moment the camera is always pointed at the origin.
                return Vector3D<float>.Zero;
            }
        }



        // eztet hasznaljuk ha nincs bepipalva hogy a kamera az origoba nez
        public Vector3D<float> PositionInWorld
        {
            get
            {
                return cameraPosition;
            }
        }
        // maskepp kell szamolni mert nem mindig az origoba nezunk itt
        public Vector3D<float> TargetInWorld
        {
            get
            {
                return cameraPosition + GetCameraFront();
            }
        }

        public Vector3D<float> RightVector
        {
            get { return Vector3D.Cross(GetCameraFront(), UpVector) * (SpeedFactor + 1f); }  // meg +1 kellett a SpeedFactorhoz mert hanem tul lassu volt
        }

        public void IncreaseZXAngle()
        {
            AngleToZXPlane += AngleChangeStepSize;
        }

        public void DecreaseZXAngle()
        {
            AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            AngleToZYPlane += AngleChangeStepSize;

        }

        public void DecreaseZYAngle()
        {
            AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            if (DistanceToOrigin + SpeedFactor < EnabledDistanceFromOrigin) DistanceToOrigin += SpeedFactor;
        }

        public void DecreaseDistance()
        {
            if (DistanceToOrigin - SpeedFactor < EnabledDistanceFromOrigin)
            {
                // hogy ne lehessen annyir kozel menni hogy atforduljon
                if (DistanceToOrigin - SpeedFactor < 0)
                {
                    DistanceToOrigin = 1;
                }
                else
                {
                    DistanceToOrigin -= SpeedFactor;
                }
            }
        }

        // a jarkalashoz kell
        public double Yaw
        {
            get { return yaw; }
            set { this.yaw = value; }
        }

        public double Pitch
        {
            get { return pitch; }
            set { this.pitch = value; }
        }

        // korlatnak van hogy ne menjunk ki a skyboxbol
        private double calculateDistanceFromOrigin(Vector3D<float> camerapoz)
        {
            return Vector3.Distance((Vector3)camerapoz, Vector3.Zero);
        }

        public void MoveBack()
        {
            // korlatnak van hogy ne menjunk ki a skyboxbol
            if (calculateDistanceFromOrigin(cameraPosition - GetCameraFront()) < EnabledDistanceFromOrigin) cameraPosition -= GetCameraFront();
        }

        public void MoveFront()
        {
            if (calculateDistanceFromOrigin(cameraPosition + GetCameraFront()) < EnabledDistanceFromOrigin) cameraPosition += GetCameraFront();
        }

        public void MoveLeft()
        {
            if (calculateDistanceFromOrigin(cameraPosition - RightVector) < EnabledDistanceFromOrigin) cameraPosition -= RightVector;
        }

        public void MoveRight()
        {
            if (calculateDistanceFromOrigin(cameraPosition + RightVector) < EnabledDistanceFromOrigin) cameraPosition += RightVector;
        }

        public void GoDown()
        {
            if (calculateDistanceFromOrigin(cameraPosition - this.UpVector * SpeedFactor) < EnabledDistanceFromOrigin) cameraPosition -= this.UpVector * SpeedFactor;
        }

        public void GoUp()
        {
            if (calculateDistanceFromOrigin(cameraPosition + this.UpVector * SpeedFactor) < EnabledDistanceFromOrigin) cameraPosition += this.UpVector * SpeedFactor;
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }

        // visszateritti hova nez a kamera eleje
        public Vector3D<float> GetCameraFront()
        {
            Vector3D<float> front;
            front.X = MathF.Cos((float)yaw * (MathF.PI / 180)) * MathF.Cos((float)pitch * (MathF.PI / 180));
            front.Y = MathF.Sin((float)pitch * (MathF.PI / 180));
            front.Z = MathF.Sin((float)yaw * (MathF.PI / 180)) * MathF.Cos((float)pitch * (MathF.PI / 180));

            front.X *= (float)SpeedFactor;
            front.Y *= (float)SpeedFactor;
            front.Z *= (float)SpeedFactor;

            //return Vector3D.Normalize(front); // nem kell a normalizalas hogy mukodjon a sebesseg, mert jol kiszamolja
            // a kamera merre nez a yaw es pitch fuggvenyeben es azt hozzaadjuk a cameraPositionhoz es akkor abba az iranyba
            // megyunk, es ezt megskalaztuk es ugy lesz sebessegunk
            return front;
        }
    }
}
