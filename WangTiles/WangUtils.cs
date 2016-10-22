using System;
using System.Collections.Generic;

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

        public static List<int> GetPossibleMatches(int tileID, WangDirection direction)
        {
            List<int> result = new List<int>();
            for (int i=0; i<16; i++)
            {
                if (MatchTile(tileID, i, direction))
                {
                    result.Add(i);
                }
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
            bool currentSide = GetConnectionForTile(currentID, newDirection);
            bool newSide = GetConnectionForTile(newID, InvertDirection(newDirection));
            return currentSide == newSide;
        }
    }
}
