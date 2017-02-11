using System;
using System.Collections.Generic;
using System.Drawing;

//http://s358455341.websitehome.co.uk/stagecast/wang/intro.html
namespace WangTiles.Core
{
    public enum WangEdgeDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    #region EDGE
    public static class WangEdgeUtils
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
            north = GetConnectionForTile(tileID, WangEdgeDirection.North);
            east = GetConnectionForTile(tileID, WangEdgeDirection.East);
            south = GetConnectionForTile(tileID, WangEdgeDirection.South);
            west = GetConnectionForTile(tileID, WangEdgeDirection.West);
        }

        public static bool GetConnectionForTile(int tileID, WangEdgeDirection direction)
        {
            int mask = 1 << ((int)direction);
            return (tileID & mask) != 0;
        }

        /// <summary>
        /// Returns the opposite direction (eg: North -> South)
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static WangEdgeDirection InvertDirection(WangEdgeDirection direction)
        {
            switch (direction)
            {
                case WangEdgeDirection.North: return WangEdgeDirection.South;
                case WangEdgeDirection.South: return WangEdgeDirection.North;
                case WangEdgeDirection.East: return WangEdgeDirection.West;
                default: return WangEdgeDirection.East;
            }
        }

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
            for (int newID = 0; newID < 16; newID++)
            {
                if (!MatchTile(left, newID, WangEdgeDirection.East)) { continue; }
                if (!MatchTile(right, newID, WangEdgeDirection.West)) { continue; }
                if (!MatchTile(up, newID, WangEdgeDirection.South)) { continue; }
                if (!MatchTile(down, newID, WangEdgeDirection.North)) { continue; }

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
        public static bool MatchTile(int currentID, int newID, WangEdgeDirection newDirection)
        {
            if (currentID == -1 || newID == -1)
            {
                return true;
            }

            bool currentSide = GetConnectionForTile(currentID, newDirection);
            bool newSide = GetConnectionForTile(newID, InvertDirection(newDirection));
            return currentSide == newSide;
        }

        public static int AddConnection(int tileID, WangEdgeDirection direction)
        {
            int mask = 1 << ((int)direction);
            return tileID | mask;
        }
    }
    #endregion

    public enum WangCornerDirection
    {
        Northeast = 0,
        Southeast = 1,
        Southwest = 2,
        Northwest = 3
    }

    #region CORNER
    public static class WangCornerUtils
    {
        /// <summary>
        /// Returns bools for all 4 directions, if true, the tile has a connection in that direction
        /// </summary>
        /// <param name="tileID"></param>
        /// <param name="north"></param>
        /// <param name="east"></param>
        /// <param name="south"></param>
        /// <param name="west"></param>
        public static void GetConnectionsForTile(int tileID, out bool northeast, out bool northwest, out bool southeast, out bool southwest)
        {
            northeast = GetConnectionForTile(tileID, WangCornerDirection.Northeast);
            northwest = GetConnectionForTile(tileID, WangCornerDirection.Northwest);
            southeast = GetConnectionForTile(tileID, WangCornerDirection.Southeast);
            southwest = GetConnectionForTile(tileID, WangCornerDirection.Southwest);
        }

        public static bool GetConnectionForTile(int tileID, WangCornerDirection direction)
        {
            int mask = 1 << ((int)direction);
            return (tileID & mask) != 0;
        }

        /// <summary>
        /// Returns the opposite direction (eg: North -> South)
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static WangCornerDirection InvertDirection(WangCornerDirection direction)
        {
            switch (direction)
            {
                case WangCornerDirection.Northeast: return WangCornerDirection.Southwest;
                case WangCornerDirection.Northwest: return WangCornerDirection.Southeast;
                case WangCornerDirection.Southeast: return WangCornerDirection.Northwest;
                default: return WangCornerDirection.Northeast;
            }
        }

        /// <summary>
        /// Returns a list of all possible tiles that can connect with a specified set of neighbors
        /// </summary>
        /// <param name="tileID"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static List<int> GetPossibleMatches(int left, int right, int up, int down, int northwest, int northeast, int southwest, int southeast)
        {
            List<int> result = new List<int>();
            for (int newID = 0; newID < 16; newID++)
            {
                if (!MatchTile(newID, WangCornerDirection.Northeast, northeast, WangCornerDirection.Southwest)) { continue; }
                if (!MatchTile(newID, WangCornerDirection.Northwest, northwest, WangCornerDirection.Southeast)) { continue; }

                if (!MatchTile(newID, WangCornerDirection.Southeast, southeast, WangCornerDirection.Northwest)) { continue; }
                if (!MatchTile(newID, WangCornerDirection.Southwest, southwest, WangCornerDirection.Northeast)) { continue; }

                if (!MatchTile(newID, WangCornerDirection.Northeast, right, WangCornerDirection.Northwest)) { continue; }
                if (!MatchTile(newID, WangCornerDirection.Southeast, right, WangCornerDirection.Southwest)) { continue; }

                if (!MatchTile(newID, WangCornerDirection.Northwest, left, WangCornerDirection.Northeast)) { continue; }
                if (!MatchTile(newID, WangCornerDirection.Southwest, left, WangCornerDirection.Southeast)) { continue; }

                if (!MatchTile(newID, WangCornerDirection.Northeast, up, WangCornerDirection.Southeast)) { continue; }
                if (!MatchTile(newID, WangCornerDirection.Northwest, up, WangCornerDirection.Southwest)) { continue; }

                if (!MatchTile(newID, WangCornerDirection.Southeast, down, WangCornerDirection.Northeast)) { continue; }
                if (!MatchTile(newID, WangCornerDirection.Southwest, down, WangCornerDirection.Northwest)) { continue; }

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
        public static bool MatchTile(int currentID, WangCornerDirection currentDirection, int newID, WangCornerDirection newDirection)
        {
            if (currentID == -1 || newID == -1)
            {
                return true;
            }

            bool currentSide = GetConnectionForTile(currentID, currentDirection);
            bool newSide = GetConnectionForTile(newID, newDirection);
            return currentSide == newSide;
        }

        public static int AddConnection(int tileID, WangEdgeDirection direction)
        {
            int mask = 1 << ((int)direction);
            return tileID | mask;
        }
    }
    #endregion

    public class WangArea
    {
        public int ID;
        public List<WangArea> children = new List<WangArea>();

        public static Color GetColor(WangArea area)
        {
            if (area == null)
            {
                return Color.FromArgb(0);
            }

            return area.GetColor();
        }

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

    public struct WangTile
    {
        public int tileID;
        public int variationID;
        public WangArea areaID;
    }

    public struct WangMapExit
    {
        public int x;
        public int y;
        public WangEdgeDirection direction;

        public WangMapExit(int x, int y, WangEdgeDirection dir)
        {
            this.x = x;
            this.y = y;
            this.direction = dir;
        }
    }

    public struct WangEdgeTile
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public WangEdgeDirection exit;
    }

    public abstract class WangMap
    {
        protected int _width = 16;
        public int Width { get { return _width; } }

        protected int _height = 10;
        public int Height { get { return _height; } }

        protected WangTile[] tiles;
        protected Random rnd;

        public WangMap(int width, int height, int seed = 0)
        {
            if (seed == 0)
            {
                seed = Environment.TickCount;
            }

            this.rnd = new Random(seed);

            this._width = width;
            this._height = height;
            tiles = new WangTile[_width * _height];

            // first clear all tiles
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    int ofs = GetTileOffset(i, j);
                    tiles[ofs].areaID = null;
                    tiles[ofs].tileID = -1;
                }
            }
        }

        public void SetTileIDAt(int x, int y, int val)
        {
            int ofs = GetTileOffset(x, y);
            tiles[ofs].tileID = val;

            int n = (x * y) + (x | y);
            int variation;
            switch (val)
            {
                case 0: variation = 0; break;
                case 1: case 2: case 4: case 8: variation = n % 3; break;
                case 3: case 6: case 9: case 12: variation = n % 3; break;
                case 5: case 10: variation = n % 4; break;
                case 15: variation = n % 2; break;

                default: variation = n % 3; break;
            }


            tiles[ofs].variationID = variation;
        }

        public abstract WangTile GetTileAt(int x, int y);

        public int GetTileOffset(int x, int y)
        {
            return x + y * _width;
        }


        public void Invert()
        {
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    int current = GetTileAt(i, j).tileID;
                    if (current < 0) // if tile is not set, skip
                    {
                        continue;
                    }

                    SetTileIDAt(i, j, 15 - current);
                }
            }
        }
    }



    #region EDGE MAP
    public class WangEdgeMap : WangMap
    {
        private bool _wrapX = false;
        private bool _wrapY = false;


        private List<WangMapExit> exits = new List<WangMapExit>();

        public WangEdgeMap(int width, int height, int seed = 0, bool wrapX = false, bool wrapY = false) : base (width, height, seed)
        {
            this._wrapX = wrapX;
            this._wrapY = wrapY;
        }

        public override WangTile GetTileAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                foreach (var exit in exits)
                {
                    if (exit.x == x && exit.y == y)
                    {
                        var result = new WangTile();
                        result.tileID = WangEdgeUtils.AddConnection(0, exit.direction);
                        result.areaID = null;
                        return result;
                    }
                }
            }

            if (_wrapX && x < 0)
            {
                x += _width;
            }

            if (_wrapX && x >= _width)
            {
                x -= _width;
            }

            if (_wrapY && y < 0)
            {
                y += _height;
            }

            if (_wrapY && y >= _height)
            {
                y -= _height;
            }

            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                var result = new WangTile();
                result.tileID = 0;
                result.areaID = null;
                return result;
            }
            return tiles[GetTileOffset(x, y)];
        }

        public bool AddExit(int x, int y)
        {
            WangEdgeDirection dir;

            if (y < 0) { dir = WangEdgeDirection.South; }
            else
            if (y >= Height) { dir = WangEdgeDirection.North; }
            else
            if (x < 0) { dir = WangEdgeDirection.East; }
            else
            if (x >= Width) { dir = WangEdgeDirection.West; }
            else
            {
                return false;
            }

            exits.Add(new WangMapExit(x, y, dir));
            return true;
        }

        private static void AddEdgeTile(Dictionary<WangArea, List<WangEdgeTile>> result, WangArea otherArea, int x1, int y1, int x2, int y2, WangEdgeDirection exitDir)
        {
            if (!result.ContainsKey(otherArea))
            {
                result[otherArea] = new List<WangEdgeTile>();
            }

            WangEdgeTile et = new WangEdgeTile();
            et.x1 = x1;
            et.y1 = y1;
            et.x2 = x2;
            et.y2 = y2;
            et.exit = exitDir;
            result[otherArea].Add(et);
        }

        private Dictionary<WangArea, List<WangEdgeTile>> FindTilesInAreaEdges(WangArea curArea)
        {
            Dictionary<WangArea, List<WangEdgeTile>> result = new Dictionary<WangArea, List<WangEdgeTile>>();
            for (int j = 0; j < this.Height; j++)
            {
                for (int i = 0; i < this.Width; i++)
                {
                    int ofs = GetTileOffset(i, j);
                    if (tiles[ofs].areaID != curArea) // check if this tile belongs to the area we are testing
                    {
                        continue;
                    }

                    var otherArea = GetTileAt(i - 1, j).areaID;
                    if (i > 0 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i - 1, j, WangEdgeDirection.West);
                    }

                    otherArea = GetTileAt(i, j - 1).areaID;
                    if (j > 0 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i, j - 1, WangEdgeDirection.North);
                    }

                    otherArea = GetTileAt(i + 1, j).areaID;
                    if (i < _width - 1 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i + 1, j, WangEdgeDirection.East);
                    }

                    otherArea = GetTileAt(i, j + 1).areaID;
                    if (j < _height - 1 && otherArea != curArea && otherArea != null)
                    {
                        AddEdgeTile(result, otherArea, i, j, i, j + 1, WangEdgeDirection.South);
                    }
                }
            }

            return result;
        }


        private void FloodFillArea(int x, int y, WangArea areaID)
        {
            int ofs = GetTileOffset(x, y);
            tiles[ofs].areaID = areaID;

            // flood all adjacent tiles if possible
            FloodFillArea(x - 1, y, areaID, WangEdgeDirection.East);
            FloodFillArea(x + 1, y, areaID, WangEdgeDirection.West);
            FloodFillArea(x, y - 1, areaID, WangEdgeDirection.South);
            FloodFillArea(x, y + 1, areaID, WangEdgeDirection.North);
        }

        private void FloodFillArea(int x, int y, WangArea areaID, WangEdgeDirection direction)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) // if out of bounds, return
            {
                return;
            }

            int ofs = GetTileOffset(x, y);
            if (tiles[ofs].areaID != null) // if already filled then stop
            {
                return;
            }

            if (tiles[ofs].tileID == 0) // empty tiles should not be filled
            {
                return;
            }

            if (WangEdgeUtils.GetConnectionForTile(tiles[ofs].tileID, direction) == false)
            {
                return;
            }

            FloodFillArea(x, y, areaID);
        }

        private bool JoinArea(WangArea parent, WangArea area, HashSet<WangArea> areaSet)
        {
            if (areaSet.Contains(area))
            {
                return false;
            }

            areaSet.Add(area);

            //Console.WriteLine("Testing area: " + GetAreaColor(area));

            var result = FindTilesInAreaEdges(area);
            if (result.Count <= 0)
            {
                return true;
            }

            foreach (var temp in result)
            {
                if (JoinArea(area, temp.Key, areaSet))
                {
                    area.children.Add(temp.Key);

                    var tiles = temp.Value;
                    var tile = tiles[rnd.Next(tiles.Count)];

                    //Console.WriteLine("Joined " + GetAreaColor(area) + " to " + GetAreaColor(temp.Key));

                    int ofs = GetTileOffset(tile.x1, tile.y1);
                    SetTileIDAt(tile.x1, tile.y1, WangEdgeUtils.AddConnection(this.tiles[ofs].tileID, tile.exit));

                    ofs = GetTileOffset(tile.x2, tile.y2);
                    SetTileIDAt(tile.x2, tile.y2, WangEdgeUtils.AddConnection(this.tiles[ofs].tileID, WangEdgeUtils.InvertDirection(tile.exit)));
                }
            }

            return true;
        }

        public void FixConnectivity()
        {
            // optional, detect isolate areas
            List<WangArea> areas = new List<WangArea>();
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    int ofs = GetTileOffset(i, j);
                    if (tiles[ofs].areaID != null) // check if this tile already have an area assigned
                    {
                        continue;
                    }

                    if (tiles[ofs].tileID == 0) // empty tiles should not be filled
                    {
                        continue;
                    }

                    var area = new WangArea();
                    area.ID = areas.Count;
                    areas.Add(area);
                    FloodFillArea(i, j, area);
                }
            }

            // join the isolated areas by adding exits to some tiles
            HashSet<WangArea> areaSet = new HashSet<WangArea>();
            for (int k = 0; k < areas.Count; k++)
            {
                var area = areas[k];
                if (k == 0)
                {
                    JoinArea(null, area, areaSet);
                }
                else
                if (!areaSet.Contains(area))
                {
                    DeleteArea(area);
                }                
            }
        }

        private void DeleteArea(WangArea area)
        {
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    if (GetTileAt(i, j).areaID == area)
                    {
                        SetTileIDAt(i, j, 0);
                    }
                }
            }
       }

        public void Generate()
        {
            // first pass places random tiles in a checkerboard pattern
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    if (i % 2 == j % 2)
                    {
                        continue;
                    }

                    TryPlacingRandomTile(i, j);
                }
            }

            // second pass places random tiles in the mising holes, checking for matching edges
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    int current = GetTileAt(i, j).tileID;
                    if (current >= 0) // if tile already set, skip
                    {
                        continue;
                    }

                    //continue;

                    TryPlacingRandomTile(i, j);
                }
            }
        }

        protected void TryPlacingRandomTile(int i, int j)
        {
            int left = GetTileAt(i - 1, j).tileID;
            int right = GetTileAt(i + 1, j).tileID;
            int up = GetTileAt(i, j - 1).tileID;
            int down = GetTileAt(i, j + 1).tileID;

            var matches = WangEdgeUtils.GetPossibleMatches(left, right, up, down);

            if (matches.Count <= 0)
            {
                return;
            }

            int n = matches[rnd.Next(matches.Count)];
            SetTileIDAt(i, j, n);
        }

    }

    #endregion

    #region CORNER MAP
    public class WangCornerMap : WangMap
    {
        public WangCornerMap(int width, int height, int seed = 0) : base(width, height, seed)
        {
        }

        public override WangTile GetTileAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                var result = new WangTile();
                result.tileID = -1;
                result.areaID = null;
                return result;
            }
            return tiles[GetTileOffset(x, y)];
        }


        public void Generate()
        {
            // first pass places random tiles in a checkerboard pattern
            int n = 12;
            for (int j = 0; j < _height; j++)
            {
                for (int i = 0; i < _width; i++)
                {
                    TryPlacingRandomTile(i, j);
                    //n--; if (n == 0) return;
                }
            }
        }


        protected void TryPlacingRandomTile(int i, int j)
        {
            int west = GetTileAt(i - 1, j).tileID;
            int east = GetTileAt(i + 1, j).tileID;
            int north = GetTileAt(i, j - 1).tileID;
            int south = GetTileAt(i, j + 1).tileID;

            int northwest = GetTileAt(i - 1, j - 1).tileID;
            int northeast = GetTileAt(i + 1, j - 1).tileID;
            int southwest = GetTileAt(i - 1, j + 1).tileID;
            int southeast = GetTileAt(i + 1, j + 1).tileID;

            var matches = WangCornerUtils.GetPossibleMatches(west, east, north, south, northwest, northeast, southwest, southeast);

            if (matches.Count <= 0)
            {
                return;
            }

            int n = matches[rnd.Next(matches.Count)];
            SetTileIDAt(i, j, n);
        }
    }

    #endregion

}
