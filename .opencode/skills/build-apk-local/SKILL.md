---
name: build-apk-local
description: Build a locally produced debug-signed Android APK (v0.1 native preview) using dotnet android workload at maximum CPU parallelism. Use when user says «собери апк», «локально apk», «нужен apk файл».
---

# Build APK Locally (native preview v0.1)

Собирает `dist/Rubilovo-*.apk` из native/Rubilovo.Android — играбельное
превью ядра (Logic) на Android Canvas. Unity-сборка идёт отдельно через GameCI.

## 0. Требования (один раз)

```bash
dotnet workload install android          # ~1.5 ГБ, JDK17 нужен (есть в системе)
```

## 1. Сборка на всех ядрах CPU

GPU компиляцией не используется (тулчейн не умеет) — жмём все потоки:

```bash
cd /home/userdev/Документы/python/orbit-survivor
dotnet publish native/Rubilovo.Android -c Release -f net10.0-android \
  -maxcpucount:6 /p:UseSharedCompilation=false
```

## 2. Артефакт

```bash
mkdir -p dist && cp native/Rubilovo.Android/bin/Release/net10.0-android/publish/*.apk dist/
ls -lh dist/*.apk
```

- APK подписан debug-ключом автоматически → ставится на любое устройство
  («неизвестные источники»). Для RuStore — релизная подпись через GameCI.
- Рендеринг игры: GPU устройства (`hardwareAccelerated=true` в манифесте).

## 3. Если workload сломался

```bash
dotnet workload update && dotnet workload list | grep android
```
