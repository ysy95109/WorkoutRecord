var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MosqApp1_ApiService>("apiservice");

builder.AddProject<Projects.MosqApp1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
