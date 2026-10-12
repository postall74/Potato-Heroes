#!/usr/bin/env python3
"""Проверка раскладки спрайт-шитов Potato Heroes: режем кадры по Layout из AssetBank.cs,
собираем превью и печатаем метрики. Скрипт зеркалирует логику AssetBank.LayoutFor."""
from PIL import Image
import numpy as np

PPU = 48.0

def layout_for(name):
    if name.startswith("char_"): return (128, 128, 4)
    if "boss" in name: return (160, 160, 4)
    if "ghost" in name: return (96, 112, 4)
    if name.startswith("mob_"): return (96, 96, 4)
    if name == "vfx_spark": return (48, 48, 4)
    if name == "swing": return (128, 128, 4)
    if name == "bullets": return (48, 32, 4)
    if name == "xp_gem": return (32, 32, 3)
    if name == "weapons_icons": return (64, 64, 6)
    if name == "chest": return (64, 64, 1)
    if name in ("floor_tile", "wall_tile"): return (128, 128, 1)
    return (64, 64, 1)

names = ["char_captain_tuber","char_engineer_peel","char_medic_sprout","char_mage_truffle",
         "mob_slime_green","mob_slime_red","mob_slime_blue","mob_bat","mob_ghost","mob_boss_king_gelatin",
         "vfx_spark","swing","bullets","xp_gem","weapons_icons","chest","floor_tile","wall_tile"]

ok = True
for n in names:
    im = Image.open(f"Assets/Sprites/{n}.png").convert("RGBA")
    W, H = im.size
    fw, fh, count = layout_for(n)
    a = np.array(im)
    print(f"{n}: sheet {W}x{H}, frame {fw}x{fh} x{count}", end="  ")
    for i in range(count):
        sub = a[0:fh, i*fw:(i+1)*fw]
        alpha = sub[:, :, 3]
        ys, xs = np.where(alpha > 10)
        if len(ys) == 0:
            print(f"[frame {i} EMPTY!]"); ok = False; continue
        # высота содержимого в юнитах мира
    print(f"content rows y {ys.min()}-{ys.max()} ({(ys.max()-ys.min()+1)/PPU:.2f}m)")

print("\nВЫВОД:", "OK — все кадры режутся с содержимым" if ok else "ОШИБКА РАСКЛАДКИ")

# Превью: первые кадры мобов+персонажа на тайле пола
tile = Image.open("Assets/Sprites/floor_tile.png").convert("RGBA").resize((512,512))
canvas = tile.copy()
x = 20
for n in ["mob_slime_green","mob_slime_red","mob_bat","mob_ghost","mob_boss_king_gelatin","char_captain_tuber"]:
    im = Image.open(f"Assets/Sprites/{n}.png").convert("RGBA")
    fw, fh, _ = layout_for(n)
    fr = im.crop((0,0,fw,fh)).resize((int(fw/PPU*120), int(fh/PPU*120)), Image.NEAREST)
    canvas.alpha_composite(fr, (x, 380-fr.height))
    x += fr.width + 12
canvas.save("/tmp/sprites_preview.png")
print("Превью сохранено: /tmp/sprites_preview.png")
