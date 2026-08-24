# Рубилово (рабочее название) — каркас игры

Гиперказуальная «залипалка» для RuStore: вид сверху, человечек бегает по арене,
вокруг вращаются клинки и рубят толпы врагов, за XP персонаж растёт как в agar.io.
Монетизация: rewarded/interstitial реклама (Яндекс → VK fallback).

## Стек
- Unity 6.3 LTS (6000.x), URP 2D Renderer
- Yandex Mobile Ads Unity Plugin 8.3.0 (OpenUPM `com.yandex.mobileads`)
- VK Ad Network (myTarget) — второй источник через медиацию Яндекса (позже)

## Структура
```
Assets/Scripts/
  Core/     GameManager, Health, ObjectPool, CameraFollow, SceneLoader
  Player/   PlayerController (drag-джойстик + WASD в редакторе),
            OrbitWeapons (вращающиеся клинки), PlayerGrowth (рост от XP)
  Enemies/  Enemy, XpOrb, EnemySpawner (волны по кольцу за камерой)
  Ads/      AdsManager (fallback-цепочка), IAdsProvider,
            YandexAdsProvider (SDK 8 API), VkAdsProvider (заглушка)
  UI/       HudController, MainMenuController, DeathScreen
docs/       GDD, чеклист публикации RuStore, монетизация самозанятого
docs/spec/  ПОЛНАЯ БИБЛИОТЕКА СПЕЦИФИКАЦИЙ («на берегу»):
            00-vision-market.md          — видение, конкуренты, KPI, роадмап M0-M4
            01-core-gameplay.md          — оружие/пассивки/эволюции/бестиарий/боссы/wave director
            02-meta-progression.md       — валюта МЯСО, дерево статов, персонажи, дейлики
            03-ui-ux-art-audio.md        — экраны, HUD, онбординг 30с, juice, арт, аудио
            04-monetization-analytics.md — мягкая реклама: плейсменты, капы, A/B, AppMetrica
setup/manifest-snippet.json — кусок для Packages/manifest.json
```

## Сборка проекта в Unity (первый запуск)
1. Unity Hub → New Project → **Universal 2D** (Unity 6.3 LTS).
   Linux-баг Hub: если модуль Android не появился в Build Settings — см.
   https://discussions.unity.com/t/1659425 (файлы из домашней папки перенести
   в `Editor/Data/PlaybackEngines/AndroidPlayer`).
2. Скопировать содержимое `Assets/Scripts` в свой проект.
3. Установить плагин рекламы: Window → Package Manager → «+» →
   «Install package by name» → `com.yandex.mobileads@8.3.0`.
4. Project Settings → Player → Scripting Define Symbols → добавить
   `YANDEX_MOBILEADS` (включит реальный код провайдера вместо заглушки).
5. Assets → External Dependency Manager → Android Resolver → **Resolve**
6. Publishing Settings: включить **Custom Main Gradle Template** и
   **Custom Gradle Properties Template**.
7. Build Settings: Android, IL2CPP, ARM64 (+ARMv7 опционально),
   Target API Level 35+, Min API 24.

## Сборка сцены (иерархия)
```
Bootstrap
 ├ GameManager        (скрипт Core/GameManager)
 ├ AdsManager         (Ads/AdsManager)
 └ Main Camera        (CameraFollow, Orthographic, size ~6)
Arena
 ├ Floor              (SpriteRenderer тайл травы/пола)
 ├ EnemyContainer     (контейнер пулов)
Player                (Layer=Player, tag=Player)
 ├ Rigidbody2D        (Gravity Scale=0, Collision Detection=Continuous)
 ├ CircleCollider2D   (не trigger)
 ├ SpriteRenderer     (человечек из kenney top-down-shooter)
 ├ Health             (Max=100, IsPlayer=true)
 ├ PlayerController   (ссылки health/cameraFollow)
 ├ PlayerGrowth       (ссылка weapons)
 └ OrbitWeapons       (child: Blade prefab c SpriteRenderer;
                       CircleCollider2D radius=1.1 isTrigger=true;
                       Layer=Weapon)
Enemy prefab          (Layer=Enemy; Rigidbody2D grav=0; CircleCollider2D;
                       Health Max=18)
XpOrb prefab          (CircleCollider2D isTrigger=true, Layer=XpOrb)
UI Canvas
 ├ Hud (timerLabel, levelLabel, xpBar Image fill, HudController→growth ref)
 ├ MenuPanel (MainMenuController: startButton)
 └ DeathPanel (DeathScreen: reviveButton, resultLabel)
```
Physics 2D matrix: Weapon↔Enemy = collide; Player↔XpOrb = collide; остальное off.

## Реклама
- Тестовые ID уже вшиты (`demo-rewarded-yandex`, `demo-interstitial-yandex`).
- Перед релизом заменить на реальные блоки вида `R-M-XXXXXX-Y` из кабинета РСЯ
  (YandexAdsProvider.cs).
- В редакторе реклама НЕ работает (только билд на устройство). Заглушка
  автогрантит reward для проверки флоу revive.
- Fallback: VK Ad Network подключается вторым провайдером (см. docs/monetization.md).

## Порядок запуска разработки
1. Открыть проект, собрать сцену по схеме выше, проверить движение/рост/спавн.
2. Собрать APK на телефон, убедиться что тестовая реклама показывается
   (logcat: `adb logcat | grep -i "Yandex Ads"`).
3. Зарегистрироваться в РСЯ как самозанятый (docs/monetization.md), получить
   реальные ad unit ID.
4. Иконка/скриншоты → docs/publish-rustore.md → публикация.
