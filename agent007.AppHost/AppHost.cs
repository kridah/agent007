var builder = DistributedApplication.CreateBuilder(args);

var agent = builder.AddProject<Projects.Agent007>("agent007");
var sqllite = builder.AddSqlite("chat");
var ollama = builder.AddOllama("ollama");
var phi35 = ollama.AddModel("phi3.5");

builder.Build().Run();

