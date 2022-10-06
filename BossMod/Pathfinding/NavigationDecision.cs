﻿using System;
using System.Linq;

namespace BossMod.Pathfinding
{
    // utility for selecting player's navigation target
    // there are several goals that navigation has to meet, in following rough priority
    // 1. stay away from aoes; tricky thing is that sometimes it is ok to temporarily enter aoe, if we're sure we'll exit it in time
    // 2. maintain uptime - this is represented by being in specified range of specified target, and not moving to interrupt casts unless needed
    // 3. execute positionals - this is strictly less important than points above, we only do that if we can meet other conditions
    // 4. be in range of healers - even less important, but still nice to do
    public struct NavigationDecision
    {
        public enum Decision
        {
            None,
            SrcOutOfBounds,
            ImminentToSafe,
            ImminentToClosest,
            UnsafeToPositional,
            UnsafeToUptime,
            UnsafeToSafe,
            SafeToUptime,
            SafeToCloser,
            SafeBlocked,
            UptimeToPositional,
            UptimeBlocked,
            Optimal,
        }

        public WPos? Destination;
        public float LeewaySeconds; // can be used for finishing casts / slidecasting etc.
        public float TimeToGoal;
        public Map? Map;
        public int MapGoal;
        public Decision DecisionType;

        public const float DefaultForbiddenZoneCushion = 0.7071068f;

        public static NavigationDecision Build(WorldState ws, AIHints hints, Actor player, WPos? targetPos, float targetRadius, Angle targetRot, Positional positional, float playerSpeed = 6, float forbiddenZoneCushion = DefaultForbiddenZoneCushion)
        {
            // TODO: skip pathfinding if there are no forbidden zones, just find closest point in circle/cone...

            // check that player is in bounds; otherwise pathfinding won't work properly anyway
            if (!hints.Bounds.Contains(player.Position))
            {
                var dest = hints.Bounds.ClampToBounds(player.Position);
                return new() { Destination = dest, LeewaySeconds = float.MaxValue, TimeToGoal = (dest - player.Position).Length() / playerSpeed, DecisionType = Decision.SrcOutOfBounds };
            }

            var imminent = ImminentExplosionTime(ws.CurrentTime);
            int numImminentZones = hints.ForbiddenZones.FindIndex(z => z.activation > imminent);
            if (numImminentZones < 0)
                numImminentZones = hints.ForbiddenZones.Count;

            // check whether player is inside each forbidden zone
            var zoneDistanceFuncs = hints.ForbiddenZones.Select(z => (z.shape.Distance(z.origin, z.rot), z.activation)).ToList();
            var inZone = zoneDistanceFuncs.Select(f => f.Item1(player.Position) <= forbiddenZoneCushion - 0.1f).ToList(); // we might have a situation where player's cell center is outside, but player is not, yet player is too close to center for navigation to work...
            if (inZone.Any(inside => inside))
            {
                // we're in forbidden zone => find path to safety (and ideally to uptime zone)
                // if such a path can't be found (that's always the case if we're inside imminent forbidden zone, but can also happen in other cases), try instead to find a path to safety that doesn't enter any other zones that we're not inside
                // first build a map with zones that we're outside of as blockers
                var map = hints.Bounds.BuildMap();
                foreach (var (zf, inside) in zoneDistanceFuncs.Zip(inZone))
                    if (!inside)
                        AddBlockerZone(map, imminent, zf.Item2, zf.Item1, forbiddenZoneCushion);

                bool inImminentForbiddenZone = inZone.Take(numImminentZones).Any(inside => inside);
                if (!inImminentForbiddenZone)
                {
                    var map2 = map.Clone();
                    foreach (var (zf, inside) in zoneDistanceFuncs.Zip(inZone))
                        if (inside)
                            AddBlockerZone(map2, imminent, zf.Item2, zf.Item1, forbiddenZoneCushion);
                    int maxGoal = targetPos != null ? AddTargetGoal(map2, targetPos.Value, targetRadius, targetRot, positional, 0) : 0;
                    var res = FindPathFromUnsafe(map2, player.Position, 0, maxGoal, playerSpeed);
                    if (res != null)
                        return res.Value;
                }

                // pathfind to any spot outside aoes we're in that doesn't enter new aoes
                foreach (var (zf, inside) in zoneDistanceFuncs.Zip(inZone))
                    if (inside)
                        map.AddGoal(zf.Item1, forbiddenZoneCushion, 0, -1);
                return FindPathFromImminent(map, player.Position, playerSpeed);
            }

            // we're safe, see if we can improve our position
            if (targetPos != null)
            {
                if (!player.Position.InCircle(targetPos.Value, targetRadius))
                {
                    // we're not in uptime zone, just run to it, avoiding any aoes
                    var map = hints.Bounds.BuildMap();
                    foreach (var (shape, activation) in zoneDistanceFuncs)
                        AddBlockerZone(map, imminent, activation, shape, forbiddenZoneCushion);
                    int maxGoal = AddTargetGoal(map, targetPos.Value, targetRadius, targetRot, Positional.Any, 0);
                    if (maxGoal != 0)
                    {
                        // try to find a path to target
                        var pathfind = new ThetaStar(map, maxGoal, player.Position, 1.0f / playerSpeed);
                        int res = pathfind.Execute();
                        if (res >= 0)
                            return new() { Destination = GetFirstWaypoint(pathfind, res), LeewaySeconds = float.MaxValue, TimeToGoal = pathfind.NodeByIndex(res).GScore, Map = map, MapGoal = maxGoal, DecisionType = Decision.SafeToUptime };
                    }

                    // goal is not reachable, but we can try getting as close to the target as we can until first aoe
                    var start = map.WorldToGrid(player.Position);
                    var end = map.WorldToGrid(hints.Bounds.ClampToBounds(targetPos.Value));
                    var best = start;
                    foreach (var (x, y) in map.EnumeratePixelsInLine(start.x, start.y, end.x, end.y))
                    {
                        if (map[x, y].MaxG != float.MaxValue)
                            break;
                        best = (x, y);
                    }
                    if (best != start)
                    {
                        var dest = map.GridToWorld(best.x, best.y, 0.5f, 0.5f);
                        return new() { Destination = dest, LeewaySeconds = float.MaxValue, TimeToGoal = (dest - player.Position).Length() / playerSpeed, Map = map, MapGoal = maxGoal, DecisionType = Decision.SafeToCloser };
                    }

                    return new() { Destination = null, LeewaySeconds = float.MaxValue, TimeToGoal = 0, Map = map, MapGoal = maxGoal, DecisionType = Decision.SafeBlocked };
                }

                bool inPositional = positional switch
                {
                    Positional.Flank => MathF.Abs(targetRot.ToDirection().Dot((targetPos.Value - player.Position).Normalized())) < 0.7071067f,
                    Positional.Rear => targetRot.ToDirection().Dot((targetPos.Value - player.Position).Normalized()) < -0.7071068f,
                    _ => true
                };
                if (!inPositional)
                {
                    // we're in uptime zone, but not in correct quadrant - move there, avoiding all aoes and staying within uptime zone
                    var map = hints.Bounds.BuildMap();
                    map.BlockPixelsInside(ShapeDistance.InvertedCircle(targetPos.Value, targetRadius), 0, 0);
                    foreach (var (shape, activation) in zoneDistanceFuncs)
                        AddBlockerZone(map, imminent, activation, shape, forbiddenZoneCushion);
                    int maxGoal = AddPositionalGoal(map, targetPos.Value, targetRadius, targetRot, positional, 0);
                    if (maxGoal > 0)
                    {
                        // try to find a path to quadrant
                        var pathfind = new ThetaStar(map, maxGoal, player.Position, 1.0f / playerSpeed);
                        int res = pathfind.Execute();
                        if (res >= 0)
                            return new() { Destination = GetFirstWaypoint(pathfind, res), LeewaySeconds = float.MaxValue, TimeToGoal = pathfind.NodeByIndex(res).GScore, Map = map, MapGoal = maxGoal, DecisionType = Decision.UptimeToPositional };
                    }

                    // fail
                    return new() { Destination = null, LeewaySeconds = float.MaxValue, TimeToGoal = 0, Map = map, MapGoal = maxGoal, DecisionType = Decision.UptimeBlocked };
                }
            }

            return new() { Destination = null, LeewaySeconds = float.MaxValue, TimeToGoal = 0, DecisionType = Decision.Optimal };
        }

        public static DateTime ImminentExplosionTime(DateTime currentTime) => currentTime.AddSeconds(1);

        public static void AddBlockerZone(Map map, DateTime imminent, DateTime activation, Func<WPos, float> shape, float threshold) => map.BlockPixelsInside(shape, MathF.Max(0, (float)(activation - imminent).TotalSeconds), threshold);

        public static int AddTargetGoal(Map map, WPos targetPos, float targetRadius, Angle targetRot, Positional positional, int minPriority)
        {
            var adjPrio = map.AddGoal(ShapeDistance.Circle(targetPos, targetRadius), 0, minPriority, 1);
            if (adjPrio == minPriority)
                return minPriority;
            return AddPositionalGoal(map, targetPos, targetRadius, targetRot, positional, minPriority + 1);
        }

        public static Func<WPos, float> ShapeDistanceFlank(WPos targetPos, Angle targetRot)
        {
            var n1 = (targetRot + 45.Degrees()).ToDirection();
            var n2 = n1.OrthoL();
            return p =>
            {
                var off = p - targetPos;
                var d1 = n1.Dot(off);
                var d2 = n2.Dot(off);
                var dr = Math.Max(d1, d2);
                var dl = Math.Max(-d1, -d2);
                return Math.Min(dr, dl);
            };
        }

        public static Func<WPos, float> ShapeDistanceRear(WPos targetPos, Angle targetRot)
        {
            var n1 = (targetRot - 45.Degrees()).ToDirection();
            var n2 = n1.OrthoL();
            return p =>
            {
                var off = p - targetPos;
                var d1 = n1.Dot(off);
                var d2 = n2.Dot(off);
                return Math.Max(d1, d2);
            };
        }

        public static Func<WPos, float> ShapeDistanceFront(WPos targetPos, Angle targetRot)
        {
            // TODO: think more about it, currently using 30-degree frontal cone...
            var n1 = (targetRot + 15.Degrees()).ToDirection().OrthoL();
            var n2 = (targetRot - 15.Degrees()).ToDirection().OrthoR();
            return p =>
            {
                var off = p - targetPos;
                var d1 = n1.Dot(off);
                var d2 = n2.Dot(off);
                return Math.Max(d1, d2);
            };
        }

        public static int AddPositionalGoal(Map map, WPos targetPos, float targetRadius, Angle targetRot, Positional positional, int minPriority)
        {
            var adjPrio = minPriority;
            switch (positional)
            {
                case Positional.Flank:
                    adjPrio = map.AddGoal(ShapeDistanceFlank(targetPos, targetRot), 0, minPriority, 1);
                    break;
                case Positional.Rear:
                    adjPrio = map.AddGoal(ShapeDistanceRear(targetPos, targetRot), 0, minPriority, 1);
                    break;
                case Positional.Front:
                    adjPrio = map.AddGoal(ShapeDistanceFront(targetPos, targetRot), 0, minPriority, 1);
                    break;
            }
            return adjPrio;
        }

        public static NavigationDecision? FindPathFromUnsafe(Map map, WPos startPos, int safeGoal, int maxGoal, float speed = 6)
        {
            if (maxGoal - safeGoal == 2)
            {
                // try finding path to flanking position
                var pathfind = new ThetaStar(map, maxGoal, startPos, 1.0f / speed);
                int res = pathfind.Execute();
                if (res >= 0)
                    return new() { Destination = GetFirstWaypoint(pathfind, res), LeewaySeconds = pathfind.NodeByIndex(res).PathLeeway, TimeToGoal = pathfind.NodeByIndex(res).GScore, Map = map, MapGoal = maxGoal, DecisionType = Decision.UnsafeToPositional };
                --maxGoal;
            }

            if (maxGoal - safeGoal == 1)
            {
                // try finding path to uptime position
                var pathfind = new ThetaStar(map, maxGoal, startPos, 1.0f / speed);
                int res = pathfind.Execute();
                if (res >= 0)
                    return new() { Destination = GetFirstWaypoint(pathfind, res), LeewaySeconds = pathfind.NodeByIndex(res).PathLeeway, TimeToGoal = pathfind.NodeByIndex(res).GScore, Map = map, MapGoal = maxGoal, DecisionType = Decision.UnsafeToUptime };
                --maxGoal;
            }

            if (maxGoal - safeGoal == 0)
            {
                // try finding path to any safe spot
                var pathfind = new ThetaStar(map, maxGoal, startPos, 1.0f / speed);
                int res = pathfind.Execute();
                if (res >= 0)
                    return new() { Destination = GetFirstWaypoint(pathfind, res), LeewaySeconds = pathfind.NodeByIndex(res).PathLeeway, TimeToGoal = pathfind.NodeByIndex(res).GScore, Map = map, MapGoal = maxGoal, DecisionType = Decision.UnsafeToSafe };
            }

            return null;
        }

        public static NavigationDecision FindPathFromImminent(Map map, WPos startPos, float speed = 6)
        {
            // just run to closest safe spot, if no good path can be found
            var pathfind = new ThetaStar(map, 0, startPos, 1.0f / speed);
            int res = pathfind.Execute();
            if (res >= 0)
            {
                return new() { Destination = GetFirstWaypoint(pathfind, res), LeewaySeconds = 0, TimeToGoal = pathfind.NodeByIndex(res).GScore, Map = map, MapGoal = 0, DecisionType = Decision.ImminentToSafe };
            }

            var closest = map.EnumeratePixels().Where(p => { var px = map[p.x, p.y]; return px.Priority == 0 && px.MaxG == float.MaxValue; }).MinBy(p => (p.center - startPos).LengthSq()).center;
            return new() { Destination = closest, LeewaySeconds = 0, TimeToGoal = (closest - startPos).Length() / speed, Map = map, DecisionType = Decision.ImminentToClosest };
        }

        public static WPos? GetFirstWaypoint(ThetaStar pf, int cell)
        {
            do
            {
                ref var node = ref pf.NodeByIndex(cell);
                int parent = pf.CellIndex(node.ParentX, node.ParentY);
                if (pf.NodeByIndex(parent).GScore == 0)
                    return pf.CellCenter(cell);
                cell = parent;
            }
            while (true);
        }
    }
}
