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

Что происходит при сборке: цель **Aggregate** запускает *Run Script* фазу,
которая сама находит `dotnet` (проверяет `command -v dotnet`, затем
`~/.dotnet`, Homebrew `/opt/homebrew/bin`, `/usr/local/bin`,
`/usr/local/share/dotnet`) и вызывает `dotnet publish` для рантайма движка с
runtime identifier `osx-arm64`, складывая бинарь прямо в
`Platformer3D.app/Contents/MacOS/`. Схема запускает получившийся `.app`.

> Раньше использовался External Build System — он давал ошибку Xcode
> «never received target ended message». Теперь это обычный Aggregate-таргет с
> Run Script, который работает стабильно.

## Настройки (в pbxproj, build settings)

| Параметр          | Значение по умолчанию                                   |
|-------------------|---------------------------------------------------------|
| `GIMNASY_PROJECT` | `../../../src/Gimnasy.Runtime/Gimnasy.Runtime.csproj`   |
| `GIMNASY_RID`     | `osx-arm64` (для Intel — `osx-x64`)                     |

Путь к `dotnet` определяется автоматически; если SDK не установлен, сборка
завершится понятной ошибкой со ссылкой на загрузку .NET 8.

Папки `Contents/MacOS` и `Contents/Resources` пустые в репозитории
(заполняются при сборке) — поэтому в них лежат файлы `.gitkeep`.
