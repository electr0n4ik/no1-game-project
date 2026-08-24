---
name: rustore-release
description: Full RuStore release checklist runner (version bump, build, card assets, moderation gates). Use when user says «релиз», «публикация в rustore», «выкатить версию».
---

# RuStore Release

Чеклист выката версии. Полные требования: docs/publish-rustore.md.

## 1. Подготовка кода
- [ ] `GameBalance.Version` и `ApplicationDisplayVersion` подняты (semver)
- [ ] Тестовые ad unit ID заменены на прод (`R-M-XXXXXX-Y`) в YandexAdsProvider.cs
- [ ] V1+V2 зелёные; CI зелёный

## 2. Сборка
- [ ] GameCI: Actions → release-apk (versionName/versionCode) ИЛИ
      локально скиллом `build-apk-local` + релизная подпись
- [ ] targetSdk ≥ 35, arm64-v8a, AAB предпочтительнее APK

## 3. Карточка
- [ ] Иконка 512×512 ≤5МБ; скриншоты 9:16 JPG 1080×1920 ×3–5 (реальный геймплей)
- [ ] Возрастной рейтинг выставлен; описание RU без внешних ссылок на сторы
- [ ] Privacy policy URL живой, упоминает Яндекс/VK рекламные SDK и advertising ID
- [ ] Экран согласия при первом запуске до инициализации рекламы

## 4. Выкат
- [ ] Тег в main → загрузка в console.rustore.ru → модерация 1–5 дней
- [ ] После одобрения: RuStore Review SDK уже опционально подключаем
