# Base Jupyter PySpark Notebook image
FROM quay.io/jupyter/pyspark-notebook:latest

USER root

# Spark home & jars folder
ENV SPARK_HOME=/usr/local/spark
ENV SPARK_JARS_DIR=$SPARK_HOME/jars

# Pin versions (WASB driver requires hadoop-azure + azure-storage)
ARG HADOOP_AZURE_VERSION=3.4.1
ARG AZURE_STORAGE_VERSION=8.6.6
ARG JETTY_VERSION=9.4.51.v20230217

# Maven Central base
ARG MVN_BASE=https://repo1.maven.org/maven2

# Download jars into Spark classpath
RUN set -eux; \
    curl -fSL ${MVN_BASE}/org/apache/hadoop/hadoop-azure/${HADOOP_AZURE_VERSION}/hadoop-azure-${HADOOP_AZURE_VERSION}.jar \
      -o ${SPARK_JARS_DIR}/hadoop-azure-${HADOOP_AZURE_VERSION}.jar && \
    curl -fSL ${MVN_BASE}/com/microsoft/azure/azure-storage/${AZURE_STORAGE_VERSION}/azure-storage-${AZURE_STORAGE_VERSION}.jar \
      -o ${SPARK_JARS_DIR}/azure-storage-${AZURE_STORAGE_VERSION}.jar && \
    curl -fSL ${MVN_BASE}/org/eclipse/jetty/jetty-util/${JETTY_VERSION}/jetty-util-${JETTY_VERSION}.jar \
      -o ${SPARK_JARS_DIR}/jetty-util-${JETTY_VERSION}.jar && \
    curl -fSL ${MVN_BASE}/org/eclipse/jetty/jetty-util-ajax/${JETTY_VERSION}/jetty-util-ajax-${JETTY_VERSION}.jar \
      -o ${SPARK_JARS_DIR}/jetty-util-ajax-${JETTY_VERSION}.jar

# Fix permissions for non-root user
RUN fix-permissions "${SPARK_HOME}/conf"

USER jovyan

# Install Azure SDK for Python packages
RUN pip install --no-cache-dir \
    azure-storage-blob==12.27.1 \
    azure-identity==1.25.1 \
    azure-core==1.36.0
