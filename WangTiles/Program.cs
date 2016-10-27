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
        private int tileSize;

        public Tileset(string fileName)
        {
            var source = new Bitmap(fileName);

            this.tileSize = source.Width / 16;

            for (int i=0; i<16; i++)
            {
                List<Bitmap> variations = new List<Bitmap>();

                int maxVariations = source.Height / tileSize;
                for (int j=0; j<maxVariations; j++)
                {
                    Bitmap tile = new Bitmap(tileSize, tileSize);
                    bool isEmpty = true;
                    for (int y=0; y<tileSize; y++)
                    {
                        for (int x = 0; x < tileSize; x++)
                        {
                            var color = source.GetPixel(x + i * tileSize, y + j * tileSize);
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
        private void DrawTile(byte[] buffer, int bufferWidth, int bufferHeight, int targetX, int targetY, int tileID, int variation, Color borderColor)
        {
            var variations = tiles[tileID];

            variation = variation % variations.Count;
            Bitmap tile = variations[variation];

            for (int y = 0; y < tileSize; y++)
            {
                for (int x = 0; x < tileSize; x++)
                {
                    if (x + targetX >= bufferWidth || x + targetX < 0) { continue; }
                    if (y + targetY >= bufferHeight || y + targetY < 0) { continue; }
                    int destOfs = ((x + targetX) + bufferWidth * (y + targetY)) * 4;
                    var c = tile.GetPixel(x, y);

                    if (borderColor.A > 0 && (x == 0 || y == 0 || x == tileSize - 1 || y == tileSize - 1))
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

        public void RedrawWithTileset(byte[] buffer, int bufferWidth, int bufferHeight, WangMap map)
        {
            for (int j = 0; j < map.Height; j++)
            {
                for (int i = 0; i < map.Width; i++)
                {
                    var tile = map.GetTileAt(i, j);
                    if (tile.tileID < 0) { continue; }
                    DrawTile(buffer, bufferWidth, bufferHeight, i * tileSize, j * tileSize, tile.tileID, i + i * j, WangUtils.GetAreaColor(tile.areaID));
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

            var tilesets = new List<Tileset>();
            // load tilesets
            for (int i=0; i<=9; i++)
            {
                var tileset = new Tileset("../data/tileset"+i+".png");
                tilesets.Add(tileset);
            }


            var map = new WangMap(16, 10, 3424);

            map.AddExit(1, -1);

            map.Generate();

            map.FixConnectivity();

            // now render the map to a pixel array
            int currentTileset = 0;
            tilesets[currentTileset].RedrawWithTileset(buffer, bufferWidth, bufferHeight, map);


            int bufferTexID = 0;

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
                        tilesets[currentTileset].RedrawWithTileset(buffer, bufferWidth, bufferHeight, map);
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
