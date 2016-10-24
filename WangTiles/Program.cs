using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using Lunar.Utils;
using System.Net;

namespace WangTiles
{
    public class Example
    {
        #region TEXTURE_BUFFER_UTILS
        private static int bufferWidth = 600;
        private static int bufferHeight = 400;
        private static byte[] buffer = new byte[bufferWidth * bufferHeight * 4];

        static void UpdateBuffer(byte[] buffer, int width, int height, int texID)
        {
            GL.BindTexture(TextureTarget.Texture2D, texID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
        }

        public static void DrawBuffer(OpenTK.GameWindow game, int textureID)
        {
            float u1 = 0;
            float u2 = 1;
            float v1 = 0;
            float v2 = 1;

            float w = 1;
            float h = 1;


            float px = 0;
            float py = 0;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, 1.0, 0.0, 1.0, 0.0, 4.0);

            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.Begin(PrimitiveType.Quads);

            GL.Color3(Color.White);
            GL.TexCoord2(u1, v1);
            GL.Vertex2(px + 0, py + h);

            GL.TexCoord2(u1, v2);
            GL.Vertex2(px + 0, py + 0);

            GL.TexCoord2(u2, v2);
            GL.Vertex2(px + w, py + 0);

            GL.TexCoord2(u2, v1);
            GL.Vertex2(px + w, py + h);

            GL.End();
        }
        #endregion

        #region TILESET
        private const int tileSize = 32;

        /// <summary>
        /// Draws a tile in the texture buffer at specified position
        /// </summary>
        private static void DrawTile(int targetX, int targetY, int tileID, Bitmap tileset, Color borderColor)
        {
            for (int y = 0; y < tileSize; y++)
            {
                for (int x = 0; x < tileSize; x++)
                {
                    if (x+targetX>=bufferWidth || x+targetX<0) { continue; }
                    if (y + targetY >= bufferHeight || y+targetY<0) { continue; }
                    int destOfs = ((x + targetX) + bufferWidth * (y + targetY)) * 4;
                    var c = tileset.GetPixel((tileID * tileSize) + x, y);

                    if (borderColor.A>0 && ( x==0 || y== 0 || x == tileSize - 1 || y == tileSize - 1))
                    {
                        c = borderColor;
                    }

                    buffer[destOfs + 0] = c.R;
                    buffer[destOfs + 1] = c.G;
                    buffer[destOfs + 2] = c.B;
                    buffer[destOfs + 3] = c.A;
                }
            }

        }
        #endregion

        #region MAP
        private struct MapTile
        {
            public int x;
            public int y;
            public int tileID;
            public WangArea areaID;
        }

        private static int mapWidth = 16;
        private static int mapHeight = 10;
        private static bool mapWrapX = false;
        private static bool mapWrapY = false;
        private static bool mapInvert = false;
        private static MapTile[] map = new MapTile[mapWidth * mapHeight];

        private static Color GetAreaColor(int areaID)
        {
            switch (areaID)
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
                default: return Color.FromArgb(0);
            }
        }

        private static List<MapTile> FindTilesInAreaEdges(int areaID)
        {
            List<MapTile> result = new List<MapTile>();
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int ofs = GetMapOffset(i, j);
                    if (map[ofs].areaID != areaID) // check if this tile already have an area assigned
                    {
                        continue;
                    }

                    if ((i > 0 && GetMapAt(i - 1, j).areaID != areaID) || (j > 0 && GetMapAt(i, j - 1).areaID != areaID)
                        || (i < mapWidth-1 && GetMapAt(i + 1, j).areaID != areaID) || (j < mapHeight -1 && GetMapAt(i, j - 1).areaID != areaID))
                    {
                        result.Add(map[ofs]);
                    }

                }
            }

            return result;
        }

        private static MapTile GetMapAt(int x, int y)
        {
            if (mapWrapX && x<0)
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

            if (x<0 || y<0 || x>=mapWidth || y>=mapHeight)
            {
                var result = new MapTile();
                result.tileID = 0;
                result.areaID = -1;
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
            map[GetMapOffset(x,y)].tileID = val;
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

            if (matches.Count<=0)
            {
                return;
            }

            int n = matches[rnd.Next(matches.Count)];
            SetMapAt(i, j, n);
        }

        #endregion

        private static Random rnd = new Random(3424);

        private static void RedrawWithTileset(Bitmap tileset)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int ofs = GetMapOffset(i, j);
                    int tileID = map[ofs].tileID;
                    if (tileID < 0) { continue; }
                    DrawTile(i * tileSize, j * tileSize, tileID, tileset, GetAreaColor(map[ofs].areaID));
                }
            }
        }

        private static void DownloadTileset(string name)
        {
            using (var client = new WebClient())
            {
                Bitmap target = new Bitmap(512, 32);
                for (int i = 0; i < 16; i++)
                {
                    var url = "http://s358455341.websitehome.co.uk/stagecast/art/edge/" + name + "/" + i + ".gif";
                    var tempName = "temp" + i + ".gif";
                    client.DownloadFile(url, tempName);
                    Bitmap temp = new Bitmap(tempName);
                    Graphics g = Graphics.FromImage(target);
                    g.DrawImage(temp, i * tileSize, 0);
                }
                target.Save("tileset.png");
            }
        }

        private static void FloodFillArea(int x, int y, WangArea area)
        {
            int ofs = GetMapOffset(x, y);
            map[ofs].areaID = areaID;

            // flood all adjacent tiles if possible
            FloodFillArea(x - 1, y, areaID, WangDirection.East);
            FloodFillArea(x + 1, y, areaID, WangDirection.West);
            FloodFillArea(x, y - 1, areaID, WangDirection.South);
            FloodFillArea(x, y + 1, areaID, WangDirection.North);
        }

        private static void FloodFillArea(int x, int y, int areaID, WangDirection direction)
        {
            if (x<0 || y<0 || x>=mapWidth || y>=mapHeight) // if out of bounds, return
            {
                return;
            }

            int ofs = GetMapOffset(x, y);
            if (map[ofs].areaID != -1) // if already filled then stop
            {
                return;
            }

            if (map[ofs].tileID == 0) // empty tiles should not be filled
            {
                return;
            }

            if (WangUtils.GetConnectionForTile(map[ofs].tileID, direction) == false)
            {
                return;
            }

            FloodFillArea(x, y, areaID);
        }

        public static void Main(string[] args)
        {
            //DownloadTileset("walkway");

            List<Bitmap> tilesets = new List<Bitmap>();
            // load tilesets
            for (int i=1; i<=9; i++)
            {
                var tileset = new Bitmap("../data/tileset"+i+".png");
                tilesets.Add(tileset);
            }
            
            // first pass clears all tiles
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int ofs = GetMapOffset(i, j);
                    map[ofs].areaID = -1;
                    map[ofs].tileID = -1;
                    map[ofs].x = i;
                    map[ofs].y = j;
                }
            }
            
            // second pass places random tiles in a checkerboard pattern
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    if (i % 2 == j % 2)
                    {
                        continue;
                    }

                    TryPlacingRandomTile(i, j);
                }
            }

            // third pass places random tiles in the mising holes, checking for matching edges
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int current = GetMapAt(i, j).tileID;
                    if (current >= 0) // if tile already set, skip
                    {
                        continue;
                    }

                    //continue;

                    TryPlacingRandomTile(i, j);
                }
            }

            // optional pass, invert bits 
            if (mapInvert)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    for (int i = 0; i < mapWidth; i++)
                    {
                        int current = GetMapAt(i, j).tileID;
                        if (current < 0) // if tile is not set, skip
                        {
                            continue;
                        }

                        SetMapAt(i, j, 15 - current);
                    }
                }
            }

            // detect isolate areas
            List<WangArea> areas = new List<WangArea>(); 
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int ofs = GetMapOffset(i, j);
                    if (map[ofs].areaID != null) // check if this tile already have an area assigned
                    {
                        continue; 
                    }

                    if (map[ofs].tileID == 0) // empty tiles should not be filled
                    {
                        continue;
                    }

                    var area = new WangArea();
                    areas.Add(area);
                    FloodFillArea(i, j, area);
                }
            }

            // after this pass, all tiles will be connected 
            HashSet<WangArea> areaSet = new HashSet<WangArea>();
            while (areaSet.Count<areas.Count)
            {
                var tiles = FindTilesInAreaEdges(k);
                if (tiles.Count<=0)
                {
                    continue;
                }

                var tile = tiles[rnd.Next(tiles.Count)];
                int ofs = GetMapOffset(tile.x, tile.y);
                map[ofs].tileID = WangUtils.AddConnection(map[ofs].tileID, WangDirection.South);
                //map[ofs].tileID = 0;
            }

            // now render the map to a pixel array
            RedrawWithTileset(tilesets[1]);


            int bufferTexID = 0;

            using (var game = new OpenTK.GameWindow(bufferWidth, bufferHeight))
            {
                game.Load += (sender, e) =>
                {
                    // setup settings, load textures, sounds
                    game.VSync = OpenTK.VSyncMode.Off;

                    // generate a texture and copy the pixel array there
                    bufferTexID = GL.GenTexture();
                    UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                };

                game.Resize += (sender, e) =>
                {
                    GL.Viewport(0, 0, game.Width, game.Height);
                };

                game.UpdateFrame += (sender, e) =>
                {
                    if (game.Keyboard[Key.Escape])
                    {
                        game.Exit();
                        Environment.Exit(0);
                    }

                    if (game.Keyboard[Key.Number1])
                    {
                        RedrawWithTileset(tilesets[0]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number2])
                    {
                        RedrawWithTileset(tilesets[1]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number3])
                    {
                        RedrawWithTileset(tilesets[2]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number4])
                    {
                        RedrawWithTileset(tilesets[3]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number5])
                    {
                        RedrawWithTileset(tilesets[4]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number6])
                    {
                        RedrawWithTileset(tilesets[5]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number7])
                    {
                        RedrawWithTileset(tilesets[6]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number8])
                    {
                        RedrawWithTileset(tilesets[7]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                    if (game.Keyboard[Key.Number9])
                    {
                        RedrawWithTileset(tilesets[8]);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                };


                game.RenderFrame += (sender, e) =>
                {
                    // draw the texture to the screen, stretched to fill the whole window
                    DrawBuffer(game, bufferTexID);
                    game.SwapBuffers();
                };

                game.Run();
            }
        }

    }
}
