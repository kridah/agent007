var builder = DistributedApplication.CreateBuilder(args);

var webapp = builder.AddProject<Projects.src>("webapp");

builder.Build().Run();
