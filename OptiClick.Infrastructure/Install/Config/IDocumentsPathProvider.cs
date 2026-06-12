namespace OptiClick.Infrastructure.Install.Config;

public interface IDocumentsPathProvider
{
    IReadOnlyList<string> GetDocumentsCandidates();
}
