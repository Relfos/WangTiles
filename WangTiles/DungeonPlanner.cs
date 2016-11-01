using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lunar.Utils
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

        public string name;
        private LayoutRoom root;
        public int rank;

        public LayoutCoord coord;

        public List<LayoutConnection> connections = new List<LayoutConnection>();

        public int distance;
        public LayoutRoom previous;

        public RoomKind kind;

        public float intensity;
        public bool spike;

        public bool lockable;
        public bool important;

        public LayoutKey require;
        public LayoutKey contains;
        public LayoutKey locked;

        public int order;

        public int tileID;

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

        public bool IsDeadEnd()
        {
            return tileID<5;
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
        public LayoutRoom room;
        public Color color;

        public float intensity;
        public int condition;

        public LayoutKey(string name, Color color)
        {
            this.color = color;
            this.name = name;
            this.intensity = 1;
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
        public LayoutRoom AddRoom(LayoutCoord coord, int tileID)
        {
            var room = FindRoomAt(coord);
            room.tileID = tileID;
            return room;
        }

        public LayoutConnection AddConnection(LayoutCoord a, LayoutCoord b)
        {
            return AddConnection(FindRoomAt(a), FindRoomAt(b));
        }

        public LayoutConnection AddConnection(LayoutCoord a, WangDirection dir)
        {
            LayoutCoord b;
            
            switch (dir)
            {
                case WangDirection.East: b = new LayoutCoord(a.X + 1, a.Y); break;
                case WangDirection.West: b = new LayoutCoord(a.X - 1, a.Y); break;
                case WangDirection.North: b = new LayoutCoord(a.X, a.Y - 1); break;
                case WangDirection.South: b = new LayoutCoord(a.X, a.Y + 1); break;
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

        public LayoutRoom FindRoomAt(LayoutCoord coord)
        {
            if (rooms.ContainsKey(coord))
            {
                return rooms[coord];
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

                if (room.IsDeadEnd())
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
                //Debug.LogError("Entrance is not set...");
                return null;
            }

            // find goal room
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

            goal = null;
            int maxGoal = 0;
            foreach (var room in rooms.Values)
            {
                if (room == entrance)
                {
                    continue;
                }

                if (!room.IsDeadEnd())
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

            goal.kind = LayoutRoom.RoomKind.Goal;

            //UpdateRoomNames();
            //Debug.LogWarning("Found goal: " + goal.FloorLevel);
            return goal;
        }

        //Kruskals-Minimum-Spanning-Tree
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

            this.GenerateIntensity();
        }

        protected void FindChildrenProgression(ref int currentOrder, LayoutRoom startRoom, List<LayoutRoom> visitedRooms)
        {
            startRoom.order = currentOrder;
            currentOrder++;
            visitedRooms.Add(startRoom);

            //Debug.Log("Semantic for " + this.Name);
            startRoom.children = new List<LayoutRoom>();
            foreach (var path in startRoom.connections)
            {
                //Debug.Log("Found connection to " + other.Name + " -> " + other.visited + " order: "+ path.pathOrder);
                if (path.pathOrder < 0)
                {
                    continue;
                }

                var other = path.roomA == startRoom ? path.roomB : path.roomA;

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
                value += 3;
                room.spike = true;
            }
            else
            {
                if (room.contains != null)
                {
                    value += 1.5f;
                    room.spike = true;
                }
                else
                if (room.important)
                {
                    value += 1;
                }
                else
                {
                    value += 0.2f;
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

            max += 1;
            goal.intensity = max;
            
            // normalize intensities
            foreach (var room in rooms.Values)
            {
                room.intensity /= max;
                //    Debug.Log(room.Name + " => " + ((int)(room.intensity * 100.0f)).ToString());
            }
        }

        protected bool CanBeLocked(LayoutRoom room)
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

            /*if (this.children.Count>2)
            {
                return false;
            }*/

            return !room.IsDeadEnd();
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
                //Debug.Log("Locked " + this.Name);
            }

            foreach (var child in room.children)
            {
                LockRoom(child, keylock);
            }
        }

        public bool CanPlaceKey(LayoutRoom room, bool deadEndsonly)
        {
            if (deadEndsonly && !room.IsDeadEnd())
            {
                return false;
            }

            return (room.kind != LayoutRoom.RoomKind.Goal && room.kind != LayoutRoom.RoomKind.Entrance);
        }

        public void PlaceKey(LayoutRoom sourceRoom, LayoutKey key, LayoutRoom targetRoom, List<LayoutRoom> testedRooms, int count, bool deadEndsonly)
        {
            if (testedRooms.Contains(sourceRoom))
            {
                return;
            }

            if (sourceRoom.locked != null)
            {
                return;
            }

            //Debug.Log("Testing " + this.Name);
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
                    //Debug.Log("Placed " + key.name + " in room " + sourceRoom);

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
        }

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


        public void GenerateLocks(List<LayoutKey> keys)
        {
            if (keys == null || keys.Count <= 0)
            {
                return;
            }

            List<LayoutKey> openKeys = new List<LayoutKey>();
            foreach (LayoutKey key in keys)
            {
                openKeys.Add(key);
            }

            if (goal == null)
            {
                //Debug.LogError("Goal is not set...");
                return;
            }

            LayoutRoom currentRoom = goal;
            bool found = true;
            int currentCondition = openKeys.Count;
            float maxLockableIntensity = 1.0f;
            while (openKeys.Count > 0)
            {

                found = false;

                //Debug.Log("Trying to find lockable room with max intensity " + (int)(maxLockableIntensity * 100));

                LayoutRoom targetRoom = FindLockableRoom(currentRoom, new List<LayoutRoom>(), maxLockableIntensity);
                if (targetRoom != null)
                {
                    //Debug.Log("Found lockable room " + targetRoom.Name);
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
                    while (keyDistance >= minKeyDist)
                    {
                        //Debug.Log("Trying to find room for " + targetKey.name + " at distance " + keyDistance);
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
                            //Debug.Log("Unable to place key " + targetKey.name);
                        }

                        keyDistance--;
                    }

                }
                else
                {
                    //Debug.Log("Unable to find lockable room");
                }


                if (!found)
                {
                    break;
                }
            }

            foreach (LayoutKey key in openKeys)
            {
                //Debug.Log("Key " + key.name + " was not placed...");
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
                if (room.contains != null /*&& !room.IsDeadEnd()*/)
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

            //UpdateRoomNames();
        }

        protected int GetRoomCondition(LayoutRoom room)
        {
            if (room.locked != null)
            {
                return room.locked.condition;
            }
            return -1;
        }

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
            for (int i = 0; i <= 5; i++)
            {
                // re-generate intensity 
                this.GenerateIntensity();
            }

            // generate monster rooms
            foreach (var room in rooms.Values)
            {
                if (room.kind == LayoutRoom.RoomKind.Treasure)
                {
                    int n = this.randomGenerator.Next(6);

                    switch (n)
                    {
                        case 0: room.kind = LayoutRoom.RoomKind.Shrine; break;
                        case 1: room.kind = LayoutRoom.RoomKind.Farm; break;
                        default: room.kind = LayoutRoom.RoomKind.Monster; break;
                    }
                }
            }

            //UpdateRoomNames();
        }

    }
}
