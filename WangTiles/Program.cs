using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using Lunar.Utils;

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
        private static Bitmap tileset;
        private const int tileSize = 32;

        /// <summary>
        /// Draws a tile in the texture buffer at specified position
        /// </summary>
        private static void DrawTile(int targetX, int targetY, int tileID)
        {
            for (int y = 0; y < tileSize; y++)
            {
                for (int x = 0; x < tileSize; x++)
                {
                    if (x+targetX>=bufferWidth || x+targetX<0) { continue; }
                    if (y + targetY >= bufferHeight || y+targetY<0) { continue; }
                    int destOfs = ((x + targetX) + bufferWidth * (y + targetY)) * 4;
                    var c = tileset.GetPixel((tileID * tileSize) + x, y);
                    buffer[destOfs + 0] = c.R;
                    buffer[destOfs + 1] = c.G;
                    buffer[destOfs + 2] = c.B;
                    buffer[destOfs + 3] = c.A;
                }
            }

        }
        #endregion

        #region MAP
        private static int mapWidth = 16;
        private static int mapHeight = 10;
        private static bool mapWrapX = false;
        private static bool mapWrapY = false;
        private static int[] map = new int[mapWidth * mapHeight];

        private static int GetMapAt(int x, int y)
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
                return 0;
            }
            return map[x + y * mapWidth];
        }

        private static void SetMapAt(int x, int y, int val)
        {
            map[x + y * mapWidth] = val;
        }

        private static void TryPlacingRandomTile(int i, int j)
        {
            int left = GetMapAt(i - 1, j);
            int right = GetMapAt(i + 1, j);
            int up = GetMapAt(i, j - 1);
            int down = GetMapAt(i, j + 1);

            int count = 0;
            do
            {
                int n = rnd.Next(16);

                count++;
                if (count > 5000)
                {
                    break;
                }

                if (right == 12)
                {
                    right += 0;
                }

                if (left != -1 && !WangUtils.MatchTile(left, n, WangDirection.East)) { continue; }
                if (right != -1 && !WangUtils.MatchTile(right, n, WangDirection.West)) { continue; }
                if (up != -1 && !WangUtils.MatchTile(up, n, WangDirection.South)) { continue; }
                if (down != -1 && !WangUtils.MatchTile(down, n, WangDirection.North)) { continue; }

                SetMapAt(i, j, n);
                break;

            } while (true);
        }

        #endregion

        private static Random rnd = new Random(3424);

        public static void Main(string[] args)
        {
            // load a tileset
            tileset = new Bitmap("../data/tileset.png");

            // first pass clears all tiles
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    SetMapAt(i, j, -1);
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
                    int current = GetMapAt(i, j);
                    if (current >= 0) // if tile already set, skip
                    {
                        continue;
                    }

                    //continue;

                    TryPlacingRandomTile(i, j);
                }
            }

            // now render the map to a pixel array
            for (int j=0; j<mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    int tileID = map[i + j * mapWidth];
                    if (tileID < 0) { continue; }
                    DrawTile(i * tileSize, j * tileSize, tileID);
                }
            }


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
