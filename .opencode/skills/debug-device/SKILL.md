---
name: debug-device
description: Debug the APK on a USB-connected phone via adb - install, launch, capture crashes, verify rendering. Use when user says «отладка на телефоне», «не запускается», «апк крашится», «проверь на устройстве».
---

# Debug Device

Полный цикл отладки APK на подключённом телефоне.

## 0. Окружение
```bash
ADB=~/android-sdk/platform-tools/adb   # ставится с workload (InstallAndroidDependencies)
$ADB devices -l                        # устройство в состоянии "device"?
```

## 1. Установить → запустить → проверить живость
```bash
$ADB install -r dist/Rubilovo-*.apk
$ADB logcat -c
$ADB shell monkey -p ru.no1game.rubilovo -c android.intent.category.LAUNCHER 1
sleep 7 && $ADB shell pidof ru.no1game.rubilovo || echo "PROCESS DEAD"
```

## 2. Если мёртв — буферы логов
```bash
$ADB logcat -d -b crash | tail -40     # нативные SIGABRT/SIGSEGV
$ADB logcat -d -s Game:* Monodroid:*   # наш тег + managed
```

## 3. Если жив, но чёрный экран
- `dumpsys gfxinfo <pkg> | grep 'Total frames'` → 0 кадров = постов нет
- `dumpsys SurfaceFlinger | grep -A6 '<pkg>#'` → `buffer=0x0` = кадры не доходят
- скриншот: `$ADB exec-out screencap -p > shot.png`

## 4. Известные грабли этого проекта (уже решены, не наступать)
| Симптом | Причина | Фикс |
|---|---|---|
| SIGABRT `klass->instance_size == instance_size` | marshal-methods баг .NET-Android 36.x | `AndroidEnableMarshalMethods=false` в csproj (стоит) |
| 1 кадр → чёрный экран, LockCanvas виснет | software-canvas vs BLAST у hw-окна (Android 16) | `HardwareAccelerated=false` (стоит) |
| `Android.Util` не найден | наш ns `Rubilovo.Android` перехватывает | писать `global::Android.Util.Log` |
| csproj не коммитится | gitignore `*.csproj` | негатив-правила уже в .gitignore |

## 5. Чёрный screencap ≠ чёрный экран
Если скриншот 24 КБ и байт-в-байт идентичен предыдущему — проверь
`adb shell "dumpsys display | grep -m1 mScreenState"`: при OFF автолок
выключил экран, игра при этом может жить (см. posted в логе).
Разбудить: `input keyevent KEYCODE_WAKEUP` + `wm dismiss-keyguard`.

## 6. Правило
Любой новый грабль с устройства заносить в таблицу §4 тем же коммитом.
