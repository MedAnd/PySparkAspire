# PySpark Aspire

**Aspire + JupyterLab & PySpark**  

Combining the power and elegance of the Aspire framework with JupyterLab & PySpark for fast, no‑friction development and testing.

---

## 🚀 Project Goals

- **Combine Aspire + JupyterLab**: Provide a simple, quick, no‑friction local dev experience by combining Aspire with JupyterLab notebooks.
- **Pre‑loaded Azure libraries**: Ship a local environment with PySpark and the Azure SDK for Python available out of the box so you can prototype quickly.
- **Local Azure Blob integration**: Provide easy local Blob storage via the official Azurite emulator.
- **Local Cosmos DB integration**: Provide easy local Cosmos DB via the official Cosmos DB (Preview) emulator.
- **Local Kafka, Kafka Connect, Schema Registry** etc.

- **Deployment readiness**: While this project focuses on **local development**, the same notebooks can be adapted for deployment to **Azure Synapse** or **Microsoft Fabric** environments.

---

## 📋 Prerequisites

- Install **.NET 10 SDK** (required to build/run Aspire components).  
  👉 [Download .NET SDK](https://dotnet.microsoft.com/download)
- Install the latest **Aspire framework** as per the [Aspire documentation](https://aspire.dev/).
- **Docker** (for building and running the custom container).
- *(Optional)* **Azure Storage Explorer** — recommended for copying files into Azurite.  
  👉 [Download Storage Explorer](https://azure.microsoft.com/features/storage-explorer/)

---

## 📂 Files & Folders of Interest

- `data/hr.csv` — sample HR data used by Azurite-Sample.ipynb notebook.
- `pyspark-notebooks/` — project notebooks; this folder is mapped into JupyterLab for convenience.

---

## 🐳 Build the Docker Image

From the repository root, build the custom container used for the **JupyterLab + PySpark + Azure SDK** environment:

```bash
docker build -t pyspark-azure-notebook .
```

## ▶️ Run Aspire (Start Aspire, Azurite & JupyterLab)

This project leverages the **Aspire framework** to orchestrate your local development environment.  
Follow these steps to get everything running smoothly:

1. 📦 **Restore dependencies**
    ```bash
   dotnet restore
    ```

2. 📦 **Build the Aspire project**
    ```bash
   dotnet build
    ```

3. 📦 **Run Aspire**
    ```bash
   aspire run
    ```

### 🚀 What Happens When You Run Aspire

Running `aspire run` will:

- 🌐 Launch the **Aspire application** and open the **Aspire Dashboard** in your browser.  
- 📦 Start the local **Azurite & Cosmos DB emulators** automatically.  
- 📒 Spin up the **Kafka, Kafka Connect, Schema Registry containers**. 
- 📒 Spin up the **JupyterLab container**.  

---

### 📥 Load Sample Data into Azurite

After Aspire has started:

1. 🔗 Use **Azure Storage Explorer** (recommended) to connect to the local Azurite instance.  
2. 📂 Copy `data/hr.csv` into a container named **`data`**.  
3. ✅ Your notebooks can now access blobs directly from Azurite for testing and prototyping.  


## 📝 Notes & Recommendations

- 📒 **Project notebooks**: The `pyspark-notebooks` folder is bound to JupyterLab at `/home/jovyan/work` for convenience — edit locally and work in Jupyter seamlessly.
- 📂 **Data mounting**: The `data/` folder lets notebooks read `data/hr.csv` directly or access blobs through the Azurite endpoint.
- 🌐 **Azurite endpoint**: If you need the blob endpoint URL for notebooks, it will typically be:
`http://azure-storage:10000`

Configure your connection strings in notebooks to point to this endpoint when running against Azurite.

---

## ✅ Quick Checklist

- 🔧 [.NET 10 SDK installed]  
- 🏗️ [Aspire framework installed]  
- 🐳 [Build Docker image with `docker build -t pyspark-azure-notebook .`]  
- 📦 [Copy `data/hr.csv` into a `data` container using Azure Storage Explorer]  