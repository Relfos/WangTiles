using System;
using System.Collections.Generic;

using WangTiles.Core;

namespace WangTiles.DungeonPlanner
{
    public struct LayoutCoord
    {
        public int X;
        public int Y;

        public LayoutCoord(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }
    }

    public class LayoutConnection
    {
        public LayoutRoom roomA;
        public LayoutRoom roomB;
        public bool active;
        public int weight;
        public int pathOrder;

        public LayoutConnection(LayoutRoom roomA, LayoutRoom roomB, Random rnd)
        {
            active = true;
            weight = 1 + rnd.Next(10);
            pathOrder = -1;
            this.roomA = roomA;
            this.roomB = roomB;
        }

        public static void QuickSort(List<LayoutConnection> edgesList, int nLeft, int nRight)
        {
            int i, j, x;
            i = nLeft; j = nRight;
            x = edgesList[(nLeft + nRight) / 2].weight;

            do
            {
                while ((edgesList[i].weight < x) && (i < nRight)) i++;
                while ((x < edgesList[j].weight) && (j > nLeft)) j--;

                if (i <= j)
                {
                    LayoutConnection y = edgesList[i];
                    edgesList[i] = edgesList[j];
                    edgesList[j] = y;
                    i++; j--;
                }
            } while (i <= j);

            if (nLeft < j) QuickSort(edgesList, nLeft, j);
            if (i < nRight) QuickSort(edgesList, i, nRight);
        }
    }

    public class LayoutRoom
    {
        public enum RoomKind
        {
            Invalid,
            Entrance,
            Hall,
            Puzzle,
            Monster,
            Farm,
            Shrine,
            Treasure,
            Goal
        }

        public enum RoomShape
        {
            Empty,
            Curve,
            Corridor,
            Room,
            Split
        }

        public enum RoomCategory
        {
            Unknown,
            Main,
            Side,
            Distant,
            Secret
        }

        public string name;
        private LayoutRoom root;
        public int rank;

        public LayoutCoord coord;

        public List<LayoutConnection> connections = new List<LayoutConnection>();

        public int distance;
        public LayoutRoom previous;

        public RoomKind kind;


        public RoomCategory category;
        public bool isLoop;
        public int distanceFromMainPath;

        public float intensity;
        public bool spike;

        public bool lockable;
        public bool important;

        public LayoutKey require;
        public LayoutKey contains;
        public LayoutKey locked;

        public int order;

        public int tileID;
        public int variationID;

        public LayoutRoom parent;
        public List<LayoutRoom> children = new List<LayoutRoom>();

        public LayoutRoom(LayoutCoord coord)
        {
            this.name = coord.X + " : "+ coord.Y;
            this.coord = coord;
            this.rank = 0;
            this.root = this;

            this.kind = RoomKind.Hall;
            this.intensity = -1;
            this.lockable = true;
        }

        public override string ToString()
        {
            return this.kind + " ("+ name+")";
        }

        public bool isDeadEnd()
        {
            switch (tileID)
            {
                case 1: case 2: case 4: case 8: return true;
                default: return false;
            }
        }


        public RoomShape GetShape()
        {
            if (tileID>0 && variationID == 1)
            {
                return RoomShape.Room;
            }


            switch (tileID)
            {                
                case 1: case 2: case 4: case 8: return RoomShape.Room;
                case 3: case 6: case 9: case 12: return RoomShape.Curve;
                case 5: case 10: return RoomShape.Corridor;
                case 7: case 11: case 13: case 14:  case 15: return RoomShape.Split;
                default: return RoomShape.Empty;
            }
        }

        internal LayoutRoom GetRoot()
        {
            if (this.root != this)// am I my own parent ? (am i the root ?)  
            {
                this.root = this.root.GetRoot();// No? then get my parent  
            }
            return this.root;
        }

        internal static void Join(LayoutRoom vRoot1, LayoutRoom vRoot2)
        {

            if (vRoot2.rank < vRoot1.rank)//is the rank of Root2 less than that of Root1 ?  
            {
                vRoot2.root = vRoot1;//yes! then Root1 is the parent of Root2 (since it has the higher rank)  
            }
            else //rank of Root2 is greater than or equal to that of Root1  
            {
                vRoot1.root = vRoot2;//make Root2 the parent  
                if (vRoot1.rank == vRoot2.rank)//both ranks are equal ?  
                {
                    vRoot1.rank++;//increment one of them, we need to reach a single root for the whole tree  
                }
            }
        }

        public LayoutConnection FindConnection(LayoutRoom room)
        {
            foreach (var conn in connections)
            {
                if (conn.roomA == room || conn.roomB == room)
                {
                    return conn;
                }
            }

            return null;
        }
    }

    public class LayoutKey
    {
        public string name;
        public LayoutRoom sourceRoom;
        public LayoutRoom targetRoom;

        public int order;
        public int condition;

        public LayoutKey(string name, int order)
        {
            this.name = name;
            this.order = order;
        }

        public override string ToString()
        {
            return this.name;
        }
    }


    public class LayoutPlanner
    {
        private Random randomGenerator;

        public LayoutRoom entrance;
        public LayoutRoom goal;

        public List<LayoutConnection> connections = new List<LayoutConnection>();
        private Dictionary<LayoutCoord, LayoutRoom> rooms = new Dictionary<LayoutCoord, LayoutRoom>();

        public LayoutPlanner(int seed)
        {
            if (seed == 0)
            {
                seed = Environment.TickCount;
            }
            this.randomGenerator = new Random(seed);
        }

        #region LAYOUT_SETUP
        public LayoutConnection AddConnection(LayoutCoord a, LayoutCoord b)
        {
            return AddConnection(FindRoomAt(a), FindRoomAt(b));
        }

        public LayoutConnection AddConnection(LayoutCoord a, WangEdgeDirection dir)
        {
            LayoutCoord b;
            
            switch (dir)
            {
                case WangEdgeDirection.East: b = new LayoutCoord(a.X + 1, a.Y); break;
                case WangEdgeDirection.West: b = new LayoutCoord(a.X - 1, a.Y); break;
                case WangEdgeDirection.North: b = new LayoutCoord(a.X, a.Y - 1); break;
                case WangEdgeDirection.South: b = new LayoutCoord(a.X, a.Y + 1); break;
                default:return null;
            }
             
            return AddConnection(FindRoomAt(a), FindRoomAt(b));
        }

        public LayoutConnection AddConnection(LayoutRoom roomA, LayoutRoom roomB)
        {
            if (roomA == roomB)
            {
                return null;
            }

            LayoutConnection conn = roomA.FindConnection(roomB);
            if (conn != null)
            {
                return conn;
            }

            conn = new LayoutConnection(roomA, roomB, this.randomGenerator);
            roomA.connections.Add(conn);
            roomB.connections.Add(conn);

            this.connections.Add(conn);

            return conn;
        }

        public LayoutRoom FindRoomAt(LayoutCoord coord, bool canCreate = true)
        {
            if (rooms.ContainsKey(coord))
            {
                return rooms[coord];
            }

            if (!canCreate)
            {
                return null;
            }

            var room = new LayoutRoom(coord);
            rooms[coord] = room;
            return room;
        }
        #endregion

        public struct RoomScore
        {
            public int score;
            public LayoutRoom room;
            public RoomScore(int score, LayoutRoom room)
            {
                this.score = score;
                this.room = room;
            }
        }

        protected int GetRoomScore(LayoutRoom room)
        {
            return 10;
        }

        public LayoutRoom FindEntrance()
        {
            entrance = null;

            List<RoomScore> temp = new List<RoomScore>();
            foreach (var room in rooms.Values)
            {
                int score = GetRoomScore(room);

                if (score < 0)
                {
                    continue;
                }

                if (room.isDeadEnd())
                {
                    score *= 2;
                }

                temp.Add(new RoomScore(score, room));
            }

            if (temp.Count == 0)
            {
                //LogError("Could not find any room that matches entrance settings");
                return null;
            }

            temp.Sort(new Comparison<RoomScore>((RoomScore x, RoomScore y) =>
            {
                //if (x.order == y.order) return x.height.CompareTo(y.height); else return x.order.CompareTo(y.order);
                return y.score.CompareTo(x.score);
            }));

            entrance = temp[randomGenerator.Next((temp.Count < 3 ? temp.Count : 3))].room;

            entrance.kind = LayoutRoom.RoomKind.Entrance;
            
            return entrance;
        }


        public LayoutRoom FindGoal()
        {
            if (entrance == null)
            {
                Console.WriteLine("Entrance is not set...");
                return null;
            }

            // run dijkstra to find shortest paths 
            List<LayoutRoom> Q = new List<LayoutRoom>();
            foreach (var room in rooms.Values)
            {
                room.distance = 99999;   // Unknown distance from source to v
                room.previous = null;    // Previous node in optimal path from source
                Q.Add(room);             // All nodes initially in Q (unvisited nodes)
            }

            entrance.distance = 0;                        // Distance from source to source
            while (Q.Count > 0)
            {
                LayoutRoom best = null;
                int min = 9999;
                foreach (LayoutRoom other in Q)
                {
                    if (other.distance < min)
                    {
                        min = other.distance;
                        best = other;
                    }
                }

                Q.Remove(best);
                foreach (var path in best.connections) // where V is still in Q.
                {
                    LayoutRoom V = path.roomA == best ? path.roomB : path.roomA;
                    if (!path.active)
                    {
                        continue;
                    }

                    if (!Q.Contains(V))
                    {
                        continue;
                    }

                    int alt = best.distance + path.weight;
                    if (alt < V.distance)
                    {
                        V.distance = alt;
                        V.previous = best;
                    }
                }
            }

            // find goal room
            goal = null;
            int maxGoal = 0;
            foreach (var room in rooms.Values)
            {
                if (room == entrance)
                {
                    continue;
                }

                if (!room.isDeadEnd())
                {
                    continue;
                }

                int dist = room.distance;
                if (dist > maxGoal)
                {
                    int score = GetRoomScore(room);
                    if (score > 0)
                    {
                        maxGoal = dist;
                        goal = room;
                    }

                }
            }

            if (goal == null)
            {
                //LogError("Could not find a goal...");
                return null;
            }

            //Debug.LogWarning("Found goal: " + goal.FloorLevel);
            return goal;
        }

        public void SetGoal(LayoutRoom goal)
        {
            this.goal = goal;
            goal.kind = LayoutRoom.RoomKind.Goal;

            this.GenerateIntensity();
        }

        /// <summary>
        /// Selects a category for each room, based if its a main path, a side path, secret path etc
        /// </summary>
        private void CategorizeRooms()
        {
            // first set any room in the shortest path from entrance to goal as a 'main' room
            var temp = goal;
            while (temp != null)
            {
                temp.category = LayoutRoom.RoomCategory.Main;
                temp.distanceFromMainPath = 0;
                temp = temp.previous;
            }

            foreach (var room in rooms.Values)
            {
                if (room.category != LayoutRoom.RoomCategory.Main)
                {
                    continue;
                }

                foreach (var path in room.connections)
                {
                    var other = path.roomA == room ? path.roomB : path.roomA;

                    if (other.category == LayoutRoom.RoomCategory.Unknown)
                    {
                        FloodAdjacentsWithCategory(other, 1, room);
                    }
                }
            }
        }

        private void FloodChildrenWithCategory(LayoutRoom room, int distance)
        {
            room.category = LayoutRoom.RoomCategory.Distant;
            room.distanceFromMainPath = distance;

            foreach (var child in room.children)
            {
                FloodChildrenWithCategory(child, distance + 1);
            }
        }

        private void FloodLoopCategory(LayoutRoom source, LayoutRoom dest)
        {
            if (source.isDeadEnd())
            {
                return;
            }

            source.isLoop = true;

            if (source == dest)
            {
                return;
            }

            foreach (var path in source.connections)
            {
                var other = path.roomA == source ? path.roomB : path.roomA;

                if (other.order < source.order)
                {
                    continue;
                }

                FloodLoopCategory(other, dest);
            }
        }

        private void FixLoopCategory(LayoutRoom source, LayoutRoom dest, LayoutRoom room)
        {
            if (source.order > dest.order)
            {
                var temp = source;
                source = dest;
                dest = temp;
            }

            /*while (room != source)
            {

            }*/

            FloodLoopCategory(source, dest);            
        }

        private void FloodAdjacentsWithCategory(LayoutRoom room, int distance, LayoutRoom parent)
        {
            if (room.connections.Count>2)
            {
                FloodChildrenWithCategory(room, distance);
                return;
            }

            room.category = LayoutRoom.RoomCategory.Side;
            room.distanceFromMainPath = distance;

            foreach (var path in room.connections)
            {
                var other = path.roomA == room ? path.roomB : path.roomA;

                if (other.category == LayoutRoom.RoomCategory.Main && other != parent)
                {
                    FixLoopCategory(other, parent, room);
                    return;
                }

                if (other.category != LayoutRoom.RoomCategory.Unknown)
                {
                    continue;
                }

                FloodAdjacentsWithCategory(other, distance + 1, parent);
            }
        }

        /// <summary>
        /// Runs Kruskals-Minimum-Spanning-Tree on the dungeon network
        /// </summary>
        /// <param name="edges"></param>
        private void SolveGraph(List<LayoutConnection> edges)
        {
            int nTotalCost = 0;
            int currentOrder = 0;

            if (edges.Count <= 0)
            {
                return;
            }

            LayoutConnection.QuickSort(edges, 0, edges.Count - 1);
            foreach (var ed in edges)
            {
                if (!ed.active)
                {
                    continue;
                }

                LayoutRoom vRoot1, vRoot2;
                vRoot1 = ed.roomA.GetRoot();
                vRoot2 = ed.roomB.GetRoot();

                if (vRoot1 != vRoot2)
                {
                    nTotalCost += ed.weight;
                    LayoutRoom.Join(vRoot1, vRoot2);

                    ed.pathOrder = currentOrder;
                    ed.active = true;
                    currentOrder++;
                }
            }
        }

        public void GenerateProgression()
        {
            this.SolveGraph(this.connections);

            // generate dungeon semantic tree (which represents the logical progression inside the dungeon)
            int currentOrder = 1;
            FindChildrenProgression(ref currentOrder, entrance, new List<LayoutRoom>());

            CategorizeRooms();
        }

        protected void FindChildrenProgression(ref int currentOrder, LayoutRoom startRoom, List<LayoutRoom> visitedRooms)
        {
            startRoom.order = currentOrder;
            currentOrder++;
            visitedRooms.Add(startRoom);

            startRoom.children = new List<LayoutRoom>();
            foreach (var path in startRoom.connections)
            {
                if (path.pathOrder < 0)
                {
                    continue;
                }

                var other = path.roomA == startRoom ? path.roomB : path.roomA;
                //Console.WriteLine("Found connection to " + other.ToString() + " -> " + other.visited + " order: " + path.pathOrder);

                if (!visitedRooms.Contains(other))
                {
                    other.parent = startRoom;
                    startRoom.children.Add(other);
                    FindChildrenProgression(ref currentOrder, other, visitedRooms);
                }
            }
        }

        protected void CalculateIntensity(LayoutRoom room, float value)
        {
            if (room.intensity >= 0)
            {
                return;
            }

            room.spike = false;

            if (room.kind == LayoutRoom.RoomKind.Goal)
            {
                value += 1.5f;
                room.spike = true;
            }
            else
            {
                if (room.contains != null)
                {
                    value += 1.25f;
                    room.spike = true;
                }
                else
                if (room.important)
                {
                    value += 1;
                }
                else
                {
                    value += 0.25f;
                }

                if (room.kind == LayoutRoom.RoomKind.Treasure)
                {
                    value += 0.5f;
                    room.spike = true;
                }

                if (room.parent != null && room.parent.spike)
                {
                    value -= 2;
                }
            }

            room.intensity = value;

            foreach (var child in room.children)
            {
                CalculateIntensity(child, value);
            }
        }

        protected void GenerateIntensity()
        {
            if (goal == null)
            {
                return;
            }

            foreach (var room in rooms.Values)
            {
                room.intensity = -1;
            }

            CalculateIntensity(entrance, 0);

            float max = goal.intensity;

            foreach (var room in rooms.Values)
            {
                if (room.intensity >= max)
                {
                    room.intensity = max;
                }
            }

            goal.intensity = max;
            
            // normalize intensities
            foreach (var room in rooms.Values)
            {
                room.intensity /= max;
                //    Debug.Log(room.Name + " => " + ((int)(room.intensity * 100.0f)).ToString());
            }
        }

        #region LOCKS_AND_KEYS
        /*protected bool CanBeLocked(LayoutRoom room)
        {
            if (room.parent == null)
            {
                return false;
            }

            if (!room.lockable)
            {
                return false;
            }

            if (room.kind == LayoutRoom.RoomKind.Entrance)
            {
                return false;
            }

            return room.GetShape() != LayoutRoom.RoomShape.DeadEnd;
            //return parent.GetBranchesCount() > 1;
        }


        protected LayoutRoom FindLockableRoom(LayoutRoom currentRoom, List<LayoutRoom> testedRooms, float maxIntensity)
        {
            if (testedRooms.Contains(currentRoom))
            {
                return null;
            }

            testedRooms.Add(currentRoom);

            if (currentRoom.locked == null)
            {
                if (currentRoom.require != null || (currentRoom.intensity < maxIntensity && CanBeLocked(currentRoom)))
                {
                    return currentRoom;
                }

            }

            if (currentRoom.parent == null)
            {
                return null;
            }

            return FindLockableRoom(currentRoom.parent, testedRooms, maxIntensity);
        }

        protected void LockRoom(LayoutRoom room, LayoutKey keylock)
        {
            if (room.locked == null)
            {
                room.locked = keylock;
                Console.WriteLine("Locked " + room.ToString());
            }

            foreach (var child in room.children)
            {
                LockRoom(child, keylock);
            }
        }

        public bool CanPlaceKey(LayoutRoom room, bool deadEndsonly)
        {
            if (deadEndsonly && room.GetShape() != LayoutRoom.RoomShape.DeadEnd)
            {
                return false;
            }

            return (room.kind != LayoutRoom.RoomKind.Goal && room.kind != LayoutRoom.RoomKind.Entrance);
        }*/

        /*public void PlaceKey(LayoutRoom sourceRoom, LayoutKey key, LayoutRoom targetRoom, List<LayoutRoom> testedRooms, int count, bool deadEndsonly)
        {
            if (testedRooms.Contains(sourceRoom))
            {
                return;
            }

            if (sourceRoom.locked != null)
            {
                return;
            }

            Console.WriteLine("Testing " + this.ToString());
            testedRooms.Add(sourceRoom);

            if (key.room != null)
            {
                return;
            }


            if (sourceRoom != targetRoom && CanPlaceKey(sourceRoom, deadEndsonly))
            {
                if (sourceRoom.contains != null)
                {
                    return;
                }

                if (count <= 0)
                {
                    Console.WriteLine("Placed " + key.name + " in room " + sourceRoom);

                    key.room = sourceRoom;
                    sourceRoom.contains = key;
                    return;
                }
            }

            List<LayoutRoom> rooms = new List<LayoutRoom>();
            if (sourceRoom.parent != null && !testedRooms.Contains(sourceRoom.parent))
            {
                rooms.Add(sourceRoom.parent);
            }


            if (sourceRoom != targetRoom)
            {
                foreach (var child in sourceRoom.children)
                {
                    if (!testedRooms.Contains(child))
                    {
                        rooms.Add(child);
                    }

                }

            }

            if (rooms.Count <= 0)
            {
                return;
            }

            while (rooms.Count > 0)
            {
                int n = this.randomGenerator.Next(rooms.Count);
                LayoutRoom target = rooms[n];

                PlaceKey(target, key, targetRoom, testedRooms, count - 1, deadEndsonly);
                if (key.room != null)
                {
                    return;
                }

                rooms.RemoveAt(n);
            }
        }*/

        protected bool IsRoomImportant(LayoutRoom room)
        {
            if (room.important)
            {
                return true;
            }

            if (room.contains != null)
            {
                return true;
            }

            foreach (var child in room.children)
            {
                if (IsRoomImportant(child))
                {
                    return true;
                }
            }

            return false;
        }

        /*private void FindRoomsForKey(LayoutRoom room, List<LayoutRoom> possibleRooms, List<LayoutRoom> visitedRooms)
        {
            if (visitedRooms.Contains(room))
            {
                return;
            }
            visitedRooms.Add(room);

            var shape = room.GetShape();
            if (room.category == LayoutRoom.RoomCategory.Distant && (shape == LayoutRoom.RoomShape.DeadEnd || shape == LayoutRoom.RoomShape.Room))
            {
                possibleRooms.Add(room);
            }

            foreach (var child in room.children)
            {
                FindRoomsForKey(child, possibleRooms, visitedRooms);
            }

            if (room.parent != null)
            {
                FindRoomsForKey(room.parent, possibleRooms, visitedRooms);
            }
        }*/

        private bool isValidKeyRoom(LayoutRoom room)
        {
            return room.category == LayoutRoom.RoomCategory.Distant && room.GetShape() == LayoutRoom.RoomShape.Room;
        }

        public void GenerateLocks(List<LayoutKey> keys)
        {
            if (keys == null || keys.Count <= 0)
            {
                return;
            }

            if (goal == null)
            {
                Console.WriteLine("Goal is not set...");
                return;
            }

            keys.Sort((x, y) => x.order.CompareTo(y.order));

            int firstKeyRoom = rooms.Count + 1;
            foreach (var room in rooms.Values)
            {
                if (isValidKeyRoom(room) && room.order < firstKeyRoom)
                {
                    firstKeyRoom = room.order;
                }
            }

            List<LayoutRoom> possibleLockedRooms = new List<LayoutRoom>();
            List<LayoutRoom> possibleKeyRooms = new List<LayoutRoom>();
            foreach (var room in rooms.Values)
            {
                if (room == entrance || room == goal)
                {
                    continue;
                }

                if (isValidKeyRoom(room))
                {
                    possibleKeyRooms.Add(room);
                }

                if (room.category == LayoutRoom.RoomCategory.Main && !room.isLoop && room.order > firstKeyRoom)
                {
                    possibleLockedRooms.Add(room);

                    if (room.require != null)
                    {
                        var key = room.require;
                        key.targetRoom = room;                        
                    }
                }
            }

            Dictionary<LayoutKey, List<LayoutRoom>> lockableRooms = new Dictionary<LayoutKey, List<LayoutRoom>>();
            int roomSpread = possibleLockedRooms.Count / keys.Count;

            possibleLockedRooms.Sort((x, y) => x.order.CompareTo(y.order));

            for (int k=0; k<keys.Count; k++)
            {
                int startRoom = k * roomSpread;
                int lastRoom = startRoom + roomSpread - 1;
                if (lastRoom>=possibleLockedRooms.Count)
                {
                    lastRoom = possibleLockedRooms.Count - 1;
                }

                var key = keys[k];
                lockableRooms[key] = new List<LayoutRoom>();
                for (int i=startRoom; i<=lastRoom; i++)
                {
                    lockableRooms[key].Add(possibleLockedRooms[i]);
                }
            }

            foreach (LayoutKey key in keys)
            {
                if (key.targetRoom != null)
                {
                    continue;
                }

                var possibleTargets = lockableRooms[key];

                if (possibleTargets.Count == 0)
                {
                    continue;
                }

                LayoutRoom targetRoom = possibleTargets[randomGenerator.Next(possibleTargets.Count)];
                key.targetRoom = targetRoom;
            }

            List<LayoutKey> originalKeys = new List<LayoutKey>();
            foreach (LayoutKey key in keys)
            {
                originalKeys.Add(key);
            }

            keys.Sort((x, y) => y.order.CompareTo(x.order));
            for (int k=0; k<keys.Count; k++)
            {
                var key = keys[k];
                var nextKey = k<keys.Count-1 ? keys[k+1] : null;

                if (key.targetRoom == null)
                {
                    continue;
                }

                key.targetRoom.locked = key;

                Console.WriteLine("Key '" + key.name + "' used to lock room " + key.targetRoom);

                List<LayoutRoom> possibleSources = new List<LayoutRoom>();
                foreach (var room in possibleKeyRooms)
                {
                    if (room.order > key.targetRoom.order)
                    {
                        continue;
                    }

                    if (nextKey!=null && room.order<=nextKey.targetRoom.order)
                    {
                        continue;
                    }

                    possibleSources.Add(room);
                }

                if (possibleSources.Count == 0)
                {
                    foreach (var room in rooms.Values)
                    {
                        if (room.category != LayoutRoom.RoomCategory.Side)
                        {
                            continue;
                        }

                        if (room.order > key.targetRoom.order)
                        {
                            continue;
                        }

                        if (nextKey != null && room.order <= nextKey.targetRoom.order)
                        {
                            continue;
                        }

                        possibleSources.Add(room);
                    }
                }

                if (possibleSources.Count == 0)
                {
                    continue;
                }

                var sourceRoom = possibleSources[randomGenerator.Next(possibleSources.Count)];
                key.sourceRoom = sourceRoom;
                sourceRoom.contains = key;

                possibleKeyRooms.Remove(sourceRoom);

                Console.WriteLine("Placed " + key.name + " in room " + sourceRoom);
            }

            foreach (LayoutKey key in originalKeys)
            {
                if (key.sourceRoom == null)
                {
                    Console.WriteLine("Key '" + key.name + "' was not placed...");
                }
            }

            // after generating the locks, its now possible to understand which rooms are important and which ones are optional
            goal.important = true;
            entrance.important = true;
            foreach (var room in rooms.Values)
            {
                room.important = IsRoomImportant(room);
            }

            // generate intensity for rooms based on tension curve 
            GenerateIntensity();
        }

        /*public void GenerateLocks(List<LayoutKey> keys)
        {
            if (keys == null || keys.Count <= 0)
            {
                return;
            }

            if (goal == null)
            {
                Console.WriteLine("Goal is not set...");
                return;
            }

            List<LayoutKey> openKeys = new List<LayoutKey>();
            foreach (LayoutKey key in keys)
            {
                openKeys.Add(key);
            }

            LayoutRoom currentRoom = goal;
            bool found = true;
            int currentCondition = openKeys.Count;
            float maxLockableIntensity = 1.0f;
            while (openKeys.Count > 0)
            {

                found = false;

                Console.WriteLine("Trying to find lockable room with max intensity " + (int)(maxLockableIntensity * 100));

                LayoutRoom targetRoom = FindLockableRoom(currentRoom, new List<LayoutRoom>(), maxLockableIntensity);
                if (targetRoom != null)
                {
                    Console.WriteLine("Found lockable room " + targetRoom.ToString());
                    LayoutKey targetKey = null;

                    if (targetRoom.require != null && openKeys.Contains(targetRoom.require))
                    {
                        targetKey = targetRoom.require;
                    }
                    else
                    {
                        int n = this.randomGenerator.Next(openKeys.Count);
                        targetKey = openKeys[n];
                    }

                    int minKeyDist = 1;
                    int keyDistance = minKeyDist + this.randomGenerator.Next(6 - minKeyDist);
                    //keyDistance = 20;
                    while (keyDistance >= minKeyDist)
                    {
                        Console.WriteLine("Trying to find room for " + targetKey.name + " at distance " + keyDistance);


                        PlaceKey(targetRoom.parent, targetKey, targetRoom, new List<LayoutRoom>(), keyDistance, true);

                        if (targetKey.room == null)
                        {
                            PlaceKey(targetRoom.parent, targetKey, targetRoom, new List<LayoutRoom>(), keyDistance, false);
                        }

                        if (targetKey.room != null)
                        {
                            LockRoom(targetRoom, targetKey);
                            targetRoom.require = targetKey;
                            targetKey.condition = currentCondition;
                            currentCondition--;

                            maxLockableIntensity = targetRoom.intensity * 0.8f;
                            float intensityLimit = 0.4f;
                            if (maxLockableIntensity < intensityLimit)
                            {
                                maxLockableIntensity = intensityLimit;
                            }


                            openKeys.Remove(targetKey);
                            currentRoom = targetRoom.parent;

                            found = true;
                            break;
                        }
                        else
                        {
                            targetRoom.require = null;
                            Console.WriteLine("Unable to place key " + targetKey.name);
                        }

                        keyDistance--;
                    }

                }
                else
                {
                    Console.WriteLine("Unable to find lockable room");
                }


                if (!found)
                {
                    break;
                }
            }

            foreach (LayoutKey key in openKeys)
            {
                Console.WriteLine("Key " + key.name + " was not placed...");
            }

            // after generating the locks, its now possible to understand which rooms are important and which ones are optional
            goal.important = true;
            entrance.important = true;
            foreach (var room in rooms.Values)
            {
                room.important = IsRoomImportant(room);
            }

            // convert non-deadend with keys into puzzle rooms
            foreach (var room in rooms.Values)
            {
                if (room.contains != null)
                {
                    int n = this.randomGenerator.Next(8);
                    if (n > 2)
                    {
                        room.kind = LayoutRoom.RoomKind.Puzzle;
                    }
                    else
                    {
                        room.kind = LayoutRoom.RoomKind.Treasure;
                    }

                }
            }

            // generate intensity for rooms based on tension curve 
            GenerateIntensity();
        }*/

        protected int GetRoomCondition(LayoutRoom room)
        {
            if (room.locked != null)
            {
                return room.locked.condition;
            }
            return -1;
        }

        #endregion

        public void GenerateBacktracking(float linearity)
        {
            List<LayoutConnection> temp = new List<LayoutConnection>();
            foreach (var path in connections)
            {
                if (!path.active && path.pathOrder < 0)
                {
                    temp.Add(path);
                }
            }

            int totalLeft = (int)(temp.Count * (1.0f - linearity));

            while (temp.Count > 0 && totalLeft > 0)
            {
                int n = this.randomGenerator.Next(temp.Count);
                LayoutConnection path = temp[n];
                temp.RemoveAt(n);


                if (GetRoomCondition(path.roomA) == GetRoomCondition(path.roomB))
                {
                    path.active = true;
                    totalLeft--;
                }
            }
        }

        public void GenerateRoomTypes()
        {
            // re-generate intensity 
            this.GenerateIntensity();

            // generate monster rooms
            foreach (var room in rooms.Values)
            {
                if (room.kind == LayoutRoom.RoomKind.Hall)
                {
                    int n = this.randomGenerator.Next(6);

                    switch (room.GetShape())
                    {
                        case LayoutRoom.RoomShape.Room:
                            {
                                switch (n)
                                {
                                    case 0: room.kind = LayoutRoom.RoomKind.Shrine; break;
                                    case 1: room.kind = LayoutRoom.RoomKind.Farm; break;
                                    case 2: room.kind = LayoutRoom.RoomKind.Puzzle; break;
                                    default: room.kind = LayoutRoom.RoomKind.Treasure; break;
                                }
                                break;
                            }

                        case LayoutRoom.RoomShape.Curve:
                            {
                                switch (n)
                                {
                                    case 0: room.kind = LayoutRoom.RoomKind.Puzzle; break;
                                    case 1: room.kind = LayoutRoom.RoomKind.Monster; break;
                                }
                                break;
                            }

                        case LayoutRoom.RoomShape.Split:
                            {
                                switch (n)
                                {
                                    case 0: room.kind = LayoutRoom.RoomKind.Puzzle; break;
                                    case 2: room.kind = LayoutRoom.RoomKind.Monster; break;
                                    case 3: room.kind = LayoutRoom.RoomKind.Shrine; break;
                                    case 4: room.kind = LayoutRoom.RoomKind.Farm; break;
                                }
                                break;
                            }

                        case LayoutRoom.RoomShape.Corridor:
                            {
                                switch (n)
                                {
                                    case 0: room.kind = LayoutRoom.RoomKind.Monster; break;
                                }
                                break;
                            }

                    }

                }
            }

            // re-generate intensity 
            this.GenerateIntensity();
        }

    }
}
