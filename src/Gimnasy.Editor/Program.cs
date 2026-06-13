using Gimnasy.Core.Object;
using Gimnasy.Core.Scene;
using Gimnasy.Core.Serialization;
using Gimnasy.Editor;
using Gimnasy.Scripting;

// ---------------------------------------------------------------------------
//  Gimnasy Editor — headless editor backend (the future GUI shell drives this
//  same API). Inspect scenes, browse the type catalog, scaffold and edit nodes.
//
//  Usage:
//    gimnasy-editor tree   <scene.scen>
//    gimnasy-editor types  [category]
//    gimnasy-editor new    <scene.scen> <RootType>
//    gimnasy-editor add    <scene.scen> <Type> <Name> [parentPath]
//    gimnasy-editor icons  <assets/editor/icons>
// ---------------------------------------------------------------------------

ScriptHost.Initialize();

if (args.Length == 0) { Usage(); return 0; }

switch (args[0])
{
    case "tree": return Tree(args);
    case "types": return Types(args);
    case "new": return NewScene(args);
    case "add": return AddNode(args);
    case "icons": return Icons(args);
    default: Usage(); return 1;
}

static int Tree(string[] a)
{
    if (a.Length < 2) { Console.Error.WriteLine("error: scene path required"); return 1; }
    var root = SceneSerializer.Load(a[1]);
    PrintTree(root, "");
    return 0;
}

static void PrintTree(Node node, string indent)
{
    var desc = ClassDb.Get(node.GetType());
    Console.WriteLine($"{indent}{node.Name} : {desc?.TypeName ?? node.GetType().Name}");
    foreach (var child in node.Children) PrintTree(child, indent + "  ");
}

static int Types(string[] a)
{
    string? filter = a.Length > 1 ? a[1] : null;
    foreach (var d in ClassDb.All.Values
                 .Where(t => filter is null || t.Category.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                 .OrderBy(t => t.Category).ThenBy(t => t.TypeName))
        Console.WriteLine($"{d.Category,-16} {d.TypeName}");
    Console.WriteLine($"\n{ClassDb.Count} types total.");
    return 0;
}

static int NewScene(string[] a)
{
    if (a.Length < 3) { Console.Error.WriteLine("error: usage: new <scene.scen> <RootType>"); return 1; }
    var root = (Node)ClassDb.Instantiate(a[2]);
    root.Name = a[2];
    SceneSerializer.Save(root, a[1]);
    Console.WriteLine($"created {a[1]} with root {a[2]}");
    return 0;
}

static int AddNode(string[] a)
{
    if (a.Length < 4) { Console.Error.WriteLine("error: usage: add <scene.scen> <Type> <Name> [parentPath]"); return 1; }
    var root = SceneSerializer.Load(a[1]);
    var node = (Node)ClassDb.Instantiate(a[2]);
    node.Name = a[3];
    var parent = a.Length > 4 ? root.GetNode(a[4]) : root;
    parent.AddChild(node);
    SceneSerializer.Save(root, a[1]);
    Console.WriteLine($"added {a[2]} '{a[3]}' under {parent.Name}");
    return 0;
}

static int Icons(string[] a)
{
    string root = a.Length > 1 ? a[1] : "assets/editor/icons";
    var icons = EditorIcons.Load(root);
    Console.WriteLine($"icons: {icons.ExtensionCount} file-type mappings, {icons.ActionCount} action mappings");
    foreach (var ext in new[] { "png", "cs", "py", "mp3", "scen", "material" })
        Console.WriteLine($"  .{ext,-9} -> {Path.GetFileName(icons.ForFile("x." + ext))}");
    return 0;
}

static void Usage() => Console.WriteLine("""
    Gimnasy Editor (headless backend)
      gimnasy-editor tree   <scene.scen>
      gimnasy-editor types  [category]
      gimnasy-editor new    <scene.scen> <RootType>
      gimnasy-editor add    <scene.scen> <Type> <Name> [parentPath]
      gimnasy-editor icons  [iconsDir]
    """);
