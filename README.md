# AspireYarpTest

This repo contains a POC of leveraging YARP as a reverse proxy for Aspire hosted projects. This is useful for scenarios in which there are multiple microservices under development and you'd like them to appear a single API surface and/or you need to expose the APIs via a tool like DevTunnels or ngrok so they can be consumed by a mobile app under development.

> NOTE: This POC takes a very naive approach and routes incoming requests to the proxied APIs using the request path. YARP supports many other routing options which goes beyond this naive implementation.

### Implementation

The logic for this POC resides in the `ResourceBuilderExtensions` within the Aspire `Weather.AppHost` project. Since YARP can read it's configuration from the .NET Configuration system, I've taken a path of least resistance and mapped the configuration from Aspire to environment variables which is then read by YARP. This can be seen in the `CreateProxiedServiceReferenceEnvironmentPopulationCallback` method:

```csharp
private static Action<EnvironmentCallbackContext> CreateProxiedServiceReferenceEnvironmentPopulationCallback(ProxiedServiceReferenceAnnotation proxiedServiceReferencesAnnotation)
{
    return (context) =>
    {
        var name = proxiedServiceReferencesAnnotation.Resource.Name;

        context.EnvironmentVariables[$"ReverseProxy__Routes__{name}Route__ClusterId"] = $"{name}Cluster";
        context.EnvironmentVariables[$"ReverseProxy__Routes__{name}Route__Match__Path"] = AppendRemainder(proxiedServiceReferencesAnnotation.PathPrefix);

        if (proxiedServiceReferencesAnnotation.RemovePathPrefix)
        {
            context.EnvironmentVariables[$"ReverseProxy__Routes__{name}Route__Transforms__0__PathRemovePrefix"] = proxiedServiceReferencesAnnotation.PathPrefix;
        }

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
```

### Usage

To wire up a project to YARP, simply invoke the `WithProxiedReference` extension method on the YARP project passing in a reference to the API project and the path to be matched which routes traffic to the API. The boolean parameter instructs the YARP project to keep or remove the path prefix in the path that is forwarded to the API project. In most instances you'll want to set this value to `true` in order to remove the path prefix.

```csharp
using Weather.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var apiservice = builder.AddProject<Projects.Weather_ApiService>("apiservice");

builder.AddProject<Projects.Weather_ReverseProxy>("reverseproxy")
    .WithProxiedReference(apiservice, "/weather", true);

builder.Build().Run();
```
