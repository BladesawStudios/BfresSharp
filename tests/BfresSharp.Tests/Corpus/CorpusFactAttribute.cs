namespace BfresSharp.Tests.Corpus;

/// <summary>A fact that runs only when BFRES_CORPUS names at least one existing model folder.</summary>
/// <remarks>Without the corpus the fact reports as skipped, so a checkout with no game files still passes.</remarks>
public sealed class CorpusFactAttribute : FactAttribute
{
    public CorpusFactAttribute()
    {
        if (GameFiles.Folders.Count == 0) Skip = $"Set {GameFiles.Env} to model folders separated by semicolons to run the corpus tests.";
    }
}
