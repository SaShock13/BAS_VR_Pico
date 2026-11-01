---
name: debug-unity
description: Diagnoses Unity compile errors, Play Mode exceptions, Zenject resolve failures, and VR runtime bugs. Use when there are console errors, NullReferenceException, ZenjectException, missing bindings, or Play Mode failures.
---

# Отладка Unity

## Сбор фактов

1. Текст ошибки целиком (тип, stack, скрипт:строка).
2. Unity MCP Console, если жив; иначе просить вставить лог.
3. Это compile, Zenject `Awake`/`Initialize`, или XR runtime?

## Частые причины в этом проекте

| Симптом | Куда смотреть |
|---|---|
| `ZenjectException` / missing binding | `*Installer`, сцена без installer |
| NRE на grab/socket | нет `XRGrabInteractable`, view не в `PartViewRegistry` |
| Event «не приходит» | нет `Subscribe` или событие не `IAppEvent` |
| `UnityEditor` в билде/плеере | `using UnityEditor` в runtime `.cs` |
| Addressables fail | ключ vs `AddressablesAssetService` |

## Фикс

Минимальный патч. Не логировать каждый кадр. После фикса — одна проверка: компиляция или MCP Console clear.
