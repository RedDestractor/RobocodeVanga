using System;
using System.Drawing;
using Robocode;

namespace RobotVanga
{
    // ReSharper disable once IdentifierTypo
    public class Vanga : Robocode.Robot
    {
        private double EnemyHeading { get; set; }
        private double EnemyBearing { get; set; }
        private double EnemyDistance { get; set; }
        private double EnemyVelocity { get; set; }
        public long LastShotTime { get; set; }
        private bool InvertDirection { get; set; }
        private bool IsTurning { get; set; }
        private bool IsRobotHitting { get; set; }
        public State State { get; set; }
        public State PreviousState { get; set; }

        public override void Run()
        {
            Start();

            while (true)
            {
                Update();
            }
        }

        public override void OnHitRobot(HitRobotEvent e)
        {
            IsRobotHitting = true;
        }

        public override void OnHitByBullet(HitByBulletEvent e)
        {
            PreviousState = State;
            State = State.HitByBullet;
        }

        public override void OnHitWall(HitWallEvent e)
        {
            if (Y > BattleFieldHeight * 0.5 && X > BattleFieldWidth * 0.5 ||
                Y < BattleFieldHeight * 0.5 && X < BattleFieldWidth * 0.5)
                TurnLeft(90);
            else
                TurnRight(90);

            PreviousState = State;
            State = State.Wall;
        }
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            if (e.Time - LastShotTime > 0.4)
            {
                EnemyBearing = e.Bearing;
                EnemyDistance = e.Distance;
                EnemyVelocity = e.Velocity;
                EnemyHeading = e.Heading;

                if (Math.Abs(GunHeat) < 0.2)
                {
                    var gunAngle = Heading - GunHeading + EnemyBearing;
                    var power = 500 / EnemyDistance;

                    if (power < 0.3) power = 0.3;

                    var addictionAngle = CalculateAddictionGunTurnAngle(power);
                    gunAngle = gunAngle.CalibrateAngle();

                    SetAllColors(Color.DimGray);

                    if (!Double.IsNaN(addictionAngle) && EnemyDistance > 10 /*&& EnemyVelocity > 1*/ && !IsTurning && !IsRobotHitting)
                    {
                        TurnGunRight(gunAngle + addictionAngle);
                    }
                    else if (EnemyDistance > 10)
                    {
                        TurnGunRight(gunAngle);
                    }

                    Fire(power);
                    SetAllColors(Color.Silver);
                    LastShotTime = e.Time;
                    IsRobotHitting = false;
                }
            }
        }

        public override void OnWin(WinEvent e)
        {
            SetAllColors(Color.Black);
        }

        private double CalculateAddictionGunTurnAngle(double firepower)
        {
            var bulletTime = EnemyDistance / (20 - 3 * firepower);
            var enemyPath = Math.Abs(EnemyVelocity * bulletTime);
            var dy = enemyPath * Math.Cos(EnemyHeading.ToRadians());
            var dx = enemyPath * Math.Sin(EnemyHeading.ToRadians());
            var enemyY = Y + EnemyDistance * Math.Cos((Heading + EnemyBearing % 360).ToRadians());
            var enemyX = X + EnemyDistance * Math.Sin((Heading + EnemyBearing % 360).ToRadians());
            var newEnemyX = enemyX + dx;
            var newEnemyY = enemyY + dy;
            var a1 = CalculateDistanceBetweenPoints(X, enemyX, Y, enemyY);
            var a2 = CalculateDistanceBetweenPoints(X, newEnemyX, Y, newEnemyY);
            var a3 = CalculateDistanceBetweenPoints(enemyX, newEnemyX, enemyY, newEnemyY);
            var angle = CalculateAngleBySides(a1, a2, a3);
            angle = GetDirection(X, enemyX, newEnemyX, Y, enemyY, newEnemyY) ? -angle : angle;

            return angle * 180 / Math.PI;
        }
        private bool GetDirection(double x1, double x2, double x3, double y1, double y2, double y3)
            => ((x2 - x1) * (y3 - y1) - (y2 - y1) * (x3 - x1)) * EnemyVelocity > 0;
        private static double CalculateAngleBySides(double a1, double a2, double a3)
            => Math.Acos((a1 * a1 + a2 * a2 - a3 * a3) / (2 * a1 * a2));

        private static double CalculateDistanceBetweenPoints(double x1, double x2, double y1, double y2)
            => Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

        private void Start()
        {
            SetColors(Color.Silver, Color.Silver, Color.Silver);
            IsAdjustGunForRobotTurn = true;
            IsAdjustRadarForRobotTurn = true;
        }

        private void Update()
        {
            Console.WriteLine(
                $"{nameof(State)}:{State.ToString()}");

            TurnRadarLeft(360);

            switch (State)
            {
                case State.Usual:
                    Ahead(50);
                    break;
                case State.Wall:
                    InvertDirection = !InvertDirection;
                    Ahead(InvertDirection ? -40 : 40);
                    PreviousState = State;
                    break;
                case State.Run:
                    Ahead(250);
                    PreviousState = State;
                    break;
                case State.HitByBullet:
                    IsTurning = true;
                    TurnRight((EnemyBearing + 90).CalibrateAngle());
                    IsTurning = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            State = State.Usual;
        }
    }

    public static class NumericExtensions
    {
        public static double ToRadians(this double angle) 
            => (Math.PI / 180) * angle;

        public static double CalibrateAngle(this double angle)
        {
            if (angle > 180) angle -= 360;
            else if (angle < -180) angle += 360;

            return angle;
        }
    }

    public enum State
    {
        Wall,
        Usual,
        Run,
        HitByBullet
    }
}