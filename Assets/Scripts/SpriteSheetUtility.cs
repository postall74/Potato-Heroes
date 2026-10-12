using UnityEngine;

namespace PotatoHeroes
{
    // Вырезание кадра из горизонтального спрайт-шита (в PPU-единицах)
    public static class SpriteSheetUtility
    {
        public static Sprite CutFrame(Texture2D sheet, int frame, int frameW, int frameH, float ppu, Vector2 pivot)
        {
            var rect = new Rect(frame * frameW, 0, frameW, frameH);
            var pix = new Vector2(frameW, frameH);
            var spr = Sprite.Create(sheet, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            spr.name = $"{sheet.name}_{frame}";
            return spr;
        }
    }
}
