---
name: unity-docs
description: Fetches current Unity, XR Interaction Toolkit, Addressables, Input System, URP, and Extenject/Zenject docs via Context7. Use before using Unity/XRIT/Zenject APIs, version-specific setup, or when unsure about XRIT 3 vs 2.
---

# Актуальная документация

Всегда Context7 MCP (`user-context7`): `resolve-library-id`, затем `query-docs`.

| Тема | Что резолвить |
|---|---|
| XRIT 3.3 | `com.unity.xr.interaction.toolkit` / XR Interaction Toolkit |
| Addressables | Unity Addressables |
| Input System | Unity Input System |
| URP 17 | Unity Universal Render Pipeline |
| Zenject | Extenject / Zenject |
| Editor/API | Unity 6 / 6000 |

Не опираться на XRIT 2 примеры (`OnSelectEntered` сигнатуры проверять в доке 3.x). В запросе указывать версию пакета из `Packages/manifest.json`.
