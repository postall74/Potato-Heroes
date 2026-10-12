#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Generator sprite-sheetov dlya igry "Potato Heroes" (Brotato-like).
Zapusk:  python3 generate_sprites.py
Rezultat: PNG faily ryadom s skriptom.
"""
from PIL import Image, ImageDraw
import os

OUT = os.path.dirname(os.path.abspath(__file__))
S = 4  # pixel scale

def new_canvas(w, h):
    return Image.new("RGBA", (w*S, h*S), (0,0,0,0))

def px(img, x, y, c):
    d = ImageDraw.Draw(img)
    d.rectangle([x*S, y*S, (x+1)*S-1, (y+1)*S-1], fill=c)

def rect(img, x, y, w, h, c):
    d = ImageDraw.Draw(img)
    d.rectangle([x*S, y*S, (x+w)*S-1, (y+h)*S-1], fill=c)

def shade(hexcolor, f):
    h = hexcolor.lstrip('#')
    r,g,b = int(h[0:2],16), int(h[2:4],16), int(h[4:6],16)
    return (int(r*f), int(g*f), int(b*f), 255)

# ---------------- Character (32x32, 4 kadrakh walk: nogi/dvizhenie) ----------------
def character(base='#e8b84a', accent='#c0392b', name='char'):
    W,H,FR = 32,32,4
    sheet = new_canvas(W*FR, H)
    body_d = shade(accent,0.8); body_l = shade(accent,1.2)
    skin = base; skin_d = shade(base,0.82)
    for f in range(FR):
        ox = f*W
        bob = [0,0,-1,0][f]          # pryg
        legA = [0,1,0,-1][f]         # nogi vrazhdru
        def P(x,y,c): px(sheet, ox+x, y+bob if y>10 else y, c)
        # ten'
        for x in range(10,22): P(x,31,(0,0,0,60))
        # nogi
        for i in range(4):
            P(13-legA+i, 27+bob, skin_d); P(13-legA+i, 28+bob, skin_d)
            P(18+legA+i, 27+bob, skin_d); P(18+legA+i, 28+bob, skin_d)
            P(13-legA+i, 29+bob, (40,40,50,255)); P(18+legA+i,29+bob,(40,40,50,255))
        # telo (kruglaya kartoshka)
        for y in range(12,28):
            w = int(max(0,6.5**2-((y-20)**2))**0.5)+7 if abs(y-20)<7 else 6
            for x in range(16-w,16+w): P(x,y,skin)
        # obvodka/blik
        for y in range(13,27):
            w = int(max(0,6.5**2-((y-20)**2))**0.5)+7
            P(16-w,y,skin_d); P(15+w,y,shade(base,0.9))
        for x in range(12,18): P(x,13,shade(base,1.25))
        # odezhda (shalik)
        for y in range(20,25):
            w = int(max(0,4**2-((y-22)**2))**0.5)+7 if abs(y-22)<4 else 6
            for x in range(16-w,16+w): P(x,y,accent)
        # glaza
        blink = (f==3)
        if not blink:
            P(13,17,(255,255,255,255)); P(14,17,(255,255,255,255)); P(18,17,(255,255,255,255)); P(19,17,(255,255,255,255))
            P(14,17,(20,20,30,255)); P(18,17,(20,20,30,255))
        else:
            P(13,17,(20,20,30,255));P(14,17,(20,20,30,255));P(18,17,(20,20,30,255));P(19,17,(20,20,30,255))
        # rot
        P(15,20,(90,40,30,255)); P(16,20,(90,40,30,255)); P(17,20,(90,40,30,255))
        # shcheki
        P(12,19,shade('#ff8888',1)); P(20,19,shade('#ff8888',1))
        # ruki + oruzhie (dlinnaya lopatka-shtyr')
        arm_y = 21+bob
        for x in range(22,26): P(x,arm_y,skin)
        for x in range(25,31): P(x,arm_y-2,body_d)   # stvol/stvka
        rect_px = lambda x,y,c: P(x,y,c)
        P(30,arm_y-3,body_l); P(31,arm_y-3,body_l)   # nakonechnik
        for x in range(22,26): P(x,arm_y+1,skin_d)
    sheet.save(os.path.join(OUT, f'char_{name}.png'))

# ---------------- Slime (24x24, 4 kadra idle-pulse) ----------------
def slime(c1='#4caf50', name='green'):
    W,H,FR=24,24,4
    sheet=new_canvas(W*FR,H)
    dark=shade(c1,0.7); lite=shade(c1,1.3)
    for f in range(FR):
        ox=f*W
        sq=[0,1,2,1][f]      # splyos
        hgt=14-sq
        for y in range(24-hgt,23):
            t=(y-(24-hgt))/max(hgt,1)
            w=int(9*(1-t*t*0.35))
            for x in range(12-w,12+w): px(sheet,ox+x,y,c1)
            px(sheet,ox+12-w,y,dark); px(sheet,ox+11+w,y,dark)
        for x in range(12-6,12+6): px(sheet,ox+x,24-hgt,lite)
        # glaza
        ey=24-hgt+5
        px(sheet,ox+8,ey,(255,255,255,255));px(sheet,ox+9,ey,(255,255,255,255))
        px(sheet,ox+14,ey,(255,255,255,255));px(sheet,ox+15,ey,(255,255,255,255))
        px(sheet,ox+9,ey+1,(20,20,30,255));px(sheet,ox+15,ey+1,(20,20,30,255))
        px(sheet,ox+11,ey+4,(30,30,40,255));px(sheet,ox+12,ey+4,(30,30,40,255))
        px(sheet,ox+8,23,(0,0,0,60));px(sheet,ox+9,23,(0,0,0,60))
    sheet.save(os.path.join(OUT,f'mob_slime_{name}.png'))

# ---------------- Bat (24x24, 4 kadra mahrovye kryl'ya) ----------------
def bat(name='bat'):
    W,H,FR=24,24,4
    sheet=new_canvas(W*FR,H)
    c1='#6b5b95'; dark=shade(c1,0.7); lite=shade(c1,1.3)
    wingpos=[[10,6],[12,9],[10,12],[12,9]]  # razmach
    for f in range(FR):
        ox=f*W
        wy=wingpos[f][1]
        # kryl'ya
        for i,wg in enumerate([-1,1]):
            cx=12+wg*(4+i*0)
            for x in range(2,10):
                yy=wy+abs(6-x)//2
                px(sheet,ox+12+wg*x,yy,c1); px(sheet,ox+12+wg*x,yy+1,dark)
        # telo
        for y in range(10,18):
            w=4-abs(y-13)
            for x in range(12-w,12+w): px(sheet,ox+x,y,c1)
        for x in range(10,14): px(sheet,ox+x,9,c1); px(sheet,ox+x,10,lite)
        # ushi
        px(sheet,ox+9,7,c1);px(sheet,ox+9,8,c1);px(sheet,ox+14,7,c1);px(sheet,ox+14,8,c1)
        # glaza
        px(sheet,ox+10,11,(255,80,80,255));px(sheet,ox+13,11,(255,80,80,255))
        # klyki
        px(sheet,ox+11,14,(255,255,255,255));px(sheet,ox+12,14,(255,255,255,255))
    sheet.save(os.path.join(OUT,f'mob_{name}.png'))

# ---------------- Ghost (24x28, 4 kadra plyushchie) ----------------
def ghost(name='ghost'):
    W,H,FR=24,28,4
    sheet=new_canvas(W*FR,H)
    c1=(190,210,235,200); dark=(150,170,200,200); 
    for f in range(FR):
        ox=f*W; fl=[0,-1,-2,-1][f]
        for y in range(4+fl,20+fl):
            t=(y-(4+fl))/16
            w=int(9 if t<0.7 else 8)
            if t>0.8: w=9
            for x in range(12-w,12+w): px(sheet,ox+x,y,c1)
        for x in range(5,19): px(sheet,ox+x,4+fl,c1)
        # zubchatyj niz
        for i,x in enumerate(range(4,20,4)):
            off=[0,2,0,2][ (i+f)%4 ]
            for y in range(20+fl,23+fl-off): px(sheet,ox+x+(y-(20+fl))//1,y,c1)
            px(sheet,ox+x+1,21+fl,c1)
        # glaza
        px(sheet,ox+8,10+fl,(40,50,80,255));px(sheet,ox+9,10+fl,(40,50,80,255))
        px(sheet,ox+14,10+fl,(40,50,80,255));px(sheet,ox+15,10+fl,(40,50,80,255))
        px(sheet,ox+11,14+fl,(60,70,100,180));px(sheet,ox+12,15+fl,(60,70,100,180))
    sheet.save(os.path.join(OUT,f'mob_{name}.png'))

# ---------------- BigSlime/Boss (40x40, 4 kadra) ----------------
def boss(name='king_gelatin'):
    W,H,FR=40,40,4
    sheet=new_canvas(W*FR,H)
    c1='#8e44ad'; dark=shade(c1,0.65); lite=shade(c1,1.3)
    for f in range(FR):
        ox=f*W; sq=[0,1,2,1][f]; hgt=26-sq
        for y in range(40-hgt,38):
            t=(y-(40-hgt))/hgt
            w=int(15*(1-t*t*0.3))
            for x in range(20-w,20+w): px(sheet,ox+x,y,c1)
            px(sheet,ox+20-w,y,dark);px(sheet,ox+19+w,y,dark)
        for x in range(20-9,20+9): px(sheet,ox+x,40-hgt,lite)
        # koruna
        crow=40-hgt-1
        for x in range(12,28): px(sheet,ox+x,crow,'#f1c40f')
        for i,x in enumerate([12,16,20,24,28]):
            for dy in range(1,4+(i%2)): px(sheet,ox+x,crow-dy,'#f1c40f')
            px(sheet,ox+x,crow-4-(i%2),'#e74c3c')
        # glaza
        ey=40-hgt+9
        for ex in (14,24):
            for dx in range(3):
                for dy in range(2): px(sheet,ox+ex+dx,ey+dy,(255,255,255,255))
            px(sheet,ox+ex+1,ey+1,(30,10,40,255))
        # рот с зубами
        my=40-hgt+17
        for x in range(15,25): px(sheet,ox+x,my,(40,10,40,255))
        for x in range(16,24,2): px(sheet,ox+x,my+1,(255,255,255,255))
    sheet.save(os.path.join(OUT,f'mob_boss_{name}.png'))

# ---------------- Bullets & items (8x8 each on strip) ----------------
def bullets():
    W=8; items=[]
    img=new_canvas(W*6,W)
    cols=['#ffd54f','#e0e0e0','#ff7043','#4fc3f7','#ba68c8','#aed581']
    for i,c in enumerate(cols):
        h=c.lstrip('#'); col=(int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),255)
        ox=i*W
        rect(img,ox+3,1,2,6,col)
        rect(img,ox+2,2,1,4,shade(c,0.7)); rect(img,ox+5,2,1,4,shade(c,0.7))
        px(img,ox+3,0,shade(c,1.4)); px(img,ox+4,0,shade(c,1.4))
    img.save(os.path.join(OUT,'bullets.png'))
    # melee swing arc 32x32 x4
    img=new_canvas(32*4,32)
    for f in range(4):
        ox=f*32
        a0=-60+f*30
        import math
        for adeg in range(a0,a0+55,6):
            a=math.radians(adeg)
            for r in range(8,15):
                x=int(16+math.cos(a)*r); y=int(16+math.sin(a)*r)
                c=(255,255,255,max(40,255-r*12))
                px(img,ox+x,y,c)
    img.save(os.path.join(OUT,'swing.png'))
    # xp gem 8x8 x3 sizes
    img=new_canvas(24,8)
    for i,(w,c) in enumerate([(2,'#4fc3f7'),(3,'#ffd54f'),(4,'#ef5350')]):
        ox=i*8; h=c.lstrip('#'); col=(int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),255)
        for y in range(8):
            ww=w-abs(4-y)*w//4
            for x in range(4-ww,4+ww): px(img,ox+x,y,col)
        px(img,ox+3,2,shade(c,1.5))
    img.save(os.path.join(OUT,'xp_gem.png'))
    # chest 16x16
    img=new_canvas(16,16)
    rect(img,2,4,12,10,'#8d6e63'); rect(img,2,4,12,3,'#a1887f')
    rect(img,2,8,12,1,'#f1c40f'); rect(img,7,7,2,4,'#f1c40f')
    rect(img,1,3,14,1,'#6d4c41')
    img.save(os.path.join(OUT,'chest.png'))

# ---------------- Floor tile 32x32 ----------------
def floor_tile():
    import random
    random.seed(7)
    img=new_canvas(32,32)
    base=(34,38,44,255)
    rect(img,0,0,32,32,base)
    for _ in range(120):
        x=random.randrange(32); y=random.randrange(32)
        c=random.choice([(30,34,40,255),(40,45,52,255),(45,50,58,255)])
        px(img,x,y,c)
    # travinka/kamek
    for _ in range(8):
        x=random.randrange(2,30); y=random.randrange(2,30)
        px(img,x,y,(56,74,58,255)); px(img,x,y-1,(66,88,66,255))
    # granicy plitki
    for i in range(32): px(img,i,0,(24,27,32,255)); px(img,0,i,(24,27,32,255))
    img.save(os.path.join(OUT,'floor_tile.png'))

# ---------------- Weapon icons 16x16 strip (6 ikonok) ----------------
def weapons_icon_strip():
    img=new_canvas(16*6,16)
    # 0: pistol
    ox=0
    rect(img,ox+2,7,9,3,(70,75,90)); rect(img,ox+9,4,4,3,(90,95,110)); rect(img,ox+4,10,3,4,(60,45,35))
    # 1: SMG
    ox=16; rect(img,ox+1,7,12,3,(110,115,130)); rect(img,ox+4,10,2,4,(70,75,90)); rect(img,ox+9,3,2,4,(110,115,130))
    # 2: shotgun
    ox=32; rect(img,ox+1,6,13,2,(120,80,60)); rect(img,ox+1,9,13,2,(120,80,60)); rect(img,ox+2,11,3,3,(80,55,40))
    # 3: rifle
    ox=48; rect(img,ox+1,7,14,2,(80,110,80)); rect(img,ox+5,9,3,4,(60,80,60)); rect(img,ox+12,5,3,2,(80,110,80))
    # 4: knife
    ox=64; rect(img,ox+4,3,2,8,(200,205,215)); rect(img,ox+3,11,4,3,(90,60,40)); px(img,ox+5,2,(230,235,245))
    # 5: wand
    ox=80; rect(img,ox+3,11,2,2,(120,80,50)); rect(img,ox+5,9,2,2,(120,80,50)); rect(img,ox+7,7,2,2,(120,80,50))
    rect(img,ox+9,4,4,4,(155,89,182)); px(img,ox+10,3,(200,140,230)); px(img,ox+13,6,(200,140,230))
    img.save(os.path.join(OUT,'weapons_icons.png'))

# ---------------- Arena walls 32x32 corner/tile ----------------
def walls():
    img=new_canvas(32,32)
    rect(img,0,0,32,32,(55,60,72))
    for y in range(0,32,8):
        rect(img,0,y,32,1,(40,44,54))
        for x in range(0,32,8): rect(img,x,y,1,8,(40,44,54))
    for _ in range(40):
        import random; random.seed(y if 'y' in dir() else 1)
    img.save(os.path.join(OUT,'wall_tile.png'))

# ---------------- VFX hit spark 12x12 x4 ----------------
def vfx():
    img=new_canvas(12*4,12)
    for f in range(4):
        ox=f*12; r=2+f*2; c=(255,230,120,max(60,255-f*60))
        import math
        for a in range(0,360,45):
            ar=math.radians(a)
            x=int(6+math.cos(ar)*r); y=int(6+math.sin(ar)*r)
            px(img,ox+x,y,c)
        px(img,ox+6,6,(255,255,255,min(255,300-f*70)))
    img.save(os.path.join(OUT,'vfx_spark.png'))

if __name__=='__main__':
    character('#e8b84a','#c0392b','captain_tuber')      # kapitan Kartoshka - krasnyj shalik
    character('#d9a441','#2980b9','engineer_peel')      # Inzhener-Ocherka - sinij
    character('#f0c987','#27ae60','medic_sprout')       # Medik-Rostok - zelenyj
    character('#c98f3b','#8e44ad','mage_truffle')       # Mag-Trjul'-fioletovyj
    slime('#4caf50','green'); slime('#ef5350','red'); slime('#42a5f5','blue')
    bat(); ghost(); boss()
    bullets(); floor_tile(); weapons_icon_strip(); walls(); vfx()
    print('OK:', sorted(os.listdir(OUT)))
