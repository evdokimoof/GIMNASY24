# Platformer3D — сборка под macOS (Xcode)

Готовый проект Xcode для сборки и запуска игры на macOS. Сгенерирован
командой:

```bash
python3 -m gimnasy.cli package Platformer3D --out <папка>
```

## Как открыть и собрать

1. Установи [.NET 8 SDK](https://dotnet.microsoft.com/download) (по умолчанию
   путь `/usr/local/share/dotnet/dotnet`).
2. Открой `Platformer3D.xcodeproj` в Xcode.
3. Выбери схему **Platformer3D** и нажми **Build (⌘B)** или **Run (⌘R)**.

Что происходит при сборке: цель использует *External Build System* и вызывает
`dotnet publish` для рантайма движка с runtime identifier `osx-arm64`,
складывая собранный бинарь прямо в `Platformer3D.app/Contents/MacOS/`. Схема
запускает получившийся `.app`.

## Настройки (в pbxproj, build settings)

| Параметр          | Значение по умолчанию                                   |
|-------------------|---------------------------------------------------------|
| `GIMNASY_PROJECT` | `../../../src/Gimnasy.Runtime/Gimnasy.Runtime.csproj`   |
| `GIMNASY_RID`     | `osx-arm64` (для Intel — `osx-x64`)                     |
| `GIMNASY_DOTNET`  | `/usr/local/share/dotnet/dotnet`                        |

Папки `Contents/MacOS` и `Contents/Resources` пустые в репозитории
(заполняются при сборке) — поэтому в них лежат файлы `.gitkeep`.
