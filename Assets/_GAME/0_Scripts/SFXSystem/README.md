# SFXSystem

Библиотека `SFXLibrary.asset`: для каждого Id назначьте Clips. Пустые записи молчат.
Volume, PitchRange, SpatialBlend (0 = 2D, 1 = 3D), Min/MaxDistance, Loop,
Priority, MaxConcurrent и Output (AudioMixerGroup) задаются отдельно для звука.
Для пространственных эффектов предпочтительны mono-клипы.

`EventBusInstaller.sfxLibrary` подключает систему в текущий Zenject-контейнер.
Библиотека: `Assets/_GAME/Settings_SO/SFXLibrary.asset`.
Автоматические события сборки соответствуют VFXService.
`SFXButtonSound` публикует `PlaySFXRequest(UIClick)` из Button.onClick;
клип MenuClick.wav и его параметры задаются в библиотеке. Компонент не создаёт
AudioSource. Динамические UI-префабы создавайте через Container.InstantiatePrefab,
чтобы Zenject внедрил IEventBus. Второй контейнер не нужен.

```csharp
// На главном потоке Unity, через обычный Publish (не PublishAsync).
eventBus.Publish(new PlaySFXRequest { SFXId = SFXId.UIClick });
eventBus.Publish(new StopSFXRequest { SFXId = SFXId.UIClick });

// ISFXService внедряется через Zenject. Loop задаётся в библиотеке.
var handle = sfx.Play(SFXId.Checkpoint, transform.position, 1f, transform);
sfx.SetVolume(handle, 0.5f); // Множитель громкости записи.
sfx.Stop(handle);
```

IsValid означает выданный handle; завершившийся звук больше не управляется.
Play возвращает default при пустой записи или превышении лимита; новые звуки
не вытесняют старые. Приоритет используется Unity для виртуализации голосов.
FollowTarget следует мировой позиции; удаление цели останавливает звук.
Пауза AudioListener сохраняет занятые голоса. Time.timeScale не управляет аудио.

Пул создаётся один раз: 16 AudioSource по умолчанию, максимум 32. Tick без
активных звуков сразу выходит; при воспроизведении выполняет ограниченный проход
без managed-аллокаций. Dispose останавливает звуки и удаляет пул.
Mixer и spatializer проекта система не создаёт и не меняет.
