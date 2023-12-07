using Weather.AppHost.ApplicationModel;

namespace Weather.AppHost.Extensions;

public static class ResourceBuilderExtensions
{
    /// <summary>
    /// Injects reverse proxy information as environment variables from the project resource into the destination resource, using the source resource's name as cluster and
    /// route names.
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="source">The resource from which to extract service bindings.</param>
    /// <param name="matchPath">The path to match.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{TDestination}"/>.</returns>
    public static IResourceBuilder<TDestination> WithProxiedReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IResourceWithBindings> source, string matchPath)
        where TDestination : IResourceWithEnvironment
    {
        ApplyBinding(builder, source.Resource, matchPath);
        return builder;
    }

    private static void ApplyBinding<T>(this IResourceBuilder<T> builder, IResourceWithBindings resourceWithBindings, string matchPath, string? bindingName = null)
        where T : IResourceWithEnvironment
    {
        // When adding a proxied service reference we get to see whether there is a ProxiedServiceReferencesAnnotation
        // on the resource, if there is then it means we have already been here before and we can just
        // skip this and note the service binding that we want to apply to the environment in the future
        // in a single pass. There is one ServiceReferenceAnnotation per service binding source.
        var serviceReferenceAnnotation = builder.Resource.Annotations
            .OfType<ProxiedServiceReferenceAnnotation>()
            .Where(sra => sra.Resource == resourceWithBindings)
            .SingleOrDefault();

        if (serviceReferenceAnnotation == null)
        {
            serviceReferenceAnnotation = new ProxiedServiceReferenceAnnotation(resourceWithBindings, matchPath);
            builder.WithAnnotation(serviceReferenceAnnotation);

            var callback = CreateProxiedServiceReferenceEnvironmentPopulationCallback(serviceReferenceAnnotation);
            builder.WithEnvironment(callback);
        }

        // If no specific binding name is specified, go and add all the bindings.
        if (bindingName == null)
        {
            serviceReferenceAnnotation.UseAllBindings = true;
        }
        else
        {
            serviceReferenceAnnotation.BindingNames.Add(bindingName);
        }
    }

    private static Action<EnvironmentCallbackContext> CreateProxiedServiceReferenceEnvironmentPopulationCallback(ProxiedServiceReferenceAnnotation proxiedServiceReferencesAnnotation)
    {
        return (context) =>
        {
            var name = proxiedServiceReferencesAnnotation.Resource.Name;

            context.EnvironmentVariables[$"ReverseProxy__Routes__{name}Route__ClusterId"] = $"{name}Cluster";
            context.EnvironmentVariables[$"ReverseProxy__Routes__{name}Route__Match__Path"] = proxiedServiceReferencesAnnotation.MatchPath;
            context.EnvironmentVariables[$"ReverseProxy__Routes__{name}Route__Transforms__0__PathPattern"] = "/{remainder}";

            var allocatedEndPoints = proxiedServiceReferencesAnnotation.Resource.Annotations
                .OfType<AllocatedEndpointAnnotation>()
                .Where(a => proxiedServiceReferencesAnnotation.UseAllBindings || proxiedServiceReferencesAnnotation.BindingNames.Contains(a.Name));

            foreach (var allocatedEndPoint in allocatedEndPoints)
            {
                var bindingNameQualifiedUriStringKey = $"ReverseProxy__Clusters__{name}Cluster__Destinations__{name}__Address";
                context.EnvironmentVariables[bindingNameQualifiedUriStringKey] = allocatedEndPoint.UriString;
            }
        };
    }
}
