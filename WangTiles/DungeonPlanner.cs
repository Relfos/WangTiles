using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lunar.Utils
{
    public class LayoutRoom
    {
        public string name;
        private LayoutRoom root;
        public int rank;

        public LayoutRoom(string name)
        {
            this.name = name;
            this.rank = 0;
            this.root = this;
        }

        public override string ToString()
        {
            return name;
        }

        public bool IsDeadEnd()
        {
            return false;
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


    public class RoomInfo
    {
        public LayoutRoom room;
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

        public LayoutRoom parent;
        public List<LayoutRoom> children = new List<LayoutRoom>();

        public RoomInfo(LayoutRoom room)
        {
            this.kind = RoomKind.Hall;
            this.intensity = -1;
            this.lockable = true;
            this.room = room;
        }
    }

    public class LayoutPlanner
    {
        private Random randomGenerator;

        public LayoutRoom entrance;
        public LayoutRoom goal;

        private List<LayoutRoom> rooms;

        private Dictionary<LayoutRoom, RoomInfo> roomInfo = new Dictionary<LayoutRoom, RoomInfo>();

        public LayoutPlanner(List<LayoutRoom> rooms, Random randomGenerator)
        {
            this.randomGenerator = randomGenerator;
            this.rooms = rooms;

            foreach (var room in rooms)
            {
                RoomInfo info = new RoomInfo(room);
                roomInfo[room] = info;
            }

            UpdateRoomNames();
        }

        protected void UpdateRoomNames()
        {
            foreach (var room in rooms)
            {
                room.name = roomInfo[room].kind.ToString();
            }
        }

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

        public RoomInfo GetRoomInfo(LayoutRoom room)
        {
            if (roomInfo.ContainsKey(room))
            {
                return roomInfo[room];
            }
            return null;
        }

        public LayoutRoom FindEntrance()
        {
            entrance = null;

            List<RoomScore> temp = new List<RoomScore>();
            foreach (var room in rooms)
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

            roomInfo[entrance].kind = RoomKind.Entrance;

            UpdateRoomNames();
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
            foreach (var room in rooms)
            {
                roomInfo[room].distance = 99999;   // Unknown distance from source to v
                roomInfo[room].previous = null;                 // Previous node in optimal path from source
                Q.Add(room);                         // All nodes initially in Q (unvisited nodes)
            }

            roomInfo[entrance].distance = 0;                        // Distance from source to source
            while (Q.Count > 0)
            {
                LayoutRoom best = null;
                int min = 9999;
                foreach (LayoutRoom other in Q)
                {
                    if (roomInfo[other].distance < min)
                    {
                        min = roomInfo[other].distance;
                        best = other;
                    }
                }

                Q.Remove(best);
                foreach (var path in best.Connections) // where path is still in Q.
                {
                    if (!path.active)
                    {
                        continue;
                    }

                    if (!Q.Contains(path))
                    {
                        continue;
                    }

                    int alt = roomInfo[best].distance + path.weight;
                    if (alt < roomInfo[path].distance)
                    {
                        roomInfo[path].distance = alt;
                        roomInfo[path].previous = best;
                    }
                }
            }

            goal = null;
            int maxGoal = 0;
            foreach (var room in rooms)
            {
                if (room == entrance)
                {
                    continue;
                }

                if (!room.IsDeadEnd())
                {
                    continue;
                }

                int dist = roomInfo[room].distance;
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

            roomInfo[goal].kind = RoomKind.Goal;

            UpdateRoomNames();
            //Debug.LogWarning("Found goal: " + goal.FloorLevel);
            return goal;
        }

        public void GenerateProgression()
        {
            // generate dungeon semantic tree (which represents the logical progression inside the dungeon)
            int currentOrder = 1;
            FindChildrenProgression(ref currentOrder, entrance, new List<LayoutRoom>());

            this.GenerateIntensity();
        }

        protected void FindChildrenProgression(ref int currentOrder, LayoutRoom startRoom, List<LayoutRoom> visitedRooms)
        {
            roomInfo[startRoom].order = currentOrder;
            currentOrder++;
            visitedRooms.Add(startRoom);

            //Debug.Log("Semantic for " + this.Name);
            roomInfo[startRoom].children = new List<LayoutRoom>();
            foreach (var path in startRoom.Connections)
            {
                //Debug.Log("Found connection to " + other.Name + " -> " + other.visited + " order: "+ path.pathOrder);
                if (path.pathOrder < 0)
                {
                    continue;
                }

                if (!visitedRooms.Contains(path.other))
                {
                    roomInfo[path.other].parent = startRoom;
                    roomInfo[startRoom].children.Add(path.other);
                    FindChildrenProgression(ref currentOrder, path.other, visitedRooms);
                }
            }
        }

        protected void CalculateIntensity(LayoutRoom room, float value)
        {
            if (roomInfo[room].intensity >= 0)
            {
                return;
            }

            roomInfo[room].spike = false;

            if (roomInfo[room].kind == RoomKind.Goal)
            {
                value += 3;
                roomInfo[room].spike = true;
            }
            else
            {
                if (roomInfo[room].contains != null)
                {
                    value += 1.5f;
                    roomInfo[room].spike = true;
                }
                else
                if (roomInfo[room].important)
                {
                    value += 1;
                }
                else
                {
                    value += 0.2f;
                }

                if (roomInfo[room].kind == RoomKind.Treasure)
                {
                    value += 0.5f;
                    roomInfo[room].spike = true;
                }

                if (roomInfo[room].parent != null && roomInfo[roomInfo[room].parent].spike)
                {
                    value -= 2;
                }
            }

            roomInfo[room].intensity = value;

            foreach (var child in roomInfo[room].children)
            {
                CalculateIntensity(child, value);
            }
        }

        protected void GenerateIntensity()
        {
            foreach (var room in rooms)
            {
                roomInfo[room].intensity = -1;
            }

            CalculateIntensity(entrance, 0);

            float max = roomInfo[goal].intensity;

            foreach (var room in rooms)
            {
                if (roomInfo[room].intensity >= max)
                {
                    roomInfo[room].intensity = max;
                }
            }

            max += 1;
            roomInfo[goal].intensity = max;

            foreach (var room in rooms)
            {
                roomInfo[room].intensity /= max;
                //    Debug.Log(room.Name + " => " + ((int)(room.intensity * 100.0f)).ToString());
            }
        }

        protected bool CanBeLocked(LayoutRoom room)
        {
            if (roomInfo[room].parent == null)
            {
                return false;
            }

            if (!roomInfo[room].lockable)
            {
                return false;
            }

            if (roomInfo[room].kind == RoomKind.Entrance)
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

            if (roomInfo[currentRoom].locked == null)
            {
                if (roomInfo[currentRoom].require != null || (roomInfo[currentRoom].intensity < maxIntensity && CanBeLocked(currentRoom)))
                {
                    return currentRoom;
                }

            }

            if (roomInfo[currentRoom].parent == null)
            {
                return null;
            }

            return FindLockableRoom(roomInfo[currentRoom].parent, testedRooms, maxIntensity);
        }

        protected void LockRoom(LayoutRoom room, LayoutKey keylock)
        {
            if (roomInfo[room].locked == null)
            {
                roomInfo[room].locked = keylock;
                //Debug.Log("Locked " + this.Name);
            }

            foreach (var child in roomInfo[room].children)
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

            return (roomInfo[room].kind != RoomKind.Goal && roomInfo[room].kind != RoomKind.Entrance);
        }

        public void PlaceKey(LayoutRoom sourceRoom, LayoutKey key, LayoutRoom targetRoom, List<LayoutRoom> testedRooms, int count, bool deadEndsonly)
        {
            if (testedRooms.Contains(sourceRoom))
            {
                return;
            }

            if (roomInfo[sourceRoom].locked != null)
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
                if (roomInfo[sourceRoom].contains != null)
                {
                    return;
                }

                if (count <= 0)
                {
                    //Debug.Log("Placed " + key.name + " in room " + sourceRoom);

                    key.room = sourceRoom;
                    roomInfo[sourceRoom].contains = key;
                    return;
                }
            }

            List<LayoutRoom> rooms = new List<LayoutRoom>();
            if (roomInfo[sourceRoom].parent != null && !testedRooms.Contains(roomInfo[sourceRoom].parent))
            {
                rooms.Add(roomInfo[sourceRoom].parent);
            }


            if (sourceRoom != targetRoom)
            {
                foreach (var child in roomInfo[sourceRoom].children)
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
            if (roomInfo[room].important)
            {
                return true;
            }

            if (roomInfo[room].contains != null)
            {
                return true;
            }

            foreach (var child in roomInfo[room].children)
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

                    if (roomInfo[targetRoom].require != null && openKeys.Contains(roomInfo[targetRoom].require))
                    {
                        targetKey = roomInfo[targetRoom].require;
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
                        PlaceKey(roomInfo[targetRoom].parent, targetKey, targetRoom, new List<LayoutRoom>(), keyDistance, true);
                        if (targetKey.room == null)
                        {
                            PlaceKey(roomInfo[targetRoom].parent, targetKey, targetRoom, new List<LayoutRoom>(), keyDistance, false);
                        }

                        if (targetKey.room != null)
                        {
                            LockRoom(targetRoom, targetKey);
                            roomInfo[targetRoom].require = targetKey;
                            targetKey.condition = currentCondition;
                            currentCondition--;

                            maxLockableIntensity = roomInfo[targetRoom].intensity * 0.8f;
                            float intensityLimit = 0.4f;
                            if (maxLockableIntensity < intensityLimit)
                            {
                                maxLockableIntensity = intensityLimit;
                            }


                            openKeys.Remove(targetKey);
                            currentRoom = roomInfo[targetRoom].parent;

                            found = true;
                            break;
                        }
                        else
                        {
                            roomInfo[targetRoom].require = null;
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
            roomInfo[goal].important = true;
            roomInfo[entrance].important = true;
            foreach (var room in rooms)
            {
                roomInfo[room].important = IsRoomImportant(room);
            }

            // convert non-deadend with keys into puzzle rooms
            foreach (var room in rooms)
            {
                if (roomInfo[room].contains != null /*&& !room.IsDeadEnd()*/)
                {
                    int n = this.randomGenerator.Next(8);
                    if (n > 2)
                    {
                        roomInfo[room].kind = RoomKind.Puzzle;
                    }
                    else
                    {
                        roomInfo[room].kind = RoomKind.Treasure;
                    }

                }
            }

            // generate intensity for rooms based on tension curve 
            GenerateIntensity();

            UpdateRoomNames();
        }

        protected int GetRoomCondition(LayoutRoom room)
        {
            if (roomInfo[room].locked != null)
            {
                return roomInfo[room].locked.condition;
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
            foreach (var room in rooms)
            {
                if (roomInfo[room].kind == RoomKind.Treasure)
                {
                    int n = this.randomGenerator.Next(6);

                    switch (n)
                    {
                        case 0: roomInfo[room].kind = RoomKind.Shrine; break;
                        case 1: roomInfo[room].kind = RoomKind.Farm; break;
                        default: roomInfo[room].kind = RoomKind.Monster; break;
                    }
                }
            }

            UpdateRoomNames();
        }

    }
}
