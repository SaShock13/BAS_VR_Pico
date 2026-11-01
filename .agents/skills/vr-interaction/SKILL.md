---
name: vr-interaction
description: Implements or fixes PICO VR interactions with XR Interaction Toolkit 3.3 (grab, sockets, UI poke, comfort). Use when the user mentions XR, grab, hands, controllers, sockets, XRGrabInteractable, XRIT, Pico, or comfort.
---

# VR-взаимодействие

Целевой девайс: **PICO**. Пакет: `com.unity.xr.interaction.toolkit` **3.3.x**. Риг и starter assets уже в проекте — расширять, не дублировать.

## Делать

- Захват детали: существующий `XRGrabInteractable` на `DronePartView`.
- Сокеты: логика в `SocketView` + события attach/detach в `Clean_AssemblySystem`.
- UI: world-space + XRIT; не `OnMouse*` / standalone EventSystem-only.
- Ввод: Input System actions, уже используемые XRIT.

## Не делать

- Свой Physics.Raycast «палец» вместо XRIT interactor.
- Резкий warp камеры / постоянный parent камеры к детали.
- Копипаст из XRIT 2.x (`XRRayInteractor` setup старых семплов) без сверки Context7.

Перед API-вызовом: Context7, library id для XR Interaction Toolkit 3.3.
