---
name: logic-tests
description: Run V1 unit tests for pure game logic (XpCurve, Economy, WaveScript, UpgradeDeck). Use when user says «прогони тесты», «тесты логики», «проверь формулы», or before any commit touching Assets/Scripts/Logic.
---

# Logic Tests (V1)

Запускает 47+ проверок формул против таблиц спек (docs/spec).

```bash
cd /home/userdev/Документы/python/orbit-survivor
dotnet run --project tests/Logic.Tests
```

- Ожидаемый вывод в конце: `ALL TESTS PASSED`.
- Если FAIL: править код под спеку, а НЕ тест под код (числа взяты из
  docs/spec/01 §7 и docs/spec/02 §1–3).
- Любое изменение чисел в Logic/GameBalance.cs обязано сопровождаться
  обновлением соответствующей таблицы в спеке тем же коммитом.
