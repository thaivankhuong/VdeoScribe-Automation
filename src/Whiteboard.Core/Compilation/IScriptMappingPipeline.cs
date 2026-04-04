namespace Whiteboard.Core.Compilation;

public interface IScriptMappingPipeline
{
    ScriptCompilationPlan Process(
        string json,
        string sourcePath,
        string templateCatalogPath,
        string mappingCatalogPath,
        string governedLibraryPath);
}
