# Форматы файлов

Все форматы — **JSON-подобный синтаксис**: на запись это валидный JSON, на
чтение допускаются комментарии (`//`, `/* */`) и висячие запятые, чтобы файлы
можно было править руками. Векторы и цвета хранятся массивами чисел.

## `project.gimnasy` — манифест проекта

```jsonc
{
  "format": 1,
  "name": "Platformer3D",
  "main_scene": "res://main.scen",
  "script_assembly": "res://build/Game.dll",  // опционально
  "window_width": 1280,
  "window_height": 720,
  "physics_tick_rate": 60
}
```

## `.scen` — сцена (PackedScene)

Плоский список узлов; каждый знает свой тип, путь родителя и изменённые
свойства. Узлы идут «корень-первым»; `parent: null` — корень, `parent: "."` —
ребёнок корня, иначе — путь от корня.

```jsonc
{
  "format": 1,
  "type": "PackedScene",
  "root": "Level",
  "nodes": [
    { "name": "Level",  "type": "Node3D",         "parent": null, "properties": {} },
    { "name": "Sun",    "type": "DirectionalLight3D", "parent": ".",
      "properties": { "RotationDegrees": [-50,-30,0], "LightEnergy": 1.2 } },
    { "name": "Player", "type": "CharacterBody3D", "parent": ".",
      "properties": { "Position": [0,1,0], "Speed": 6.0 },
      "script": "res://scripts/Player.cs" },
    { "name": "Camera", "type": "Camera3D",        "parent": "Player",
      "properties": { "Position": [0,3,8], "Fov": 70 } }
  ]
}
```

Сохраняются только свойства, отличающиеся от значений по умолчанию.

## `.material` — материал

```jsonc
{
  "format": 1,
  "type": "StandardMaterial3D",
  "properties": {
    "AlbedoColor": [0.2, 0.6, 1.0, 1.0],
    "Metallic": 0.1,
    "Roughness": 0.4,
    "EmissionEnabled": true,
    "Emission": [0.0, 0.1, 0.3, 1.0]
  }
}
```

Типы: `StandardMaterial3D`, `ShaderMaterial`, `CanvasItemMaterial`, `PhysicsMaterial`.

## `.shadergraph` — нодовый шейдер

Узлы (`id`, `type`, `pos`, `params`, `strings`) и связи (`from`/`from_port` →
`to`/`to_port`). Любое значение в графе — `vec4`. Компилируется в GLSL
(`gimnasy-editor shader …` или `gimnasy.cli shader …`).

```jsonc
{
  "format": 1, "type": "ShaderGraph", "name": "Water", "unshaded": false,
  "nodes": [
    { "id": "output",  "type": "Output", "pos": [600,0] },
    { "id": "shallow", "type": "Color",  "pos": [0,-120], "params": { "value": [0.2,0.6,0.7,1] } },
    { "id": "deep",    "type": "Color",  "pos": [0, 120], "params": { "value": [0.05,0.2,0.35,1] } },
    { "id": "uv",      "type": "UV",     "pos": [-200,240] },
    { "id": "noise",   "type": "Noise",  "pos": [200,240] },
    { "id": "blend",   "type": "Mix",    "pos": [400,0] }
  ],
  "connections": [
    { "from": "uv",      "to": "noise", "to_port": "uv" },
    { "from": "shallow", "to": "blend", "to_port": "a" },
    { "from": "deep",    "to": "blend", "to_port": "b" },
    { "from": "noise",   "to": "blend", "to_port": "t" },
    { "from": "blend",   "to": "output","to_port": "albedo" }
  ]
}
```

Узлы графа: `Output`, `Float`, `Vector3`, `Color`, `Time`, `UV`, `Normal`,
`Add`, `Subtract`, `Multiply`, `Divide`, `Mix`, `Dot`, `Cross`, `Normalize`,
`Length`, `Sin`, `Cos`, `Power`, `Clamp01`, `OneMinus`, `Fresnel`, `Noise`,
`TextureSample`. Порты `Output`: `albedo`, `metallic`, `roughness`,
`emission`, `alpha`.

## Ресурсы `.tres` и `.import`

Импортёр (`gimnasy.cli import`) копирует ассет в `assets/` и пишет рядом
`<файл>.import` (как загружать) и, для картинок, `<файл>.tres` (готовый
`Texture2D`).
