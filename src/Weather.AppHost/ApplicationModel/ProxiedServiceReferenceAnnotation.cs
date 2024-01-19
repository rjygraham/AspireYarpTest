using System.Collections.ObjectModel;

namespace Weather.AppHost.ApplicationModel;

internal class ProxiedServiceReferenceAnnotation(IResource resource, string pathPrefix, bool removePathPrefix = true) : IResourceAnnotation
{
    public IResource Resource { get; } = resource;
    public string PathPrefix { get; } = pathPrefix.StartsWith('/') ? pathPrefix : $"/{pathPrefix}";
    public bool RemovePathPrefix { get; } = removePathPrefix;
    public bool UseAllBindings { get; set; }
    public Collection<string> BindingNames { get; } = new();
}
