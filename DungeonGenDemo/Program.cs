using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Net;
using WangTiles.Utils;
using WangTiles.DungeonPlanner;
using WangTiles.Core;

namespace DungeonDemo
{
    public class DungeonExample
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

        public static void Main(string[] args)
        {
            //DownloadTileset("walkway");

            bool drawBorders = false;
            int drawScale = 4;

            var tileset = new Tileset("../data/tileset0.png");

            int exitX = 1;
            int exitY = -1;

            #region WANG_GENERATION
            var map = new WangEdgeMap(14, 9, 3424);
            map.AddExit(exitX, exitY);
            map.Generate();
            map.FixConnectivity();
            #endregion

            #region DUNGEON_PLANNING
            LayoutPlanner planner = new LayoutPlanner(4343);
            for (int j = 0; j < map.Height; j++)
            {
                for (int i = 0; i < map.Width; i++)
                {
                    var tile = map.GetTileAt(i, j);
                    int tileID = tile.tileID;
                    if (tileID <= 0)
                    {
                        continue;
                    }

                    bool north, south, east, west;
                    WangEdgeUtils.GetConnectionsForTile(tileID, out north, out east, out south, out west);

                    if (north) { planner.AddConnection(new LayoutCoord(i, j), WangEdgeDirection.North); }
                    if (south) { planner.AddConnection(new LayoutCoord(i, j), WangEdgeDirection.South); }
                    if (east) { planner.AddConnection(new LayoutCoord(i, j), WangEdgeDirection.East); }
                    if (west) { planner.AddConnection(new LayoutCoord(i, j), WangEdgeDirection.West); }

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
            tileset.RedrawWithTileset(buffer, bufferWidth, bufferHeight, map, drawBorders, drawScale);


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

                    selX = (mousePos.X) / (drawScale * tileset.TileSize);
                    selY = (mousePos.Y) / (drawScale * tileset.TileSize);

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

                };


                game.RenderFrame += (sender, e) =>
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.MatrixMode(MatrixMode.Projection);
                    GL.LoadIdentity();
                    GL.Ortho(0.0, 1.0, 0.0, 1.0, 0.0, 4.0);


                    // draw the texture to the screen, stretched to fill the whole window
                    DrawBuffer(game, bufferTexID);


                    if (selX >= 0 && selY >= 0)
                    {
                        var selRoom = planner.FindRoomAt(new LayoutCoord(selX, selY), false);
                        if (selRoom != null)
                        {
                            int percent = ((int)(selRoom.intensity * 100));
                            string s = selRoom.order + "# " + selRoom.ToString();
                            string s2 = selRoom.GetShape() + "/" + selRoom.category + "(" + selRoom.distanceFromMainPath + ") " + percent + "%";

                            if (selRoom.contains != null)
                            {
                                s += "(Contains " + selRoom.contains + ")";
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
