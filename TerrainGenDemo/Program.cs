using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;
using System.Net;
using WangTiles.Utils;
using WangTiles.Core;

namespace WangTiles
{
    public class LayoutGenExample
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
                    var url = "http://cr31.co.uk/stagecast/art/corn/" + name + "/" + i + ".gif";
                    var tempName = "temp" + i + ".gif";
                    client.DownloadFile(url, tempName);
                    Bitmap temp = new Bitmap(tempName);
                    Graphics g = Graphics.FromImage(target);
                    g.DrawImage(temp, i * 32, 0);
                }
                target.Save(@"..\data\"+name+".png");
            }
        }


        public static void Main(string[] args)
        {
            //DownloadTileset("glob");

            bool drawBorders = false;
            int drawScale = 1;

            var tileset = new Tileset("../data/seasand.png");

            #region WANG_GENERATION
            var map = new WangCornerMap(18, 12, 1424);

            // not everything needs to be random, we can pre-fill parts of the map with our own data
            for (int j = 2; j <= 6; j++)
            {
                for (int i = 2; i <= 6; i++)
                {
                    map.SetTileIDAt(i, j, 0);
                }
            }

            for (int j = 8; j <= 11; j++)
            {
                for (int i = 8; i <= 14; i++)
                {
                    map.SetTileIDAt(i, j, 15);
                }
            }

            // now fill the missing tiles randomly
            map.Generate();
            map.Invert();
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


                    game.SwapBuffers();
                };

                game.Run();
            }
        }

    }
}
