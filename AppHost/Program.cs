var builder = DistributedApplication.CreateBuilder(args);

var ollamaApi = builder.AddProject<Projects.OllamaApi>("ollama-api");
var openAiApi = builder.AddProject<Projects.src>("webapp")
    .WithReference(ollamaApi);


builder.Build().Run();
