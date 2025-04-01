extern alias SC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using SharpDX;

namespace EVESharpCore.Framework
{
    public class DirectWorldPosition
    {
        public double XCoordinate { get; set; }
        public double YCoordinate { get; set; }
        public double ZCoordinate { get; set; }

        public bool DirectPathFlag { get; set; }
        public int Visits { get; set; }

        public override string ToString()
        {
            return $"{XCoordinate}|{YCoordinate}|{ZCoordinate}";
        }

        public bool IsXYZCoordValid
        {
            get
            {
                if (XCoordinate == 0)
                    return false;

                if (YCoordinate == 0)
                    return false;

                if (ZCoordinate == 0)
                    return false;

                return true;
            }
        }

        public Vec3 PositionInSpace
        {
            get
            {
                return new Vec3(XCoordinate, YCoordinate, ZCoordinate);
            }
        }

        private DirectEve DirectEve { get; set; }



        public DirectWorldPosition(double x, double y, double z, DirectEve directEve = null)
        {
            this.DirectEve = directEve;
            this.XCoordinate = x;
            this.YCoordinate = y;
            this.ZCoordinate = z;
            CreationTime = DateTime.UtcNow;
        }

        public DirectWorldPosition(Vec3 vec3, DirectEve directEve = null)
        {
            this.DirectEve = directEve;
            this.XCoordinate = vec3.X;
            this.YCoordinate = vec3.Y;
            this.ZCoordinate = vec3.Z;
            CreationTime = DateTime.UtcNow;
        }

        DateTime CreationTime;

        public DirectWorldPosition(double x, double y, double z, bool flag)
        {
            this.XCoordinate = x;
            this.YCoordinate = y;
            this.ZCoordinate = z;
            this.DirectPathFlag = flag;
        }

        public override bool Equals(object obj)
        {
            var tolerance = 0.5;
            if (object.ReferenceEquals(obj, null))
            {
                return false;
            }
            if (obj is DirectWorldPosition k)
            {
                return Math.Abs(XCoordinate - k.XCoordinate) < tolerance && Math.Abs(YCoordinate - k.YCoordinate) < tolerance && Math.Abs(ZCoordinate - k.ZCoordinate) < tolerance;
            }
            return false;
        }

        public bool Equals(object obj, double tolerance)
        {
            return Equals(obj);
        }

        public static bool operator ==(DirectWorldPosition a, DirectWorldPosition b)
        {

            if (object.ReferenceEquals(a, null))
            {
                if (object.ReferenceEquals(b, null))
                    return true;

                return false;
            }

            return a?.Equals(b) ?? false;
        }

        public static bool operator !=(DirectWorldPosition a, DirectWorldPosition b)
        {
            return !(a == b);
        }

        public static double GetDistance(DirectWorldPosition from, DirectWorldPosition to)
        {
            var deltaX = to.XCoordinate - from.XCoordinate;
            var deltaY = to.YCoordinate - from.YCoordinate;
            var deltaZ = to.ZCoordinate - from.ZCoordinate;
            var distance = Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY +
                deltaZ * deltaZ);
            return distance;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + XCoordinate.GetHashCode();
                hash = hash * 23 + YCoordinate.GetHashCode();
                hash = hash * 23 + ZCoordinate.GetHashCode();
                return hash;
            }
        }

        public static void DrawPath(List<DirectWorldPosition> path, bool clearDebugLines = true, bool skipFirst = true, bool isCircular = true)
        {

            if (path.Count < 2)
                return;

            var me = ESCache.Instance.DirectEve.ActiveShip.Entity;
            if (me != null)
            {
                var meWorldPos = me.DirectAbsolutePosition;

                if (clearDebugLines)
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();

                var prev = me.BallPos;

                if (isCircular)
                    path.Add(path.First());

                var n = 0;
                foreach (var waypoint in path)
                {
                    var wpPos = meWorldPos.GetDirectionalVectorTo(waypoint);
                    if (n != 0 && skipFirst)
                        ESCache.Instance.DirectEve.SceneManager.DrawLine(prev, wpPos);
                    prev = wpPos;
                    n++;
                }
            }
        }

        private static Random _rnd = new Random();


        public static void OnSessionChange()
        {
            DirectEntity.AStarErrors = 0;
            _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
            _orbitCacheXYZ = string.Empty;
        }

        private static Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)> _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();

        private static Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)> _speedTankPathCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();

        public static string _orbitCacheXYZ = string.Empty;

        public bool Orbit(double radius, bool clearDebugLines = false, bool humanize = true, Vec3? radiusVector = null, Range<double> humanizeFactor = null)
        {
            var key = _orbitCache.Keys.Where(e => this.Equals(e, 2500)).FirstOrDefault();
            var activeShip = ESCache.Instance.DirectEve.ActiveShip.Entity;
            if (key != null && _orbitCache[key].Item1 == radius && _orbitCache[key].Item2.Any())
            {
                //if (!_orbitCache[this].Item2.Any())
                //{
                //    // If it exists already, re-use the previously generated path
                //    _orbitCache[this] = (radius, _orbitCache[this].Item3.ToList(), _orbitCache[this].Item3.ToList());
                //}
            }
            else
            {
                // Generate a path if there is none yet
                var p = GetRandomOrbitPath(radius, 10, humanize, radiusVector, humanizeFactor);
                // Find the closest point to us and start from there
                var closest = p.OrderBy(e => e.GetDistance(activeShip.DirectAbsolutePosition)).First();
                var index = p.IndexOf(closest);
                p = p.Skip(index).Concat(p.Take(index)).ToList();

                // Ensure we go the correct way around the orbit, the last point should be the second closes else reverse
                if (p.Last().GetDistance(activeShip.DirectAbsolutePosition) > p[1].GetDistance(activeShip.DirectAbsolutePosition))
                {
                    p.Reverse();
                }

                _orbitCache[this] = (radius, p.ToList(), p.ToList());
                key = this;
                ESCache.Instance.DirectEve.Log($"Generated a new path with length {p.Count} for {this} with radius {radius}.");
            }

            var entry = _orbitCache[key];

            // The current path, where we also remove items from
            var path = entry.Item2;
            if (path != null && path.Any())
            {
                var current = path.FirstOrDefault();

                // If we are too far away, return false
                if (current.GetDistance(activeShip.DirectAbsolutePosition) > radius * 1.5)
                {
                    if (DirectEve.Interval(6000)) ESCache.Instance.DirectEve.Log("We are too far away.");
                    _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
                    _orbitCacheXYZ = XCoordinate.ToString() + YCoordinate.ToString() + ZCoordinate.ToString();
                    return false;
                }

                if (_orbitCacheXYZ != XCoordinate.ToString() + YCoordinate.ToString() + ZCoordinate.ToString())
                {
                    if (DirectEve.Interval(6000)) ESCache.Instance.DirectEve.Log("XYZ coord have changed");
                    _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
                    _orbitCacheXYZ = XCoordinate.ToString() + YCoordinate.ToString() + ZCoordinate.ToString();
                    return false;
                }

                // Draw the original path
                DrawPath(entry.Item3.ToList(), clearDebugLines: clearDebugLines);

                // If we are close pick the next wp and move to it
                if (current.GetDistance(activeShip.DirectAbsolutePosition) < Math.Min(radius / 2, 50_000))
                {
                    if (DirectEve.Interval(10000))
                        ESCache.Instance.DirectEve.Log($"Removed a waypoint. {current}. Remaining [{path.Count}]");
                    path.Remove(current);
                    current = path.FirstOrDefault();

                }

                if (current != null)
                {
                    ESCache.Instance.DirectEve.ActiveShip.MoveTo(current);
                    return true;
                }

                return false;
            }
            else _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();

            return false;
        }

        private static int _preventedSameDirectionCount = 0;

        public bool MoveTo()
        {
            var x = XCoordinate;
            var y = YCoordinate;
            var z = ZCoordinate;

            double minPerc = 0.01d;
            double maxPerc = 0.03d;

            var currentDirectionVector = ESCache.Instance.ActiveShip.Entity.GetDirectionVectorFinal();

            //DirectEve.SceneManager.ClearDebugLines();
            DirectEve.SceneManager.DrawLineGradient(new Vec3(0, 0, 0), currentDirectionVector.Scale(15000), new System.Numerics.Vector4(1, 0, 1, 1), new System.Numerics.Vector4(0, 0.5f, 0.5f, 0.5f));

            if (currentDirectionVector != null)
            {

                //DirectEve.Log($"currentDirectionVector {currentDirectionVector}");
                //DirectEve.Log($"XAbs [{Math.Abs(x - currentDirectionVector.X)}] YAbs [{Math.Abs(y - currentDirectionVector.Y)}] ZAbs [{Math.Abs(z - currentDirectionVector.Z)}]");
                if (
                    _preventedSameDirectionCount <= 3
                    && Math.Abs(x - currentDirectionVector.X) <= maxPerc
                    && Math.Abs(y - currentDirectionVector.Y) <= maxPerc
                    && Math.Abs(z - currentDirectionVector.Z) <= maxPerc
                    )
                {
                    if (DirectEve.Interval(3000, 6000))
                        _preventedSameDirectionCount++;
                    if (DirectEve.Interval(5000))
                    {
                        Log.WriteLine($"-- MoveTo skipping. We are already moving into that direction. _preventedSameDirectionCount {_preventedSameDirectionCount}");

                    }
                    return false;
                }
            }
            else
            {
                DirectEve.Log($"Warning: currentDirectionVector == null");
            }
            _preventedSameDirectionCount = 0;

            if (Math.Abs(x) < minPerc)
                x = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1); // x = 0.01 ... 0.03

            if (Math.Abs(y) < minPerc)
                y = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);  // y = 0.01 ... 0.03

            if (Math.Abs(z) < minPerc)
                z = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1); // z = 0.01 ... 0.03

            x = x + (_rnd.NextDouble() * (x * maxPerc - x * minPerc) + x * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            y = y + (_rnd.NextDouble() * (y * maxPerc - y * minPerc) + y * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            z = z + (_rnd.NextDouble() * (z * maxPerc - z * minPerc) + z * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);

            if (ESCache.Instance.ActiveShip.IsImmobile)
                return false;

            if (!DirectEve.Interval(1500, 3100))
                return false;

            var unitVec = new Vec3(x, y, z).Normalize();

            if (!unitVec.IsUnitVector(0.00001d))
            {
                Console.WriteLine("Error: MoveTo -- Is not a unit vector");
                DirectEve.Log("Error: MoveTo -- Is not a unit vector");
                return false;
            }

            DirectEve.Log($"-- Framework MoveTo [{unitVec}] ");

            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("michelle").Call("GetRemotePark").Attribute("CmdGotoDirection"), unitVec.X, unitVec.Y, unitVec.Z);
        }

        public bool OrbitWithHighTransversal(double radius, DirectEntity _directEntity = null)
        {
            if (_directEntity != null)
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    {
                        if (_directEntity.IsNPCBattlecruiser)
                        {
                            if (30000 > _directEntity.Distance)
                            {
                                //orbit here
                                _directEntity.Orbit((int)radius, false);
                                return true;
                            }
                        }
                    }
                }
            }

            if (_orbitCache.ContainsKey(this) && _orbitCache[this].Item1 == radius)
            {
                if (!_orbitCache[this].Item2.Any())
                {
                    // If it exists already, re-use the previously generated path
                    if (!DirectEve.Interval(30000)) _orbitCache[this] = (radius, _orbitCache[this].Item3.ToList(), _orbitCache[this].Item3.ToList());
                }
            }
            else
            {
                int numPoints = 6;
                bool humanize = true;
                bool checkIsSpotWithinAbyssalBounds = true;
                bool checkIsSpotTooCloseToNPCs = true;
                // Generate a path if there is none yet
                var p = GetHighTransversalOrbitPath(radius, numPoints, humanize, checkIsSpotTooCloseToNPCs, checkIsSpotWithinAbyssalBounds);
                if (p.Count() == 0)
                {
                    numPoints = 6;
                    humanize = false;
                    checkIsSpotWithinAbyssalBounds = true;
                    checkIsSpotTooCloseToNPCs = true;
                    ESCache.Instance.DirectEve.Log($"Generated a new high transversal path with length [" + p.Count + "] numPoints [" + numPoints + "] radius [" + radius + "] checkIsSpotWithinAbyssalBounds [" + checkIsSpotWithinAbyssalBounds + "] checkIsSpotTooCloseToNPCs [" + checkIsSpotTooCloseToNPCs + "] - this is bad!");
                    p = GetHighTransversalOrbitPath(radius, numPoints, humanize, checkIsSpotTooCloseToNPCs, checkIsSpotWithinAbyssalBounds);
                    if (p.Count() == 0)
                    {
                        numPoints = 12;
                        humanize = false;
                        checkIsSpotWithinAbyssalBounds = true;
                        checkIsSpotTooCloseToNPCs = false;
                        ESCache.Instance.DirectEve.Log($"Generated a new high transversal path with length [" + p.Count + "] numPoints [" + numPoints + "] radius [" + radius + "] checkIsSpotWithinAbyssalBounds [" + checkIsSpotWithinAbyssalBounds + "] checkIsSpotTooCloseToNPCs [" + checkIsSpotTooCloseToNPCs + "] - this is bad!!");
                        p = GetHighTransversalOrbitPath(radius, numPoints, humanize, checkIsSpotTooCloseToNPCs, checkIsSpotWithinAbyssalBounds);
                        if (p.Count() == 0)
                        {
                            numPoints = 12;
                            humanize = false;
                            checkIsSpotWithinAbyssalBounds = false;
                            checkIsSpotTooCloseToNPCs = false;
                            ESCache.Instance.DirectEve.Log($"Generated a new high transversal path with length [" + p.Count + "] numPoints [" + numPoints + "] radius [" + radius + "] checkIsSpotWithinAbyssalBounds [" + checkIsSpotWithinAbyssalBounds + "] checkIsSpotTooCloseToNPCs [" + checkIsSpotTooCloseToNPCs + "] - this very is bad");
                            p = GetHighTransversalOrbitPath(radius, numPoints, humanize, checkIsSpotTooCloseToNPCs, checkIsSpotWithinAbyssalBounds);
                        }
                    }
                }

                _orbitCache[this] = (radius, p.ToList(), p.ToList());
                ESCache.Instance.DirectEve.Log($"Generated a new high transversal path with length [" + p.Count + "] numPoints [" + numPoints + "] radius [" + radius + "] checkIsSpotWithinAbyssalBounds [" + checkIsSpotWithinAbyssalBounds + "] checkIsSpotTooCloseToNPCs [" + checkIsSpotTooCloseToNPCs + "]");
            }

            var entry = _orbitCache[this];

            // The current path, where we also remove items from
            var path = entry.Item2;
            if (path != null && path.Any())
            {
                var activeShip = ESCache.Instance.DirectEve.ActiveShip.Entity;
                var current = path.FirstOrDefault();

                // Draw the original path
                if (DirectEve.Interval(1000, 1000, XCoordinate.ToString() + YCoordinate.ToString() + ZCoordinate.ToString()))
                {
                    DrawPath(entry.Item3.ToList());
                }
                else DrawPath(entry.Item3.ToList(), DebugConfig.ClearDebugLines);

                // If we are too far away, return false
                if (current.GetDistance(activeShip.DirectAbsolutePosition) > radius * 20)
                {
                    ESCache.Instance.DirectEve.Log("We are [" + current.GetDistance(activeShip.DirectAbsolutePosition) + "]m away.");
                    _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
                    if (50000 > current.GetDistance(activeShip.DirectAbsolutePosition))
                    {
                        ESCache.Instance.DirectEve.Log("We are [" + current.GetDistance(activeShip.DirectAbsolutePosition) + "]m away. MoveToViaAStar");
                        DirectEntity.MoveToViaAStar(stepSize: 2000, distanceToTarget: 12500,
                        forceRecreatePath: false,
                        dest: this,
                        ignoreAbyssEntities: false,
                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                        ignoreWideAreaAutomataPylon: false,
                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                        return false;
                    }

                    if (120000 > current.GetDistance(activeShip.DirectAbsolutePosition))
                    {
                        ESCache.Instance.DirectEve.Log("We are [" + current.GetDistance(activeShip.DirectAbsolutePosition) + "]m away. MoveTo()");
                        MoveTo();
                        return false;
                    }

                    ESCache.Instance.DirectEve.Log("We are [" + current.GetDistance(activeShip.DirectAbsolutePosition) + "]m away. MoveTo().....");
                    MoveTo();
                    return false;
                }

                // If we are close pick the next wp and move to it
                if (current.GetDistance(activeShip.DirectAbsolutePosition) < (radius / 2))
                {
                    if (DirectEve.Interval(10000, 10000, XCoordinate.ToString() + YCoordinate.ToString() + ZCoordinate.ToString()))
                        ESCache.Instance.DirectEve.Log($"Removed a waypoint. {current}. Remaining [{path.Count}]");
                    path.Remove(current);
                    current = path.FirstOrDefault();

                }

                if (current != null)
                {
                    DirectEntity.MoveToViaAStar(stepSize: 2000, distanceToTarget: 12500,
                    forceRecreatePath: false,
                    dest: current,
                    ignoreAbyssEntities: false,
                    ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                    ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                    ignoreWideAreaAutomataPylon: false,
                    ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                    ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                    ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                    return true;
                }

                return false;
            }
            else
            {
                _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
                Log.WriteLine("No path found. how?! can we do a regular orbit instead?");
                if (_directEntity != null)
                {
                    _directEntity.Orbit((int)radius, false);
                    return true;
                }
            }

            return false;
        }

        public bool OrbitWithHighTransversalIgnoreTowers(double radius)
        {
            if (_orbitCache.ContainsKey(this) && _orbitCache[this].Item1 == radius)
            {
                //if (!_orbitCache[this].Item2.Any())
                //{
                //    // If it exists already, re-use the previously generated path
                //    _orbitCache[this] = (radius, _orbitCache[this].Item3.ToList(), _orbitCache[this].Item3.ToList());
                //}
            }
            else
            {
                // Generate a path if there is none yet
                int numPoints = 6;
                bool humanize = true;
                bool checkIsSpotWithinAbyssalBounds = true;
                bool checkIsSpotTooCloseToNPCs = true;
                var p = GetHighTransversalOrbitPath(radius, numPoints, humanize, checkIsSpotTooCloseToNPCs, checkIsSpotWithinAbyssalBounds);
                _orbitCache[this] = (radius, p.ToList(), p.ToList());
                ESCache.Instance.DirectEve.Log($"Generated a new high transversal path with length [" + p.Count + "] numPoints [" + numPoints + "] radius [" + radius + "] checkIsSpotWithinAbyssalBounds [" + checkIsSpotWithinAbyssalBounds + "] checkIsSpotTooCloseToNPCs [" + checkIsSpotTooCloseToNPCs + "]");
            }

            var entry = _orbitCache[this];

            // The current path, where we also remove items from
            var path = entry.Item2;
            if (path != null && path.Any())
            {
                var activeShip = ESCache.Instance.DirectEve.ActiveShip.Entity;
                var current = path.FirstOrDefault();

                // If we are too far away, return false
                if (current.GetDistance(activeShip.DirectAbsolutePosition) > radius * 10)
                {
                    ESCache.Instance.DirectEve.Log("We are too far away.");
                    _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
                    return false;
                }

                // Draw the original path
                DrawPath(entry.Item3.ToList());

                // If we are close pick the next wp and move to it
                if (current.GetDistance(activeShip.DirectAbsolutePosition) < (radius / 2))
                {
                    if (DirectEve.Interval(10000))
                        ESCache.Instance.DirectEve.Log($"Removed a waypoint. {current}. Remaining [{path.Count}]");
                    path.Remove(current);
                    current = path.FirstOrDefault();

                }

                if (current != null)
                {
                    DirectEntity.MoveToViaAStar(stepSize: 2000, distanceToTarget: 12500,
                    forceRecreatePath: false,
                    dest: current,
                    ignoreAbyssEntities: false,
                    ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                    ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                    ignoreWideAreaAutomataPylon: true,
                    ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                    ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                    ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                    return true;
                }

                return false;
            }
            else _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();

            return false;
        }

        private double RandomBetween(double smallNumber, double bigNumber)
        {
            return _rnd.NextDouble() * (bigNumber-smallNumber) + (smallNumber);
        }

        public List<DirectWorldPosition> GetRandomOrbitPath(double radius, int numPoints, bool humanize = true, Vec3? radiusVector = null, Range<double> humanizeFactor = null)
        {
            List<DirectWorldPosition> path = new List<DirectWorldPosition>();
            // Generate a random normal vector to define the plane of the orbit
            Vec3 planeNormal = new Vec3(_rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5).Normalize();
            if (radiusVector != null)
            {
                // Generate a normal vector which is orthogonal to the radius vector
                planeNormal = radiusVector.Value.CrossProduct(planeNormal).Normalize();
            }

            // Generate two vectors that are orthogonal to the plane normal
            Vec3 tangent = new Vec3(planeNormal.Y, -planeNormal.X, 0.0).Normalize();
            if (tangent.Magnitude < 0.0001)
                tangent = new Vec3(0.0, planeNormal.Z, -planeNormal.Y).Normalize();
            Vec3 bitangent = planeNormal.CrossProduct(tangent).Normalize();

            // Generate the path points by rotating a vector around the plane

            if (!humanize)
            {
                double angleIncrement = 2 * Math.PI / numPoints;
                for (int i = 0; i < numPoints; i++)
                {
                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    Vec3 pointInSpace = pointOnPlane * radius + new Vec3(XCoordinate, YCoordinate, ZCoordinate);

                    path.Add(new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z));
                }
            }
            else
            {
                double angleIncrement = 2 * Math.PI / numPoints;
                for (int i = 0; i < numPoints; i++)
                {
                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    var min = humanizeFactor?.Min ?? 0.75d;
                    var max = humanizeFactor?.Max ?? 1.0d;
                    Vec3 pointInSpace = pointOnPlane * (radius * RandomBetween(min, max)) + new Vec3(XCoordinate, YCoordinate, ZCoordinate);

                    path.Add(new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z));
                }
            }
            return path;
        }

        internal bool IsSpotTooCloseToNPCs(DirectWorldPosition SpotToCheck)
        {
            try
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                            {
                                var NPC = Combat.PotentialCombatTargets.Where(i => i.IsNPCBattlecruiser).OrderBy(x => x.Distance).FirstOrDefault();
                                if (16000 > NPC._directEntity.DistanceTo(SpotToCheck) && NPC.Distance > NPC._directEntity.DistanceTo(SpotToCheck))
                                {
                                    Log.WriteLine("HighAngleDroneBattleCruiserSpawn: spot too close [" + NPC._directEntity.DistanceTo(SpotToCheck) + "]m to a Battlecruiser discarding");
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                            {
                                var NPC = Combat.PotentialCombatTargets.Where(i => i.IsNPCBattlecruiser).OrderBy(x => x.Distance).FirstOrDefault();
                                if (16000 > NPC._directEntity.DistanceTo(SpotToCheck) && NPC.Distance > NPC._directEntity.DistanceTo(SpotToCheck))
                                {
                                    Log.WriteLine("DrekavacBattleCruiserSpawn: spot too close [" + NPC._directEntity.DistanceTo(SpotToCheck) + "]m to a Battlecruiser: discarding");
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                            {
                                var NPC = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderBy(x => x.Distance).FirstOrDefault();
                                if (20000 > NPC._directEntity.DistanceTo(SpotToCheck) && NPC.Distance > NPC._directEntity.DistanceTo(SpotToCheck))
                                {
                                    Log.WriteLine("KikimoraDestroyerSpawn: spot too close [" + NPC._directEntity.DistanceTo(SpotToCheck) + "]m to a Kikimora: discarding");
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Knight")))
                            {
                                var NPC = Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser && i.Name.Contains("Knight")).OrderBy(x => x.Distance).FirstOrDefault();
                                if (16000 > NPC._directEntity.DistanceTo(SpotToCheck) && NPC.Distance > NPC._directEntity.DistanceTo(SpotToCheck))
                                {
                                    Log.WriteLine("DevotedCruiserSpawn: spot too close [" + NPC._directEntity.DistanceTo(SpotToCheck) + "]m to a Devoted Knight: discarding");
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Cynabal")))
                            {
                                var NPC = Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser && i.Name.Contains("Cynabal")).OrderBy(x => x.Distance).FirstOrDefault();
                                if (15000 > NPC._directEntity.DistanceTo(SpotToCheck) && NPC.Distance > NPC._directEntity.DistanceTo(SpotToCheck))
                                {
                                    Log.WriteLine("LuciferSpawn: spot too close  [" + NPC._directEntity.DistanceTo(SpotToCheck) + "]m to a Cynabal: discarding");
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
                            {
                                var NPC = Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(x => x.Distance).FirstOrDefault();
                                if (12000 > NPC._directEntity.DistanceTo(SpotToCheck) && NPC.Distance > NPC._directEntity.DistanceTo(SpotToCheck))
                                {
                                    Log.WriteLine("HighAngleDroneBattleCruiserSpawn: spot too close [" + NPC._directEntity.DistanceTo(SpotToCheck) + "]m to a NPC Frigate: discarding");
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        internal bool IsSpotWithinAbyssalBounds(DirectWorldPosition SpotToCheck)
        {
            try
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.AbyssalCenter._directEntity.DistanceTo(SpotToCheck) > ESCache.Instance.SafeDistanceFromAbyssalCenter)
                    {
                        Log.WriteLine("discarding proposed spot on route [" + ESCache.Instance.AbyssalCenter._directEntity.DistanceTo(SpotToCheck) + "] > SafeDistanceFromAbyssalCenter [" + ESCache.Instance.SafeDistanceFromAbyssalCenter + "]");
                        return false;
                    }

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        public bool IsSpotWithinAbyssalBounds2(DirectWorldPosition p, long offset = 0)
        {
            if (!ESCache.Instance.InAbyssalDeadspace)
                return false;

            if (offset == 0)
                return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= DirectEntity.AbyssBoundarySizeSquared;

            return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= (DirectEntity.AbyssBoundarySize + offset) * (DirectEntity.AbyssBoundarySize + offset);
        }

        public List<DirectWorldPosition> GetHighTransversalOrbitPath(double radius, int numPoints, bool humanize = true, bool checkIsSpotTooCloseToNPCs = true, bool checkIsSpotWithinAbyssalBounds = true)
        {
            numPoints = numPoints * 3;
            List<DirectWorldPosition> path = new List<DirectWorldPosition>();
            // Generate a random normal vector to define the plane of the orbit
            //Vec3 planeNormal = new Vec3(_rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5).Normalize();
            Vec3 planeNormal = new Vec3(1.0, 0.0, 0.0);
            // Add a small random perturbation to the planeNormal vector
            double maxPerturbation = radius * 0.05; //5% of the radius

            Vec3 perturbation = new Vec3(_rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5).Normalize() * _rnd.NextDouble() * maxPerturbation;
            planeNormal += perturbation;
            planeNormal.Normalize();

            // Generate two vectors that are orthogonal to the plane normal
            Vec3 tangent = new Vec3(planeNormal.Y, -planeNormal.X, 0.0).Normalize();
            if (tangent.Magnitude < 0.0001)
                tangent = new Vec3(0.0, planeNormal.Z, -planeNormal.Y).Normalize();

            Vec3 bitangent = planeNormal.CrossProduct(tangent).Normalize();

            // Generate the path points by rotating a vector around the plane

            if (ESCache.Instance.AbyssalCenter.Distance > 45000)
                numPoints = numPoints * 2;

            if (!humanize)
            {
                double angleIncrement = 2 * Math.PI / (numPoints * 3);
                for (int i = 0; i < numPoints; i++)
                {
                    if (path.Any() && path.Count() >= numPoints)
                        break;

                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    Vec3 pointInSpace = pointOnPlane * radius + new Vec3(XCoordinate, YCoordinate, ZCoordinate);
                    //Should we be checking if this spot is within the abyssal bounds?
                    var thisDirectWorldPosition = new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z);
                    if (checkIsSpotTooCloseToNPCs)
                    {
                        if (!IsSpotTooCloseToNPCs(thisDirectWorldPosition))
                        {
                            path.Add(thisDirectWorldPosition);
                            continue;
                        }
                    }
                    else
                    {
                        path.Add(thisDirectWorldPosition);
                        continue;
                    }

                    continue;
                }
            }
            else
            {
                double angleIncrement = 2 * Math.PI / (numPoints * 3);
                for (int i = 0; i < numPoints; i++)
                {
                    if (path.Any() && path.Count() >= numPoints)
                        break;

                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    Vec3 pointInSpace = pointOnPlane * (radius * RandomBetween(0.83d, 1.1d)) + new Vec3(XCoordinate, YCoordinate, ZCoordinate);
                    //Should we be checking if this spot is within the abyssal bounds?
                    var thisDirectWorldPosition = new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z);
                    if (checkIsSpotTooCloseToNPCs)
                    {
                        if (!IsSpotTooCloseToNPCs(thisDirectWorldPosition))
                        {
                            path.Add(thisDirectWorldPosition);
                            continue;
                        }
                    }
                    else
                    {
                        path.Add(thisDirectWorldPosition);
                        continue;
                    }

                    continue;
                }
            }

            if (!path.Any())
            {
                Log.WriteLine("No spots in path?! generating new path without checking NPC Distances...");
                double angleIncrement = 2 * Math.PI / (numPoints * 3);
                for (int i = 0; i < numPoints; i++)
                {
                    if (path.Any() && path.Count() >= numPoints)
                        break;

                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    Vec3 pointInSpace = pointOnPlane * radius + new Vec3(XCoordinate, YCoordinate, ZCoordinate);
                    //Should we be checking if this spot is within the abyssal bounds?
                    var thisDirectWorldPosition = new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z);

                    continue;
                }
            }

            if (checkIsSpotWithinAbyssalBounds)
            {
                List<DirectWorldPosition> temppath = new List<DirectWorldPosition>();
                int count = 0;
                foreach (var individualPointInPath in path)
                {
                    count++;
                    if (IsSpotWithinAbyssalBounds(individualPointInPath))
                    {
                        if (IsSpotWithinAbyssalBounds2(individualPointInPath))
                        {
                            if (DirectEve.Interval(30000) && DebugConfig.DebugNavigateOnGrid) Log.WriteLine("individualPointInPath[" + count + "]: Distance to AbyssalCenter [ " + ESCache.Instance.AbyssalCenter.DistanceTo(individualPointInPath) + "m] Distance to AbyssalGate [ " + ESCache.Instance.AbyssalGate.DistanceTo(individualPointInPath) + "m] Distance to PlayerSpawnLocation [ " + ESCache.Instance.PlayerSpawnLocation.DistanceTo(individualPointInPath) + "m]");
                            temppath.Add(individualPointInPath);
                            continue;
                        }

                        if (DirectEve.Interval(30000) && DebugConfig.DebugNavigateOnGrid) Log.WriteLine("individualPointInPath[" + count + "]: Too Far? Distance to AbyssalCenter [ " + ESCache.Instance.AbyssalCenter.DistanceTo(individualPointInPath) + "m] Distance to AbyssalGate [ " + ESCache.Instance.AbyssalGate.DistanceTo(individualPointInPath) + "m] Distance to PlayerSpawnLocation [ " + ESCache.Instance.PlayerSpawnLocation.DistanceTo(individualPointInPath) + "m]!");
                        continue;
                    }

                    if (DirectEve.Interval(30000) && DebugConfig.DebugNavigateOnGrid) Log.WriteLine("individualPointInPath[" + count + "]: Too Far? Distance to AbyssalCenter [ " + ESCache.Instance.AbyssalCenter.DistanceTo(individualPointInPath) + "m] Distance to AbyssalGate [ " + ESCache.Instance.AbyssalGate.DistanceTo(individualPointInPath) + "m] Distance to PlayerSpawnLocation [ " + ESCache.Instance.PlayerSpawnLocation.DistanceTo(individualPointInPath) + "m]!!");
                    continue;
                }

                path = temppath;
            }

            return path;
        }

        public static DirectWorldPosition GetNextStepInPathToSpeedTankSpotOnImaginarySphere(DirectWorldPosition SpeedTankSpot)
        {
            //
            // use only spots (or spots close to?) on the Imaginary Sphere
            //
            List<DirectWorldPosition> SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = new List<DirectWorldPosition>();
            List<DirectWorldPosition> SpotsWithinXDistanceInTheCorrectGeneralDirection = new List<DirectWorldPosition>();
            List<DirectWorldPosition> SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation = new List<DirectWorldPosition>();

            if (!ESCache.Instance.AbyssalSphereCoordinates.Any())
            {
                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!ESCache.Instance.AbyssalSphereCoordinates.Any())");
            }
            else if (DebugConfig.DebugNavigateOnGridImaginarySphere)
            {
                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("ESCache.Instance.AbyssalSphereCoordinates.Count() [" + ESCache.Instance.AbyssalSphereCoordinates.Count() + "]");
                int intCount = 0;
                foreach (var AbyssalSphereCoordinate in ESCache.Instance.AbyssalSphereCoordinates)
                {
                    intCount++;
                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("[" + intCount + "] AbyssalSphereCoordinate [" + Math.Round(AbyssalSphereCoordinate.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) / 1000 ,0) + "]k from our ship");
                    continue;
                }
            }

            //
            // Find closest spots on the imaginary Abyssal sphere to our ship
            //
            SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = ESCache.Instance.AbyssalSphereCoordinates.Where(spot => 15000 > spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition)).ToList();
            if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())
            {
                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any()).");
                SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = ESCache.Instance.AbyssalSphereCoordinates.Where(spot => 25000 > spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition)).ToList();
                if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())
                {
                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())..");
                    SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = ESCache.Instance.AbyssalSphereCoordinates.Where(spot => 35000 > spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition)).ToList();
                    if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())
                    {
                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())...");
                        SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = ESCache.Instance.AbyssalSphereCoordinates.Where(spot => 45000 > spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition)).ToList();
                        if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())
                        {
                            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())....");
                            SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = ESCache.Instance.AbyssalSphereCoordinates.Where(spot => 65000 > spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition)).ToList();
                            if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())
                            {
                                if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())..!.!");
                                SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection = ESCache.Instance.AbyssalSphereCoordinates.Where(spot => 75000 > spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition)).ToList();
                            }
                            else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!.!.");

                        }
                        else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!.!");

                    }
                    else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!!!");

                }
                else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!!");

            }
            else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!");

            //
            // Find the spots going in the correct general direction: even if only slightly correct
            //
            if (SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())
            {
                SpotsWithinXDistanceInTheCorrectGeneralDirection = SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Where(spot => ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.GetDistance(SpeedTankSpot) > SpeedTankSpot.GetDistance(spot)).ToList();
                //
                // Find the spots going in the correct general direction: even if only slightly correct
                //
                if (SpotsWithinXDistanceInTheCorrectGeneralDirection.Any())
                {
                    if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceInTheCorrectGeneralDirection.Any())!");
                    foreach (var spot in SpotsWithinXDistanceInTheCorrectGeneralDirection)
                    {
                        if (spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) > 6000)
                        {
                            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("spot distance [" + spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) + "] > 6000");
                        }
                    }

                    SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation = SpotsWithinXDistanceInTheCorrectGeneralDirection.Where(spot => spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) > 6000).ToList();
                    if (!SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any())
                    {
                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any())!.!! ");
                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("");
                        SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation = SpotsWithinXDistanceInTheCorrectGeneralDirection.Where(spot => spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) > 4000).ToList();
                        if (!SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any())
                        {
                            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any())!!!");
                            SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation = SpotsWithinXDistanceInTheCorrectGeneralDirection.Where(spot => spot.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) > 2000).ToList();
                        }
                        else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any())!");

                    }
                    else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any()).");

                    if (SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Any())
                    {
                        if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Count() [" + SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.Count() + "]");
                        foreach (var thisSpotOnTheSphere in SpotsWithinXDistanceInTheCorrectGeneralDirectionFarEnoughAwayToUseForNavigation.OrderBy(x => x.GetDistance(SpeedTankSpot)))
                        {
                            if (Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.IsActiveTarget).Any(x => Combat.MaxWeaponRange > thisSpotOnTheSphere.GetDistance(x._directEntity.DirectAbsolutePosition)))
                            {
                                if (Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.IsActiveTarget).Any(x => 15000 > thisSpotOnTheSphere.GetDistance(x._directEntity.DirectAbsolutePosition)))
                                    continue;

                                //we should probably try to stay in weaponrange?
                                //this should be the next spot we use on the path...
                                return thisSpotOnTheSphere;
                            }
                        }
                    }
                    else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!.!");

                    //
                    // How would we hit this?
                    //
                    return null;
                }
                else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!!");

                return null;
            }
            else if (DebugConfig.DebugNavigateOnGridImaginarySphere) Log.WriteLine("if (!SpotsWithinXDistanceEvenIfTheyAreTheWrongDirection.Any())!");

            return null;
        }

        public static double GetDistance(Vec3 from, Vec3 to)
        {
            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            var deltaZ = to.Z - from.Z;
            var distance = Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY +
                deltaZ * deltaZ);
            return distance;
        }

        public double GetDistance(DirectWorldPosition to)
        {
            var deltaX = to.XCoordinate - XCoordinate;
            var deltaY = to.YCoordinate - YCoordinate;
            var deltaZ = to.ZCoordinate - ZCoordinate;
            var distance = Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY +
                deltaZ * deltaZ);
            return Math.Round(distance, 0);
        }

        public double GetDistance(Vec3 to)
        {
            var deltaX = to.X - XCoordinate;
            var deltaY = to.Y - YCoordinate;
            var deltaZ = to.Z - ZCoordinate;
            var distance = Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY +
                deltaZ * deltaZ);
            return Math.Round(distance, 0);
        }

        public List<DirectWorldPosition> GenerateNeighbours(int stepSize, DirectWorldPosition end)
        {
            Vec3 direction = this.GetDirectionalVectorTo(end).Normalize().Scale(stepSize);
            var directDWP = new DirectWorldPosition(XCoordinate + direction.X, YCoordinate + direction.Y, ZCoordinate + direction.Z);

            var ret = new List<DirectWorldPosition>()
            {
                new DirectWorldPosition(XCoordinate + stepSize,YCoordinate,ZCoordinate),
                new DirectWorldPosition(XCoordinate - stepSize,YCoordinate,ZCoordinate),
                new DirectWorldPosition(XCoordinate,YCoordinate + stepSize,ZCoordinate),
                new DirectWorldPosition(XCoordinate,YCoordinate - stepSize,ZCoordinate),
                new DirectWorldPosition(XCoordinate,YCoordinate,ZCoordinate + stepSize),
                new DirectWorldPosition(XCoordinate,YCoordinate,ZCoordinate - stepSize),
                new DirectWorldPosition(XCoordinate - stepSize, YCoordinate - stepSize, ZCoordinate + stepSize),
                new DirectWorldPosition(XCoordinate - stepSize, YCoordinate - stepSize, ZCoordinate - stepSize)
            };

            return ret;
        }

        public double? GetDistance(DirectWorldPosition to, double modifier = 0, double offsetX = 0, double offsetY = 0,
            double offsetZ = 0)
        {
            try
            {
                var deltaX = to.XCoordinate - XCoordinate + offsetX;
                var deltaY = to.YCoordinate - YCoordinate + offsetY;
                var deltaZ = to.ZCoordinate - ZCoordinate + offsetZ;
                var distance = Math.Sqrt(
                    deltaX * deltaX +
                    deltaY * deltaY +
                    deltaZ * deltaZ);

                if (modifier != 0)
                    return distance * modifier;

                return distance;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetDist Overflow: {e}");
                return null;
            }
        }
        public Vec3 GetVector()
        {
            return new Vec3(this.XCoordinate, this.YCoordinate, this.ZCoordinate);
        }

        public Vec3 GetDirectionalVectorTo(DirectWorldPosition to)
        {
            return new Vec3(to.XCoordinate - this.XCoordinate, to.YCoordinate - this.YCoordinate, to.ZCoordinate - this.ZCoordinate);
        }

        public double? GetInverseDistanceSquared(DirectWorldPosition to, double modifier = 0, double offsetX = 0,
            double offsetY = 0, double offsetZ = 0)
        {
            return 1/ GetDistanceSquared(to, modifier, offsetX, offsetY, offsetZ);
        }

        public double? GetInverseDistance(DirectWorldPosition to, double modifier = 0, double offsetX = 0,
            double offsetY = 0, double offsetZ = 0)
        {
            return 1 / GetDistance(to, modifier, offsetX, offsetY, offsetZ);
        }

        public double? GetDistanceSquared(DirectWorldPosition to, double modifier = 0, double offsetX = 0,
            double offsetY = 0, double offsetZ = 0)
        {
            try
            {
                var deltaX = to.XCoordinate - XCoordinate + offsetX;
                var deltaY = to.YCoordinate - YCoordinate + offsetY;
                var deltaZ = to.ZCoordinate - ZCoordinate + offsetZ;
                var distance = (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                if (modifier != 0)
                    return distance * modifier;

                return distance;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetDistanceSquared Overflow: {e}");
                return null;
            }
        }
    }
}