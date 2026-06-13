# Gimnasy Engine

**Кроссплатформенный игровой движок** с архитектурой в духе Godot: дерево
сцены, узлы (nodes), сигналы, lifecycle-колбэки, сериализация сцен и
материалов в JSON-подобном формате, скриптинг на **C#**, нодовый редактор
шейдеров, террейн в стиле Roblox Studio, Grease Pencil, скульптинг мешей и
богатый набор VFX.

Ядро написано на **C# (.NET 8)**, инструментарий (импорт ассетов, валидация,
упаковка под Windows/Linux/macOS) — на **Python 3**.

> ⚠️ **Статус: ранняя версия (0.1).** Это прочный, целостный фундамент
> движка с рабочей архитектурой и инструментами, а **не** готовый конкурент
> Godot. Что реализовано и что в планах — см. [docs/ROADMAP.md](docs/ROADMAP.md).
> Графический бэкенд сейчас — headless (см. раздел «Рендеринг»).

---

## Возможности

- 🌳 **Дерево сцены и узлы** — `Node` → `Node2D` / `Node3D` / `Control`,
  жизненный цикл `_Ready` / `_Process` / `_PhysicsProcess`, группы, `QueueFree`.
- 🧩 **150+ типов объектов** — спрайты, камеры, источники света, физика 2D/3D,
  UI-контролы, аудио, анимация, навигация, частицы и многое другое
  (`gimnasy info` печатает полный список).
- 📡 **Сигналы** — строковые сигналы с подключением/отключением, как в Godot.
- 💾 **Сериализация** — сцены `.scen`, материалы `.material`, шейдер-графы
  `.shadergraph` в JSON-подобном синтаксисе (с комментариями и висячими запятыми).
- ⌨️ **C#-скриптинг** — скрипты это подклассы узлов с `[Export]`-свойствами;
  компилируются в сборку игры и регистрируются движком.
- 🎛️ **Нодовый редактор шейдеров** — граф узлов компилируется в GLSL.
- ⛰️ **Профессиональный террейн** (в духе Roblox Studio + World Machine) —
  height-field с 11 кистями (Add/Subtract/Grow/Erode/Smooth/Flatten/Paint/
  SeaLevel/Noise/Hole/Fill), мульти-слойные **сплатмапы** с авто-текстурированием
  по высоте и уклону, **гидравлическая и термальная эрозия**, фрактальный шум с
  domain warp, генерация **полного 3D-меша** (вершины, нормали, тангенты, UV,
  цвета вершин, дыры, **LOD-чанки**) и сериализация всего ландшафта в большой
  JSON `.terrain` (как экспорт 3D-модели).
- ✏️ **Grease Pencil** — рисование штрихами в 2D/3D, слои, упрощение кривых (RDP).
- 🗿 **Скульптинг и редактирование мешей** — кисти Draw / Inflate / Grab /
  Smooth / Flatten / Pinch / Crease, операции Subdivide / Extrude / Smooth.
- 🌫️ **VFX** — дым, огонь, вода, облака, объёмный туман, взрывы, трейлы,
  молнии, брызги, погода и пост-эффекты экрана.
- ✨ **Сложная система частиц** (как Unity Shuriken / Godot) — 16 модулей
  (emission+bursts, shape, velocity/force/limit over lifetime, color/size/
  rotation over lifetime, noise/turbulence, collision, sub-emitters, texture
  sheet, trails, lights, renderer), кривые `MinMaxCurve` и градиенты, CPU-
  симулятор и полная сериализация в JSON `.particles`.
- 🛠️ **Инструментарий на Python** — импорт медиа/3D, валидация форматов,
  генерация манифеста иконок, упаковка под Windows/Linux/macOS.
- 🖼️ **Иконки редактора** — набор из 54 иконок с автогенерируемым манифестом.

## Структура репозитория

```
GimnasyEngine.sln            Solution (.NET)
src/
  Gimnasy.Core/              ядро: математика, объекты, сцена, сериализация,
                             ресурсы, террейн, геометрия (скульпт), шейдер-граф
  Gimnasy.Nodes/             каталог из 150+ типов узлов (2D/3D/UI/VFX/террейн…)
  Gimnasy.Scripting/         базовый класс скрипта и загрузчик сборок
  Gimnasy.Runtime/           игровой цикл, абстракция рендера, плеер (exe)
  Gimnasy.Editor/            backend редактора (CLI): дерево, типы, иконки, шейдеры
tools/
  gimnasy/                   Python-пакет инструментов
  tests/                     юнит-тесты инструментов (19 тестов)
assets/editor/icons/         иконки редактора + manifest.json
examples/platformer3d/       пример проекта (сцена, материал, скрипт, шейдер-граф)
docs/                        архитектура, форматы, дорожная карта
```

## Сборка движка (.NET 8)

Нужен [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build GimnasyEngine.sln -c Release

# Список всех зарегистрированных типов:
dotnet run --project src/Gimnasy.Runtime -- info

# Запуск примера в headless-режиме на 120 кадров:
dotnet run --project src/Gimnasy.Runtime -- run examples/platformer3d/project.gimnasy --frames 120

# Backend редактора:
dotnet run --project src/Gimnasy.Editor -- tree   examples/platformer3d/main.scen
dotnet run --project src/Gimnasy.Editor -- types  3D
dotnet run --project src/Gimnasy.Editor -- shader examples/platformer3d/water.shadergraph
```

## Инструментарий (Python 3)

Не требует .NET — удобно для CI и для проверки ассетов.

```bash
cd tools

# Валидация всех .scen / .material / .shadergraph в проекте:
python3 -m gimnasy.cli validate ../examples/platformer3d

# Компиляция нодового шейдера в GLSL:
python3 -m gimnasy.cli shader ../examples/platformer3d/water.shadergraph

# Генерация полного ландшафта (большой JSON) и системы частиц:
python3 -m gimnasy.cli gen-terrain ../examples/platformer3d/island.terrain --res 65
python3 -m gimnasy.cli gen-particles ../examples/platformer3d/fire.particles

# Импорт медиа/3D ассета в проект (png/jpg/ogg/mp3/fbx/glb/ttf…):
python3 -m gimnasy.cli import path/to/hero.png ../examples/platformer3d

# Генерация манифеста иконок редактора:
python3 -m gimnasy.cli gen-icons ../assets/editor/icons

# Упаковка под все ОС (exe для Windows, бинарь для Linux, .app + .xcodeproj для macOS):
python3 -m gimnasy.cli package MyGame --out build

# Новый пустой проект:
python3 -m gimnasy.cli new-project ./MyGame --name "My Game"

# Тесты инструментария:
python3 -m unittest discover -s tests
```

## Упаковка под Windows / Linux / macOS

`gimnasy.cli package` генерирует:

- `build/build_windows.bat` — self-contained `.exe` (`dotnet publish -r win-x64 -p:PublishSingleFile=true`);
- `build/build_linux.sh` — self-contained бинарь (`linux-x64`);
- `build/build_macos.sh` — бинарь (`osx-arm64`);
- `build/MyGame.app/` — каркас macOS app-бандла с `Info.plist`;
- `build/MyGame.xcodeproj/` — проект Xcode (External Build System), вызывающий `dotnet publish`.

Сами команды `dotnet publish` нужно запускать там, где установлен .NET 8 SDK.

## Рендеринг

Движок построен вокруг интерфейса `IRenderingServer`. Сейчас в комплекте —
**headless-бэкенд** (`NullRenderingServer`), который прогоняет всю логику (дерево
сцены, скрипты, физику, сигналы) без GPU. Это позволяет запускать игры на CI и
серверах и служит эталоном для будущего GPU-бэкенда (OpenGL/Vulkan) — см.
[docs/ROADMAP.md](docs/ROADMAP.md).

## Форматы файлов

Подробно — в [docs/FORMATS.md](docs/FORMATS.md). Кратко:

```jsonc
// main.scen — сцена
{ "type": "PackedScene", "root": "Level", "nodes": [
  { "name": "Level",  "type": "Node3D",          "parent": null, "properties": {} },
  { "name": "Player", "type": "CharacterBody3D",  "parent": ".",  "properties": { "Position": [0,1,0] } }
]}
```

```jsonc
// player.material — материал
{ "type": "StandardMaterial3D", "properties": { "AlbedoColor": [0.2,0.6,1,1], "Roughness": 0.4 } }
```

## Документация

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — устройство движка.
- [docs/FORMATS.md](docs/FORMATS.md) — форматы `.scen` / `.material` / `.shadergraph` / `project.gimnasy`.
- [docs/ROADMAP.md](docs/ROADMAP.md) — что готово и что дальше.

## Лицензия

См. [LICENSE](LICENSE).
