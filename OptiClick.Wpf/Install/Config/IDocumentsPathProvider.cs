namespace OptiClick.Wpf.Install.Config;

public interface IDocumentsPathProvider
{
    IReadOnlyList<string> GetDocumentsCandidates();
}
