using System.Collections.Generic;
using System.Drawing;
using WangTiles.Core;

namespace WangTiles.Utils
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

            for (int i = 0; i < 16; i++)
            {
                List<Bitmap> variations = new List<Bitmap>();

                int maxVariations = source.Height / _tileSize;
                for (int j = 0; j < maxVariations; j++)
                {
                    Bitmap tile = new Bitmap(_tileSize, _tileSize);
                    bool isEmpty = true;
                    for (int y = 0; y < _tileSize; y++)
                    {
                        for (int x = 0; x < _tileSize; x++)
                        {
                            var color = source.GetPixel(x + i * _tileSize, y + j * _tileSize);
                            if (color.A <= 0)
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
            Bitmap tile = variations[variation % variations.Count];

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

                    DrawTile(buffer, bufferWidth, bufferHeight, i * _tileSize * drawScale, j * _tileSize * drawScale, tile.tileID, variation, drawBorders ? WangArea.GetColor(tile.areaID) : Color.FromArgb(0), drawScale);
                }
            }
        }

    }
}
