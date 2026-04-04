namespace Whiteboard.Core.Compilation;

public interface IScriptCompiler
{
    ScriptCompileResult Compile(
        string json,
        string sourcePath,
        string templateCatalogPath,
        string mappingCatalogPath,
        string governedLibraryPath);
}
