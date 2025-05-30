var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
                    .WithOpenWebUI()
                    .AddModel("llama3.2");

var webapp = builder.AddProject<Projects.src>("webapp")
.WithReference(ollama);

builder.Build().Run();
