---
name: add-gameplay-feature
description: Adds a BAS_VR gameplay feature through EventBus, Zenject installer, and existing Domain/Application/Presentation folders. Use when adding a system, mechanic, mission step, garage/assembly command, or new service.
---

# Новая игровая фича BAS_VR

## Порядок

1. Найти ближайшую систему в `Assets/_GAME/0_Scripts/` и скопировать её слой, не изобретать архитектуру.
2. Данные и инварианты — Domain (пример: `ASSYMBLY_System/DOMAIN`).
3. Намерение игрока — `IAppEvent` (`*Request`/`*Command`) в application/Events; подписка в сервисе (`IInitializable`).
4. View/`MonoBehaviour` только транслирует XR/UI в Publish и отображает состояние.
5. Bind интерфейса в существующем `*Installer` (`Assets/_GAME/0_Scripts/Zenject/`).
6. Если нужен префаб/компонент на сцене — Unity MCP (скил `unity-editor-mcp`), не YAML руками.
7. Проверить: нет `UnityEditor` в runtime, нет `FindObjectOfType`, отписка от шины есть.
8. Стоимость на **Pico 4 Ultra**: скил `pico-standalone-perf`. Нет нового `Update`/камеры/VFX «на все детали». Затем субагент `pico-perf-reviewer`.

## Карта «куда класть»

| Запрос | Папка | Installer |
|---|---|---|
| Деталь, сокет, undo сборки | `ASSYMBLY_System/` | `AssemblyInstaller` |
| Меню руки, пикер, инспектор | `UI/`, `SelectionService/` | `UIInstaller` |
| Миссия / шаг | `MissionsSystem/` | существующий binder/factory |
| Подсказка | `Hints/` | UI или отдельный bind рядом с `HintService` |
| Полёт | `FlyPhysics/` | bootstrap сцены полёта |
| Префлайт FX | `DroneValidation/` | `DroneValidationInstaller` |
| Адресные ассеты | `AseetLoader/` | `AddressablesLoaderInstaller` |

Не создавать namespace. Не переименовывать `ASSYMBLY`/`Applaication`.
