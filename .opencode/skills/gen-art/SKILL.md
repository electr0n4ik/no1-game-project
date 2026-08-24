---
name: gen-art
description: Regenerate all game sprites/icons from the procedural generator and rebuild the APK. Use when user says «сгенерируй арт», «перегенерируй спрайты», «обнови картинки», «новая иконка».
---

# Gen Art

Весь визуал нативного превью генерируется кодом (стиль по спеке 05).

```bash
cd /home/userdev/Документы/python/orbit-survivor
python3 tools/gen_art.py
```

- Выход: `native/Rubilovo.Android/Resources/drawable/*.png` (19 спрайтов +
  icon.png) и контактный лист `docs/img/art-sheet.png` (показать пользователю!).
- Палитра/образы — ТОЛЬКО через правку `docs/spec/05-visual-bible.md` +
  генератора одним коммитом (Refs: spec 05 §N).
- После генерации: собрать APK скиллом `build-apk-local` (6 потоков),
  на устройстве проверить скиллом `debug-device`.

## Грабли
- `gen_icon()` возвращает RGBA; RGB-конверсия только при сохранении icon.png
  (иначе alpha_composite контактного листа падает "images do not match").
- Новый спрайт = добавить в SPRITES + имя в LoadArt() MainActivity.
