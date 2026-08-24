#!/usr/bin/env python3
"""Генератор спрайтов «Рубилово» по docs/spec/05-visual-bible.md.
Запуск: python3 tools/gen_art.py
Выход: native/Rubilovo.Android/Resources/drawable/*.png + docs/img/art-sheet.png
"""
import math
import os
import random

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "native", "Rubilovo.Android", "Resources", "drawable")
SHEET = os.path.join(os.path.dirname(__file__), "..", "docs", "img", "art-sheet.png")
os.makedirs(OUT, exist_ok=True)

def hx(s): return tuple(int(s[i:i+2], 16) for i in (1, 3, 5))

def darken(rgb, f=0.45): return tuple(int(c * f) for c in rgb)
def lighten(rgb, f=0.25): return tuple(min(255, int(c + (255 - c) * f)) for c in rgb)

PAL = {
    "player": hx("#fff5eb"), "gold": hx("#ffd166"), "red": hx("#ef476f"),
    "walker": hx("#6a994e"), "runner": hx("#ffb703"), "tank": hx("#e76f51"),
    "shooter": hx("#cdb4db"), "sprinter": hx("#90be6d"), "kamikaze": hx("#f94144"),
    "butcher": hx("#d90a99"), "foundry": hx("#118ab2"), "executioner": hx("#e5383b"),
    "overlord": hx("#9d4edd"), "heal": hx("#06d6a0"), "bone": hx("#e0d7ee"),
    "steel": hx("#dfe7ef"), "wood": hx("#9c6644"), "leather": hx("#6b4f2a"),
}

def canvas(s): return Image.new("RGBA", (s, s), (0, 0, 0, 0))

def ell(d, cx, cy, rx, ry, fill, ol=None, w=2):
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=fill,
              outline=ol or darken(fill), width=w)

def rrect(d, box, fill, ol=None, w=2, r=4):
    d.rounded_rectangle(box, radius=r, fill=fill, outline=ol or darken(fill), width=w)

def poly(d, pts, fill, ol=None, w=2):
    d.polygon(pts, fill=fill, outline=ol or darken(fill), width=w)

def eyes(d, cx, cy, dx, r, col=(20, 20, 30, 255)):
    for sx in (-1, 1):
        d.ellipse([cx + sx * dx - r, cy - r, cx + sx * dx + r, cy + r], fill=col)

# ---------- characters (top-down, face UP, rotate at runtime) ----------

def gen_player():
    im = canvas(64); d = ImageDraw.Draw(im)
    body = PAL["player"]; ol = darken(body, 0.35)
    ell(d, 32, 36, 17, 15, body, ol)                      # корпус
    ell(d, 32, 24, 11, 11, lighten(body, .05), ol)        # голова/шлем
    d.rectangle([26, 22, 38, 25], fill=darken(body, .3))  # визор
    ell(d, 17, 38, 5, 5, body, ol)                        # руки
    ell(d, 47, 38, 5, 5, body, ol)
    rrect(d, [24, 34, 40, 39], PAL["leather"], w=1)       # ремень
    return im

def gen_walker():
    im = canvas(64); d = ImageDraw.Draw(im)
    b = PAL["walker"]; ol = darken(b)
    ell(d, 32, 38, 16, 14, b, ol)                          # тело
    for i, x in enumerate((22, 32, 42)):                   # рваные лохмотья
        poly(d, [(x - 6, 46), (x + 6, 46), (x, 56 - i)], darken(b, .7), w=1)
    ell(d, 32, 22, 10, 9, lighten(b, .1), ol)              # голова
    eyes(d, 32, 21, 4, 2)
    ell(d, 22, 16, 3, 7, lighten(b, .05), ol)              # руки вперёд
    ell(d, 42, 16, 3, 7, lighten(b, .05), ol)
    return im

def gen_runner():
    im = canvas(64); d = ImageDraw.Draw(im)
    b = PAL["runner"]; ol = darken(b)
    ell(d, 32, 40, 11, 18, b, ol)                          # вытянутый корпус
    poly(d, [(26, 56), (38, 56), (32, 64)], darken(b, .7), w=1)   # хвост
    ell(d, 32, 20, 9, 8, lighten(b, .1), ol)               # голова
    poly(d, [(24, 14), (28, 4), (31, 13)], b, ol, w=1)     # уши
    poly(d, [(40, 14), (36, 4), (33, 13)], b, ol, w=1)
    eyes(d, 32, 19, 4, 2)
    return im

def gen_tank():
    im = canvas(64); d = ImageDraw.Draw(im)
    b = PAL["tank"]; ol = darken(b); plate = PAL["wood"]
    ell(d, 32, 36, 22, 20, b, ol)
    rrect(d, [6, 24, 22, 44], plate, darken(plate), r=6)   # наплечники
    rrect(d, [42, 24, 58, 44], plate, darken(plate), r=6)
    poly(d, [(10, 24), (14, 14), (18, 24)], lighten(plate, .1), w=1)  # шипы
    poly(d, [(46, 24), (50, 14), (54, 24)], lighten(plate, .1), w=1)
    ell(d, 32, 22, 12, 11, lighten(b, .12), ol)            # голова
    eyes(d, 32, 21, 5, 2)
    return im

def gen_shooter():
    im = canvas(64); d = ImageDraw.Draw(im)
    b = PAL["shooter"]; bone = PAL["bone"]
    ell(d, 32, 38, 15, 14, b, darken(b))
    ell(d, 32, 23, 10, 10, bone, darken(bone))             # череп
    eyes(d, 32, 22, 4, 2, (10, 10, 20, 255))
    d.arc([38, 8, 60, 40], start=-70, end=70, fill=PAL["leather"], width=3)   # лук
    d.line([49, 10, 49, 38], fill=(230, 230, 235, 255), width=1)              # тетива
    ell(d, 18, 34, 4, 4, bone, darken(bone))               # рука
    return im

def gen_sprinter():
    im = canvas(64); d = ImageDraw.Draw(im)
    b = PAL["sprinter"]; ol = darken(b)
    ell(d, 32, 40, 10, 14, b, ol)
    ell(d, 32, 24, 7, 6, lighten(b, .1), ol)
    poly(d, [(27, 20), (25, 12), (30, 18)], b, ol, w=1)
    poly(d, [(37, 20), (39, 12), (34, 18)], b, ol, w=1)
    eyes(d, 32, 23, 3, 1)
    d.arc([20, 40, 34, 60], start=250, end=60, fill=hx("#e5989b"), width=2)   # хвост
    return im

def gen_kamikaze():
    im = canvas(64); d = ImageDraw.Draw(im)
    b = PAL["kamikaze"]; ol = darken(b)
    ell(d, 32, 38, 16, 16, b, ol)
    ell(d, 26, 32, 5, 4, lighten(b, .35), None)            # блик
    d.line([32, 22, 32, 12], fill=PAL["leather"], width=3) # запал
    d.ellipse([29, 6, 35, 12], fill=PAL["gold"])           # искра
    return im

def boss_base(col, r=26):
    im = canvas(64); d = ImageDraw.Draw(im)
    ell(d, 32, 36, r - 4, r - 6, col)
    ell(d, 32, 22, r // 2 + 1, r // 2, lighten(col, .12))
    return im, d

def gen_butcher():
    im, d = boss_base(PAL["butcher"])
    hood = darken(PAL["butcher"], .55)
    ell(d, 32, 22, 13, 11, hood)                            # капюшон
    eyes(d, 32, 23, 4, 2, PAL["gold"] + (255,))
    d.line([8, 30, 56, 30], fill=PAL["wood"], width=4)      # топорище
    poly(d, [(48, 20), (60, 26), (48, 34)], PAL["steel"], darken(PAL["steel"]), w=2)
    return im

def gen_foundry():
    im, d = boss_base(PAL["foundry"])
    d.line([50, 8, 50, 52], fill=PAL["wood"], width=4)      # посох
    d.ellipse([44, 2, 56, 14], fill=PAL["gold"], outline=darken(PAL["gold"]), width=2)
    ell(d, 32, 22, 12, 11, lighten(PAL["foundry"], .15))
    eyes(d, 32, 22, 4, 2, (240, 255, 255, 255))
    d.arc([12, 26, 52, 54], start=180, end=360, fill=darken(PAL["foundry"], .6), width=3)  # мантия
    return im

def gen_executioner():
    im, d = boss_base(PAL["executioner"])
    for a in (45, -45):
        x, y = 32, 34
        dx, dy = math.cos(math.radians(a)), math.sin(math.radians(a))
        d.line([x - dx * 26, y - dy * 26, x + dx * 26, y + dy * 26],
               fill=PAL["steel"], width=4)
    ell(d, 32, 22, 12, 11, darken(PAL["executioner"], .6))
    eyes(d, 32, 22, 4, 2, PAL["gold"] + (255,))
    return im

def gen_overlord():
    im, d = boss_base(PAL["overlord"], 28)
    poly(d, [(20, 16), (10, 2), (26, 12)], PAL["bone"], w=1)   # рога
    poly(d, [(44, 16), (54, 2), (38, 12)], PAL["bone"], w=1)
    eyes(d, 32, 22, 5, 2, PAL["gold"] + (255,))
    rrect(d, [22, 44, 42, 50], PAL["gold"], w=1)               # ворот-золото
    return im

# ---------- items & world ----------

def gen_sword():
    im = Image.new("RGBA", (16, 48), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    poly(d, [(8, 2), (12, 10), (12, 28), (4, 28), (4, 10)], PAL["steel"], darken(PAL["steel"]), w=1)
    d.line([8, 6, 8, 26], fill=(255, 255, 255, 220), width=1)
    rrect(d, [2, 28, 14, 32], PAL["wood"], w=1)            # гарда
    rrect(d, [6, 32, 10, 44], PAL["leather"], w=1)         # рукоять
    d.ellipse([5, 43, 11, 47], fill=PAL["gold"])           # навершие
    return im

def gen_orb(col, s=32):
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    c = s // 2
    for rr, a in ((s // 2, 40), (s // 3, 90), (s // 4, 160)):
        d.ellipse([c - rr, c - rr, c + rr, c + rr], fill=col + (a,))
    core = s // 6
    d.ellipse([c - core, c - core, c + core, c + core], fill=(255, 255, 255, 235))
    return im

def gen_tile(seed=1):
    rnd = random.Random(seed)
    im = Image.new("RGBA", (128, 128), hx("#232538") + (255,)); d = ImageDraw.Draw(im)
    for _ in range(46):
        x, y, s = rnd.randint(0, 124), rnd.randint(0, 124), rnd.choice((3, 4, 5))
        col = rnd.choice([hx("#262840"), hx("#212335"), hx("#282a45")])
        d.rectangle([x, y, x + s, y + s], fill=col + (255,))
    return im

def gen_rock():
    im = canvas(48); d = ImageDraw.Draw(im)
    poly(d, [(10, 38), (6, 24), (18, 10), (36, 12), (42, 30), (34, 40)],
         hx("#4a4e77"), darken(hx("#4a4e77")), w=2)
    poly(d, [(18, 12), (36, 12), (30, 22), (16, 22)], lighten(hx("#4a4e77"), .12), w=1)
    return im

def gen_grass():
    im = canvas(48); d = ImageDraw.Draw(im)
    for i, x in enumerate((14, 20, 24, 29, 34)):
        d.line([x, 40, x + (i - 2) * 2, 22 + abs(i - 2) * 3], fill=hx("#3a5a40"), width=3)
    return im

def gen_skull():
    im = canvas(48); d = ImageDraw.Draw(im)
    ell(d, 24, 22, 11, 10, PAL["bone"], darken(PAL["bone"]))
    d.rectangle([18, 28, 30, 36], fill=PAL["bone"], outline=darken(PAL["bone"]))
    eyes(d, 24, 21, 5, 3, (15, 15, 25, 255))
    d.line([24, 28, 24, 32], fill=(15, 15, 25, 200), width=2)
    return im

def gen_icon():
    im = Image.new("RGBA", (512, 512), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for rr, col in ((250, hx("#1a1b2e")), (236, hx("#2b2d42"))):
        d.ellipse([256 - rr, 256 - rr, 256 + rr, 256 + rr], fill=col + (255,))
    d.arc([136, 136, 376, 376], start=200, end=520, fill=hx("#3d4060"), width=6)
    sw = gen_sword().resize((48, 144))
    for ang in (210, 270, 330):
        rot = sw.rotate(ang - 270, expand=True, resample=Image.BICUBIC)
        px = 256 + int(150 * math.cos(math.radians(ang - 90)))
        py = 256 + int(150 * math.sin(math.radians(ang - 90)))
        im.alpha_composite(rot, (px - rot.width // 2, py - rot.height // 2))
    ell(d, 256, 256, 118, 118, PAL["red"], darken(PAL["red"], .5), w=10)
    ell(d, 256, 236, 62, 62, lighten(PAL["red"], .18))
    eyes(d, 256, 234, 26, 12, (20, 20, 30, 255))
    return im

# ---------- run ----------

SPRITES = {
    "player": gen_player(), "walker": gen_walker(), "runner": gen_runner(),
    "tank": gen_tank(), "shooter": gen_shooter(), "sprinter": gen_sprinter(),
    "kamikaze": gen_kamikaze(), "butcher": gen_butcher(), "foundry": gen_foundry(),
    "executioner": gen_executioner(), "overlord": gen_overlord(),
    "sword": gen_sword(), "orb": gen_orb(PAL["gold"]),
    "orb5": gen_orb(PAL["heal"], 40), "tile": gen_tile(7),
    "decor_rock": gen_rock(), "decor_grass": gen_grass(), "decor_skull": gen_skull(),
}

for name, im in SPRITES.items():
    im.save(os.path.join(OUT, f"{name}.png"))
icon_rgba = gen_icon()
icon_rgba.convert("RGB").save(os.path.join(OUT, "icon.png"))

# contact sheet
SPRITES["icon"] = icon_rgba
names = list(SPRITES)
cols, cell = 6, 140
rows = (len(names) + cols - 1) // cols
sheet = Image.new("RGBA", (cols * cell, rows * cell + 20), hx("#1a1b2e") + (255,))
sd = ImageDraw.Draw(sheet)
for i, n in enumerate(names):
    x, y = (i % cols) * cell + 10, (i // cols) * cell + 6
    im = SPRITES[n]
    im2 = im.copy(); im2.thumbnail((cell - 24, cell - 34))
    sheet.alpha_composite(im2, (x + (cell - 20 - im2.width) // 2, y + (cell - 30 - im2.height) // 2))
    sd.text((x + 4, y + cell - 24), n, fill=hx("#b8bdd4") + (255,))
sheet.convert("RGB").save(SHEET)
print("art generated:", len(SPRITES) + 1, "files ->", os.path.abspath(OUT))
