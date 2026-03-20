using System;
using System.Reflection;
class Program {
    static void Main() {
        var asm = Assembly.LoadFrom("DLLS/RCLibrary.dll");
        Console.WriteLine("=== All Types ===");
        foreach (var tp in asm.GetTypes())
            if (tp.Name.Contains("Debug") || tp.Name.Contains("debug"))
                Console.WriteLine("  " + tp.FullName);
        var t = asm.GetType("DebugSystem") ?? asm.GetType("RCLibrary.DebugSystem") ?? asm.GetType("RCLibrary.Core.DebugSystem");
        if (t == null) {
            foreach (var tp in asm.GetTypes())
                if (tp.Name == "DebugSystem") { t = tp; break; }
        }
        if (t == null) { Console.WriteLine("DebugSystem not found in any namespace"); return; }
        Console.WriteLine("=== Fields ===");
        foreach (var f in t.GetFields(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            Console.WriteLine("  {0} {1} {2}", f.IsStatic?"static":"", f.FieldType.Name, f.Name);
        Console.WriteLine("=== Methods ===");
        foreach (var m in t.GetMethods(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            Console.WriteLine("  {0} {1} {2}({3})", m.IsStatic?"static":"", m.ReturnType.Name, m.Name, string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name+" "+p.Name)));
    }
}
