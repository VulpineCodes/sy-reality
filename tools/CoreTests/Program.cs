// Headless entry point (NUnitLite in-process runner) — this file lives outside
// Assets/ on purpose: Unity never compiles it; the Unity Test Framework runs
// the same test sources in-editor.
using System.Reflection;
using NUnitLite;

namespace SyReality.Tests
{
    public static class Program
    {
        public static int Main(string[] args) =>
            new AutoRun(Assembly.GetExecutingAssembly()).Execute(args);
    }
}
