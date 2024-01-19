using Weather.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var apiservice = builder.AddProject<Projects.Weather_ApiService>("apiservice");

builder.AddProject<Projects.Weather_ReverseProxy>("reverseproxy")
    .WithProxiedReference(apiservice, "/weather", true);

builder.Build().Run();
