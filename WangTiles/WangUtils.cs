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
}
