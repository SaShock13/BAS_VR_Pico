---
name: pico-standalone-perf
description: Constrains BAS_VR features to Pico 4 Ultra standalone budgets (90 Hz stereo, thermal, URP mobile). Use when adding rendering, VFX, UI, physics, outlines, painting, cameras, lights, or when reviewing performance on Pico.
---

# Производительность Pico 4 Ultra

Автономный VR: два глаза, высокий fill-rate, троттлинг. Бюджет кадра **11.1 ms @ 90 Hz**. Не оптимизировать «как на PC».

Эталон качества: `Assets/Settings/Mobile_RPAsset.asset` (render scale 0.8, MSAA 1, additional shadows off, SRP Batcher). Не поднимать эти цифры без явной задачи и замера на шлеме.

## Перед кодом

Оценить стоимость фичи в одном предложении: CPU каждый кадр / GPU fill / память / разово при действии игрока. Если «каждый кадр и на все детали дрона» — другой дизайн.

## Разрешено

- `MaterialPropertyBlock` (как в `DronePartView`), общий материал, GPU instancing где уже URP/SRP Batcher.
- Outline/highlight — включать существующий `Outline` на выбранной детали, не клонировать меши всем частям сразу.
- VFX/particles — короткий burst, offscreen stop, лимит частиц.
- Физика — `FixedUpdate` только в `FlyPhysics`; гараж без лишних Rigidbody на каждую гайку.
- UI — один world canvas на панель, не `Camera.main` в `Update` (кэш XR camera).
- Загрузка — Addressables, пул или disable, не `Resources.Load` в тике.

## Запрещено без явного ОК

- Дополнительная Camera / overlay camera / GrabPass / fullscreen override.
- `new Material()`, `mesh.colors`, `ReadPixels`, CPU texture paint каждый кадр (система рисования — только по триггеру, RT минимального размера).
- `GetComponent`/`GetComponentsInChildren`/`LINQ` в `Update`/`FixedUpdate`.
- Realtime reflections, extra shadow cascades, soft shadows, HDR heavy post (bloom+DOF+AA stack).
- Лог/`Debug.Log` в тике, аллокации строк, `foreach` по `GetComponents` без кэша.

## После патча

Делегировать **pico-perf-reviewer**. В ответе пользователю: что нагружает кадр и почему это разово, а не 90 раз в секунду.
