---
name: gitflow-feature
description: Start or finish a feature branch per project GitFlow (develop-based, PR + squash, protected main). Use when user says «начни фичу», «новая ветка», «заверши фичу», «вмержи в develop».
---

# GitFlow Feature

## Начать

```bash
cd /home/userdev/Документы/python/orbit-survivor
git checkout develop && git pull
git checkout -b feature/<slug>
```

## Работать

- Коммиты conventional (EN), по логическим точкам, `Refs:` на спеку в теле.
- Перед каждым коммитом: V1 (+V2 если трогали Unity-слой) — скиллы
  `logic-tests`, `syntax-check`.

## Завершить

```bash
git push -u origin feature/<slug>
gh pr create --base develop --fill
gh pr merge --squash --delete-branch
```

- main не трогаем; туда ветка попадёт только через release/hotfix.
- CI «Logic unit tests» обязан быть зелёным до мержа.

## Релиз (когда спросят)

```bash
git checkout -b release/x.y.z develop   # стабилизация
git checkout main && git merge --no-ff release/x.y.z && git tag vx.y.z
git checkout develop && git merge --no-ff release/x.y.z
git push origin main develop --tags     # затем GameCI собирает APK из main
```
