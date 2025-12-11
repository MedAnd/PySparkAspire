using System.IO;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Create a local folder to persist your notebooks (relative to AppHost project)
var notebooksDir = Path.Combine(Directory.GetCurrentDirectory(), "pyspark-notebooks");

// Add Azurite (Azure Storage Emulator) as a container
var azureStorage = builder.AddContainer("azure-storage", "mcr.microsoft.com/azure-storage/azurite:3.35.0")
    .WithHttpEndpoint(port: 10000, targetPort: 10000, name: "blob", isProxied: false)
    .WithHttpEndpoint(port: 10001, targetPort: 10001, name: "queue", isProxied: false)
    .WithHttpEndpoint(port: 10002, targetPort: 10002, name: "table", isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent);

// Add PySpark as a container
var pySpark = builder
    //.AddContainer("pyspark-notebook", "quay.io/jupyter/pyspark-notebook:latest")
    .AddContainer("pyspark-notebook", "pyspark-azure-notebook")
    .WithHttpEndpoint(port: 8888, targetPort: 8888, name: "jupyter", isProxied: false)
    .WithHttpEndpoint(port: 4040, targetPort: 4040, name: "sparkui", isProxied: false)
    .WithBindMount(source: notebooksDir, target: "/home/jovyan/work", isReadOnly: false)    
    .WithArgs("start-notebook.sh", "--NotebookApp.token=''", "--NotebookApp.password=''")
    .WithUrlForEndpoint("jupyter", url =>
    {
        url.DisplayText = "Jupyter Lab";
    })
    .WithUrlForEndpoint("sparkui", url =>
    {
        url.DisplayText = "Spark Server";
    })
    .WaitFor(azureStorage)
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();
