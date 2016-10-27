using System;
using System.Collections.Generic;
using System.Drawing;

//http://s358455341.websitehome.co.uk/stagecast/wang/intro.html
namespace Lunar.Utils
{
    public enum WangDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class WangUtils
    {
        

        /// <summary>
        /// Returns bools for all 4 directions, if true, the tile has a connection in that direction
        /// </summary>
        /// <param name="tileID"></param>
        /// <param name="north"></param>
        /// <param name="east"></param>
        /// <param name="south"></param>
        /// <param name="west"></param>
        public static void GetConnectionsForTile(int tileID, out bool north, out bool east, out bool south, out bool west)
        {
            north = GetConnectionForTile(tileID, WangDirection.North);
            east = GetConnectionForTile(tileID, WangDirection.East);
            south = GetConnectionForTile(tileID, WangDirection.South);
            west = GetConnectionForTile(tileID, WangDirection.West);
        }

        public static bool GetConnectionForTile(int tileID, WangDirection direction)
        {
            int mask = 1 << ((int)direction);
            return (tileID & mask)!=0;
        }

        /// <summary>
        /// Returns the opposite direction (eg: North -> South)
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static WangDirection InvertDirection(WangDirection direction)
        {
            switch (direction)
            {
                case WangDirection.North: return WangDirection.South;
                case WangDirection.South: return WangDirection.North;
                case WangDirection.East: return WangDirection.West;
                default: return WangDirection.East;
            }
        }

        //private static Dictionary<ushort, List<int>> _matches = new Dictionary<ushort, List<int>>();

        /// <summary>
        /// Returns a list of all possible tiles that can connect with a specified set of neighbors
        /// </summary>
        /// <param name="tileID"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static List<int> GetPossibleMatches(int left, int right, int up, int down)
        {
            //ushort matchID = (ushort)(left + (right << 4) + (up << 8) + (down << 12));
            
            List<int> result = new List<int>();
            for (int newID=0; newID < 16; newID++)
            {
                if (!MatchTile(left, newID, WangDirection.East)) { continue; }
                if (!MatchTile(right, newID, WangDirection.West)) { continue; }
                if (!MatchTile(up, newID, WangDirection.South)) { continue; }
                if (!MatchTile(down, newID, WangDirection.North)) { continue; }

                result.Add(newID);
            }

            return result;
        }

        /// <summary>
        /// Checks if the two tiles can be placed in an adjancent manner
        /// </summary>
        /// <param name="currentID"></param>
        /// <param name="newID"></param>
        /// <param name="newDirection"></param>
        public static bool MatchTile(int currentID, int newID, WangDirection newDirection)
        {
            if (currentID == -1 || newID == -1)
            {
                return true;
            }

            bool currentSide = GetConnectionForTile(currentID, newDirection);
            bool newSide = GetConnectionForTile(newID, InvertDirection(newDirection));
            return currentSide == newSide;
        }

        public static int AddConnection(int tileID, WangDirection direction)
        {
            int mask = 1 << ((int)direction);
            return tileID | mask; 
        }
    }

    public class WangArea
    {
        public int ID;
        public List<WangArea> children = new List<WangArea>();

        public override string ToString()
        {
            return "Area "+ID.ToString()+ " ("+ GetColor().ToString() +")";
        }

        public Color GetColor()
        {
            switch (ID)
            {
                case 0: return Color.Red;
                case 1: return Color.Green;
                case 2: return Color.Blue;
                case 3: return Color.Yellow;
                case 4: return Color.Magenta;
                case 5: return Color.Cyan;
                case 6: return Color.Orange;
                case 7: return Color.Purple;
                case 8: return Color.Gray;
                case 9: return Color.Beige;
                case 10: return Color.Chocolate;
                default: return Color.Black;
            }
        }    
    }

    public struct MapTile
    {
        public int tileID;
        public WangArea areaID;
    }

    public struct MapEdgeTile
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public WangDirection exit;
    }

    #region MAP

    public class WangMap
    {
        private int mapWidth = 16;
        private int mapHeight = 10;
        private bool mapWrapX = false;
        private bool mapWrapY = false;
        private bool mapInvert = false;
        private MapTile[] map;

        public WangMap(int width, int height)
        {
            this.mapWidth = width;
            this.mapHeight = height;
            map = new MapTile[mapWidth * mapHeight];
        }


        private static void AddEdgeTile(Dictionary<WangArea, List<MapEdgeTile>> result, WangArea otherArea, int x1, int y1, int x2, int y2, WangDirection exitDir)
        {
            if (!result.ContainsKey(otherArea))
            {
                result[otherArea] = new List<MapEdgeTile>();
            }

            MapEdgeTile et = new MapEdgeTile();
            et.x1 = x1;
            et.y1 = y1;
            et.x2 = x2;
            et.y2 = y2;
            et.exit = exitDir;
            result[otherArea].Add(et);
        }

        private static Dictionary<WangArea, List<MapEdgeTile>> FindTilesInAreaEdges(WangArea curArea)
        {
            Dictionary<WangArea, List<MapEdgeTile>> result = new Dictionary<WangArea, List<MapEdgeTile>>();
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int ofs = GetMapOffset(i, j);
                    if (map[ofs].areaID != curArea) // check if this tile belongs to the area we are testing
                    {
                        continue;
                    }

                    var otherArea = GetMapAt(i - 1, j).areaID;
                    if (i > 0 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i - 1, j, WangDirection.West);
                    }

                    otherArea = GetMapAt(i, j - 1).areaID;
                    if (j > 0 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i, j - 1, WangDirection.North);
                    }

                    otherArea = GetMapAt(i + 1, j).areaID;
                    if (i < mapWidth - 1 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i + 1, j, WangDirection.East);
                    }

                    otherArea = GetMapAt(i, j + 1).areaID;
                    if (j < mapHeight - 1 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i, j + 1, WangDirection.South);
                    }
                }
            }

            return result;
        }

        private static MapTile GetMapAt(int x, int y)
        {
            if (mapWrapX && x < 0)
            {
                x += mapWidth;
            }

            if (mapWrapX && x >= mapWidth)
            {
                x -= mapWidth;
            }

            if (mapWrapY && y < 0)
            {
                y += mapHeight;
            }

            if (mapWrapY && y >= mapHeight)
            {
                y -= mapHeight;
            }

            if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight)
            {
                var result = new MapTile();
                result.tileID = 0;
                result.areaID = null;
                return result;
            }
            return map[GetMapOffset(x, y)];
        }

        private static int GetMapOffset(int x, int y)
        {
            return x + y * mapWidth;
        }

        private static void SetMapAt(int x, int y, int val)
        {
            map[GetMapOffset(x, y)].tileID = val;
        }

        private static void TryPlacingRandomTile(int i, int j)
        {
            int left = GetMapAt(i - 1, j).tileID;
            int right = GetMapAt(i + 1, j).tileID;
            int up = GetMapAt(i, j - 1).tileID;
            int down = GetMapAt(i, j + 1).tileID;

            /*List<int> matches = new List<int>();
            for (int newID=0; newID<16; newID++)
            {
                if (left != -1 && !WangUtils.MatchTile(left, newID, WangDirection.East)) { continue; }
                if (right != -1 && !WangUtils.MatchTile(right, newID, WangDirection.West)) { continue; }
                if (up != -1 && !WangUtils.MatchTile(up, newID, WangDirection.South)) { continue; }
                if (down != -1 && !WangUtils.MatchTile(down, newID, WangDirection.North)) { continue; }

                matches.Add(newID);
            }*/

            var matches = WangUtils.GetPossibleMatches(left, right, up, down);

            if (matches.Count <= 0)
            {
                return;
            }

            int n = matches[rnd.Next(matches.Count)];
            SetMapAt(i, j, n);
        }
    }

#endregion

}
