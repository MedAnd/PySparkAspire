using System.IO;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Create a local folder to persist your notebooks (relative to AppHost project)
var notebooksDir = Path.Combine(Directory.GetCurrentDirectory(), "pyspark-notebooks");

// Add Azure Cosmos DB Emulator as a container
var azureCosmosDB = builder.AddAzureCosmosDB("azure-cosmos-db")
    .RunAsPreviewEmulator(emulator =>
    {
        emulator.WithDataExplorer();
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

// Add Azurite (Azure Storage Emulator) as a container
var azureStorage = builder.AddContainer("azure-storage", "mcr.microsoft.com/azure-storage/azurite:3.35.0")
    .WithHttpEndpoint(port: 10000, targetPort: 10000, name: "blob", isProxied: false)
    .WithHttpEndpoint(port: 10001, targetPort: 10001, name: "queue", isProxied: false)
    .WithHttpEndpoint(port: 10002, targetPort: 10002, name: "table", isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent);

// Add Kafka Local container
var kafka = builder.AddContainer("kafka", "confluentinc/confluent-local", "7.8.1")
    .WithEndpoint(port: 9092, targetPort: 9092, name: "CLIENT", isProxied: false)
    .WithEndpoint(port: 9093, targetPort: 9093, name: "INTERNAL", isProxied: false)
    .WithEndpoint(port: 29093, targetPort: 29093, name: "CONTROLLER", isProxied: false)
    .WithEndpoint(port: 19092, targetPort: 19092, name: "EXTERNAL", isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["KAFKA_LISTENERS"] = "INTERNAL://0.0.0.0:9093,CLIENT://0.0.0.0:9092,CONTROLLER://0.0.0.0:29093,EXTERNAL://0.0.0.0:19092";
        context.EnvironmentVariables["KAFKA_ADVERTISED_LISTENERS"] = "INTERNAL://kafka:9093,CLIENT://kafka:9092,EXTERNAL://localhost:19092";
        context.EnvironmentVariables["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "INTERNAL:PLAINTEXT,CLIENT:PLAINTEXT,CONTROLLER:PLAINTEXT,EXTERNAL:PLAINTEXT";
        context.EnvironmentVariables["KAFKA_INTER_BROKER_LISTENER_NAME"] = "INTERNAL";
        context.EnvironmentVariables["KAFKA_CONTROLLER_LISTENER_NAMES"] = "CONTROLLER";
        context.EnvironmentVariables["KAFKA_CONTROLLER_QUORUM_VOTERS"] = "1@kafka:29093";
        context.EnvironmentVariables["KAFKA_NODE_ID"] = "1";
        context.EnvironmentVariables["KAFKA_PROCESS_ROLES"] = "broker,controller";
        context.EnvironmentVariables["KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS"] = "0";
        context.EnvironmentVariables["KAFKA_DEFAULT_REPLICATION_FACTOR"] = "1";
        context.EnvironmentVariables["KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"] = "1";
        context.EnvironmentVariables["KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR"] = "1";
        context.EnvironmentVariables["KAFKA_TRANSACTION_STATE_LOG_MIN_ISR"] = "1";
        context.EnvironmentVariables["KAFKA_LOG_FLUSH_INTERVAL_MESSAGES"] = "1";
        context.EnvironmentVariables["KAFKA_TLS_ENABLED"] = "false";
    });

// Add Confluent Platform Schema Registry container
var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry", "7.8.1")
    .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "http", isProxied: false)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["SCHEMA_REGISTRY_HOST_NAME"] = "schema-registry";
        context.EnvironmentVariables["SCHEMA_REGISTRY_LISTENERS"] = "http://0.0.0.0:8081";
        // As Aspire adds all containers to the same docker network by default, we can use kafka DNS name here
        context.EnvironmentVariables["SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS"] = "PLAINTEXT://kafka:9093";
    })
    .WithLifetime(ContainerLifetime.Persistent)
    .WithParentRelationship(kafka)
    .WaitFor(kafka);

// Add Kafka Connect container
var kafkaConnect = builder.AddContainer("kafka-connect", "confluentinc/cp-kafka-connect:7.8.1")
    .WithHttpEndpoint(port: 8083, targetPort: 8083, name: "http", isProxied: false)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["CONNECT_TLS_ENABLED"] = "false";
        context.EnvironmentVariables["CONNECT_PORT_BINDING"] = "8083:8083";
        context.EnvironmentVariables["CONNECT_BOOTSTRAP_SERVERS"] = "kafka:9093";
        context.EnvironmentVariables["CONNECT_REST_ADVERTISED_HOST_NAME"] = "kafka-connect";
        context.EnvironmentVariables["CONNECT_REST_PORT"] = "8083";
        context.EnvironmentVariables["CONNECT_GROUP_ID"] = "kafka-connect-group";
        context.EnvironmentVariables["CONNECT_CONFIG_STORAGE_TOPIC"] = "kafka-connect-configs";
        context.EnvironmentVariables["CONNECT_CONFIG_STORAGE_REPLICATION_FACTOR"] = "1";
        context.EnvironmentVariables["CONNECT_OFFSET_FLUSH_INTERVAL_MS"] = "10000";
        context.EnvironmentVariables["CONNECT_OFFSET_STORAGE_TOPIC"] = "kafka-connect-offsets";
        context.EnvironmentVariables["CONNECT_OFFSET_STORAGE_REPLICATION_FACTOR"] = "1";
        context.EnvironmentVariables["CONNECT_STATUS_STORAGE_TOPIC"] = "kafka-connect-status";
        context.EnvironmentVariables["CONNECT_STATUS_STORAGE_REPLICATION_FACTOR"] = "1";
        context.EnvironmentVariables["CONNECT_KEY_CONVERTER"] = "org.apache.kafka.connect.storage.StringConverter";
        context.EnvironmentVariables["CONNECT_VALUE_CONVERTER"] = "io.confluent.connect.avro.AvroConverter";
        context.EnvironmentVariables["CONNECT_VALUE_CONVERTER_SCHEMA_REGISTRY_URL"] = "http://schema-registry:8081";
        context.EnvironmentVariables["CONNECT_INTERNAL_KEY_CONVERTER"] = "org.apache.kafka.connect.json.JsonConverter";
        context.EnvironmentVariables["CONNECT_INTERNAL_VALUE_CONVERTER"] = "org.apache.kafka.connect.json.JsonConverter";
    })
    .WithLifetime(ContainerLifetime.Persistent)
    .WithParentRelationship(kafka)
    .WaitFor(kafka);

// Add Redpanda Console container
var redpandaConsole = builder.AddContainer("redpanda-console", "docker.redpanda.com/redpandadata/console:v2.8.7")
    .WithHttpEndpoint(port: 8050, targetPort: 8050, name: "http")
    .WithBindMount("./config/redpanda-console-config.yaml", "/etc/redpanda/console.yaml", isReadOnly: true)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["CONFIG_FILEPATH"] = "/etc/redpanda/console.yaml";  
        context.EnvironmentVariables["CONSOLE_TELEMETRY_ENABLED"] = "false";

        Thread.Sleep(TimeSpan.FromSeconds(25)); // Wait for the container to be ready
    })
    .WithParentRelationship(kafka)
    .WaitFor(kafka)
    .WaitFor(schemaRegistry);

// Add PySpark as a container
var pySpark = builder
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
    .WaitFor(kafka)
    .WaitFor(azureCosmosDB)
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();
