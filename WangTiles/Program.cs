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
    public class Tileset
    {
        private Dictionary<int, List<Bitmap>> tiles = new Dictionary<int, List<Bitmap>>();
        private int _tileSize;
        public int TileSize { get { return _tileSize; } }

        public Tileset(string fileName)
        {
            var source = new Bitmap(fileName);

            this._tileSize = source.Width / 16;

            for (int i=0; i<16; i++)
            {
                List<Bitmap> variations = new List<Bitmap>();

                int maxVariations = source.Height / _tileSize;
                for (int j=0; j<maxVariations; j++)
                {
                    Bitmap tile = new Bitmap(_tileSize, _tileSize);
                    bool isEmpty = true;
                    for (int y=0; y<_tileSize; y++)
                    {
                        for (int x = 0; x < _tileSize; x++)
                        {
                            var color = source.GetPixel(x + i * _tileSize, y + j * _tileSize);
                            if (color.A<=0)
                            {
                                continue;
                            }

                            isEmpty = false;
                            tile.SetPixel(x, y, color);
                        }
                    }

                    if (isEmpty)
                    {
                        break;
                    }

                    variations.Add(tile);
                }

                tiles[i] = variations;
            }
        }


        /// <summary>
        /// Draws a tile in the texture buffer at specified position
        /// </summary>
        private void DrawTile(byte[] buffer, int bufferWidth, int bufferHeight, int targetX, int targetY, int tileID, int variation, Color borderColor, int drawScale)
        {
            var variations = tiles[tileID];
            Bitmap tile = variations[variation];

            for (int y = 0; y < _tileSize; y++)
            {
                for (int x = 0; x < _tileSize; x++)
                {                   
                    var c = tile.GetPixel(x, y);

                    if (borderColor.A > 0 && (x == 0 || y == 0 || x == _tileSize - 1 || y == _tileSize - 1))
                    {
                        c = borderColor;
                    }

                    for (int iy = 0; iy < drawScale; iy++)
                    {
                        for (int ix = 0; ix < drawScale; ix++)
                        {
                            int tx = targetX + x * drawScale + ix;
                            int ty = targetY + y * drawScale + iy;
                            if (tx >= bufferWidth || tx < 0) { continue; }
                            if (ty >= bufferHeight || ty < 0) { continue; }

                            int destOfs = (tx + bufferWidth * ty) * 4;
                            buffer[destOfs + 0] = c.R;
                            buffer[destOfs + 1] = c.G;
                            buffer[destOfs + 2] = c.B;
                            buffer[destOfs + 3] = c.A;
                        }
                    }
                }
            }
        }

        public void RedrawWithTileset(byte[] buffer, int bufferWidth, int bufferHeight, WangMap map, bool drawBorders, int drawScale)
        {
            for (int j = 0; j < map.Height; j++)
            {
                for (int i = 0; i < map.Width; i++)
                {
                    var tile = map.GetTileAt(i, j);
                    if (tile.tileID < 0) { continue; }

                    int variation = tile.variationID;

                    DrawTile(buffer, bufferWidth, bufferHeight, i * _tileSize * drawScale, j * _tileSize * drawScale, tile.tileID, variation, drawBorders ? WangUtils.GetAreaColor(tile.areaID) : Color.FromArgb(0), drawScale);
                }
            }
        }

    }

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

            GL.Disable(EnableCap.Texture2D);
        }
        #endregion



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
                    g.DrawImage(temp, i * 32, 0);
                }
                target.Save("tileset.png");
            }
        }


        public static void Main(string[] args)
        {
            //DownloadTileset("walkway");

            bool drawBorders = false;
            int drawScale = 4;

            var tilesets = new List<Tileset>();
            // load tilesets
            for (int i=0; i<=9; i++)
            {
                var tileset = new Tileset("../data/tileset"+i+".png");
                tilesets.Add(tileset);
            }


            int exitX = 1;
            int exitY = -1;

            #region WANG_GENERATION
            var map = new WangMap(14, 9, 3424);
            map.AddExit(exitX, exitY);
            map.Generate();
            map.FixConnectivity();
            #endregion

            #region DUNGEON_PLANNING
            LayoutPlanner planner = new LayoutPlanner(4343);
            for (int j=0; j<map.Height; j++)
            {
                for (int i = 0; i < map.Width; i++)
                {
                    var tile = map.GetTileAt(i, j);
                    int tileID = tile.tileID;
                    if (tileID<=0)
                    {
                        continue;
                    }

                    bool north, south, east, west;
                    WangUtils.GetConnectionsForTile(tileID, out north, out east, out south, out west);

                    if (north) { planner.AddConnection(new LayoutCoord(i, j), WangDirection.North); }
                    if (south) { planner.AddConnection(new LayoutCoord(i, j), WangDirection.South); }
                    if (east) { planner.AddConnection(new LayoutCoord(i, j), WangDirection.East); }
                    if (west) { planner.AddConnection(new LayoutCoord(i, j), WangDirection.West); }

                    var room = planner.FindRoomAt(new LayoutCoord(i, j));
                    room.tileID = tileID;
                    room.variationID = tile.variationID;
                }
            }
            planner.entrance = planner.FindRoomAt(new LayoutCoord(exitX, exitY));
            Console.WriteLine("Selected entrance: " + planner.entrance);

            var goal = planner.FindGoal();

            goal = goal.previous;
            while (goal.GetShape() == LayoutRoom.RoomShape.Corridor)
            {
                goal = goal.previous;
            }

            planner.SetGoal(goal);
            Console.WriteLine("Selected goal: " + goal);

            List<LayoutKey> keys = new List<LayoutKey>();
            keys.Add(new LayoutKey("Copper", 0));
            keys.Add(new LayoutKey("Bronze", 1));
            keys.Add(new LayoutKey("Silver", 2));
            keys.Add(new LayoutKey("Gold", 3));

            planner.GenerateProgression();
            planner.GenerateRoomTypes();
            planner.GenerateLocks(keys);
            #endregion


            // now render the map to a pixel array
            int currentTileset = 0;
            tilesets[currentTileset].RedrawWithTileset(buffer, bufferWidth, bufferHeight, map, drawBorders, drawScale);


            int bufferTexID = 0;

            int selX = -1;
            int selY = -1;

            using (var game = new OpenTK.GameWindow(bufferWidth, bufferHeight, OpenTK.Graphics.GraphicsMode.Default, "Wang Tiles"))
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


                game.Mouse.Move += (object sender, MouseMoveEventArgs mouseEvent) =>
                {
                    var mousePos = mouseEvent.Position;
                    //mousePos = game.PointToClient(mousePos);

                    //Console.WriteLine(mousePos.X + "    " + mousePos.Y);

                    selX = (mousePos.X) / (drawScale * tilesets[currentTileset].TileSize);
                    selY = (mousePos.Y) / (drawScale * tilesets[currentTileset].TileSize);

                    if (selX >= map.Width) { selX = -1; }
                    if (selY >= map.Height) { selY = -1; }
                };

                game.Mouse.ButtonDown += (object sender, MouseButtonEventArgs buttonEvent) => {
                };

                game.UpdateFrame += (sender, e) =>
                {
                    if (game.Keyboard[Key.Escape])
                    {
                        game.Exit();
                        Environment.Exit(0);
                    }

                    int oldTileset = currentTileset;
                    if (game.Keyboard[Key.Number1])
                    {
                        currentTileset = 0;
                    }
                    if (game.Keyboard[Key.Number2])
                    {
                        currentTileset = 1;
                    }
                    if (game.Keyboard[Key.Number3])
                    {
                        currentTileset = 2;
                    }
                    if (game.Keyboard[Key.Number4])
                    {
                        currentTileset = 3;
                    }
                    if (game.Keyboard[Key.Number5])
                    {
                        currentTileset = 4;
                    }
                    if (game.Keyboard[Key.Number6])
                    {
                        currentTileset = 5;
                    }

                    if (game.Keyboard[Key.Number7])
                    {
                        currentTileset = 6;
                    }

                    if (game.Keyboard[Key.Number8])
                    {
                        currentTileset = 7;
                    }

                    if (game.Keyboard[Key.Number9])
                    {
                        currentTileset = 8;
                    }

                    if (oldTileset != currentTileset)
                    {
                        tilesets[currentTileset].RedrawWithTileset(buffer, bufferWidth, bufferHeight, map, drawBorders, drawScale);
                        UpdateBuffer(buffer, bufferWidth, bufferHeight, bufferTexID);
                    }
                };


                game.RenderFrame += (sender, e) =>
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    GL.Ortho(0.0, 1.0, 0.0, 1.0, 0.0, 4.0);


                    // draw the texture to the screen, stretched to fill the whole window
                    DrawBuffer(game, bufferTexID);


                    if (selX>=0 && selY >=0)
                    {
                        var selRoom = planner.FindRoomAt(new LayoutCoord(selX, selY), false);
                        if (selRoom!=null)
                        {
                            int percent = ((int)(selRoom.intensity * 100));
                            string s = selRoom.order + "# " + selRoom.ToString();
                            string s2 = selRoom.GetShape() + "/" + selRoom.category + "("+selRoom.distanceFromMainPath+") " + percent + "%";

                            if (selRoom.contains!=null)
                            {
                                s += "(Contains " + selRoom.contains+")";
                            }

                            if (selRoom.locked != null)
                            {
                                s += "(Requires " + selRoom.locked + ")";
                            }

                            if (selRoom.isLoop)
                            {
                                s += "(Loop)";
                            }

                            FontUtils.DrawText(game, s, 4, 20, 0.6f, Color.White);
                            FontUtils.DrawText(game, s2, 4, 0, 0.6f, Color.White);
                        }
                    }

                    game.SwapBuffers();
                };

                game.Run();
            }
        }

    }
}
