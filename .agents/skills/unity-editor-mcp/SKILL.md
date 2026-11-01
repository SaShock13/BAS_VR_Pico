---
name: unity-editor-mcp
description: Uses Unity MCP to inspect the open editor, scenes, GameObjects, components, consoles, and to apply scene/prefab changes. Use when Unity is running, when editing scenes or prefabs, when console errors appear, or when the user mentions Play Mode, Hierarchy, or Inspector.
---

# Unity MCP

## Старт

1. `GetDynamicTools` по namespace `user-unityMCP` или `user-unity-mcp` (имя сервера в Codex может отличаться).
2. Если `namespaceStatus` = `error` / `needsAuth` — сказать пользователю: открыть Unity с проектом BAS_VR и проверить MCP в Codex Settings. **Не** эмулировать редактор правкой YAML.
3. Прочитать схему нужного tool, затем `CallDynamicTool`.

## Правила

- Сначала read/search (иерархия, компонент, консоль), потом mutate.
- После смены скрипта дождаться compile / проверить Console на ошибки.
- Не вызывать Play Mode без запроса. Если включили — выключить, когда проверка закончена.
- Prefab mode vs сцена: менять тот объект, который пользователь имеет в виду; не применять override на префаб XRIT Samples.

## Если MCP мёртв

Минимальный diff в `.cs` + точные инструкции «куда повесить компонент в инспекторе». Сцены не ломать.
