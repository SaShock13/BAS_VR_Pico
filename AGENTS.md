# BAS_VR — агентский контекст

VR-приложение для **автономного PICO 4 Ultra** (не PC Streaming): конструктор дронов в гараже, префлайт-валидация, полётная физика, миссии и подсказки. Бюджет кадра ~90 Hz / 11.1 ms, stereo, троттлинг. Новые фичи не поднимают URP mobile (`Mobile_RPAsset`) и не добавляют работу на каждый кадр без нужды. Ревью нагрузки — субагент `pico-perf-reviewer`.

## Стек

- Unity **6000.3** (URP), New Input System
- **XR Interaction Toolkit 3.3**, **PICO XR SDK**, Pico Live Preview
- **Zenject**, **Addressables**, собственный **IEventBus**
- Тесты: Unity Test Framework (NUnit)

Игровой код живёт в `Assets/_GAME/`. Не трогать `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`.

## Карта систем

| Область | Путь | Как расширять |
|---|---|---|
| Сборка дрона | `Assets/_GAME/0_Scripts/ASSYMBLY_System/` | Domain / Applaication / Presentation; события через EventBus |
| DI | `Assets/_GAME/0_Scripts/Zenject/` | Bind в существующий `*Installer`, не создавать второй контейнер |
| События | `Assets/_GAME/0_Scripts/EventBus/` | Тип события реализует `IAppEvent` |
| Гараж / сейв | `Garage/`, `SaveSystem/` | |
| Полёт | `FlyPhysics/` | |
| Миссии | `MissionsSystem/` | Шаги как ScriptableObject + runtime |
| Подсказки | `Hints/` | |
| Валидация | `DroneValidation/` | |
| VR UI / руки | `UI/`, `HandTablet/`, `SelectionService/` | XRIT interactables, не uGUI-only клики |
| Ассеты | `AseetLoader/` (опечатка в имени — не переименовывать без нужды) | Addressables |

## Соглашения (факт репозитория)

- Скрипты **без namespace** (глобальные типы). Новые файлы — так же, пока явно не попросили иначе.
- Имена папок и классов местами с опечатками (`ASSYMBLY`, `Applaication`, `Clean_*`). Не «чинить» переименованием в рамках фичи.
- Связь систем: **событие → обработчик в application → view/prefab**. Не тянуть View напрямую в Domain.
- Компоненты на сцене: `[SerializeField]` + Zenject `FromInstance` / `FromComponentInHierarchy`, не `FindObjectOfType` в рантайме.
- Документацию Unity / XRIT / Addressables / Zenject брать через Context7 MCP, не из памяти.

## Что агент не делает

- Не правит YAML сцен/префабов вручную, если доступен Unity MCP и редактор запущен.
- Не коммитит `.meta` без парного ассета и не удаляет `.meta` «как мусор».
- Не добавляет Input Manager (старый), только Input System + XRIT.
- Не добавляет камеры, fullscreen-эффекты, realtime extra shadows и per-frame аллокации «как на PC».
