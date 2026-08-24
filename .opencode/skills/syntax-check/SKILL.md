---
name: syntax-check
description: Compile ALL Unity game scripts against committed UnityEngine stubs without the Unity editor (V2 gate). Use when user says «проверь компиляцию», «синтаксис», «соберётся ли», or after editing anything under Assets/Scripts outside Logic.
---

# Syntax Check (V2)

Заглушки UnityEngine живут в репо: `tests/SyntaxCheck/UnityStubs.cs`.
Проект включает ВСЕ скрипты `Assets/Scripts/**` и ловит опечатки/битые API
без редактора Unity.

```bash
cd /home/userdev/Документы/python/orbit-survivor
dotnet build tests/SyntaxCheck/SyntaxCheck.csproj -maxcpucount:6
```

- Цель: `Build succeeded. 0 Errors`. Предупреждения CS0414/CS0649 игнорируются.
- Ошибка вида «тип не найден» может быть пробелом ЗАГЛУШКИ, а не кода — тогда
  дополни `UnityStubs.cs` минимальной сигнатурой тем же коммитом.
- После правки заглушек перезапусти V1 (`logic-tests`) — они шарят исходники Logic.
