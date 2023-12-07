using System.Collections.ObjectModel;

namespace Weather.AppHost.ApplicationModel;

internal class ProxiedServiceReferenceAnnotation(IResource resource, string matchPath, string? pathTransformPattern = null) : IResourceAnnotation
{
    public IResource Resource { get; } = resource;
    public string MatchPath { get; } = matchPath;
    public string? PathTransformPattern { get; } = pathTransformPattern;
    public bool UseAllBindings { get; set; }
    public Collection<string> BindingNames { get; } = new();
}
