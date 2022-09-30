# C# Sample Accelerator

A sample accelerator for C# consuming Services (Rabbitmq and Reddis).

It simply does the following:
* Generates some weather data
* Sends the data to a RMQ Queue
* Receives back the same data from RMQ
* Stashes the data in Redis

This application has two versions on different branches:
- Application that consumes Services via environment variables - See [rmq_redis_env_vars](https://github.com/Samze/csharp-weatherforecast/tree/rmq_redis_env_vars)
- Application that consumes Services via [Service Binding specification](https://github.com/servicebinding/spec) - See [rmq_redis](https://github.com/Samze/csharp-weatherforecast/tree/rmq_redis)

This sample is a modified version of the Weather Forecast RESTful API application made available from Microsoft.

## Running the app locally

To run the sample application:

```console
dotnet run
```

## Deploying to Kubernetes as a TAP workload

### Install Service Operators
1. Create a Services namespace
```console
kubectl create ns service-instances
```

1. Install the Redis Enterprise operator
```console
 kapp -y deploy --app redis-operator --file https://raw.githubusercontent.com/RedisLabs/redis-enterprise-k8s-docs/v6.2.10-45/bundle.yaml -n service-instances
```

1. Install the RabbitMQ cluster Operator
```console
 kapp -y deploy --app rmq-operator --file https://github.com/rabbitmq/cluster-operator/releases/latest/download/cluster-operator.yml -n service-instances
 ```

### Create Service instances

1. Create a Redis instance
```console
kubectl apply -f config/redis.yaml
 ```

1. Create a RabbitMQ instance
```console
kubectl apply -f config/rabbitmq.yaml
```

1. Wait for both service instances to be ready
```console
kubectl wait --for=condition=ReconcileSuccess RabbitmqCluster rmq-1 -n service-instances

kubectl wait --for=jsonpath='{.status.state}'=Running RedisEnterpriseCluster redis-1 -n service-instances
kubectl wait --for=jsonpath='{.status.status}'=active RedisEnterpriseDatabase redis-1-db -n service-instances
```

### Create Application

1. Create Service Metadata
```console
kubectl apply -f config/meta.yaml
```

#### Option 1: Using YAML
1. Create ResourceClaims
```console
kubectl apply -f config/claims.yaml
```

1. Create workload
```console
kubectl apply -f config/workload.yaml
```

#### Option 2: Using Tanzu CLI
1. Discover what service classes are available
```console
tanzu service class list
```

1. Discover what service instances are available in your namespace
```console
tanzu service claimable list --class rabbitmq-cluster
tanzu service claimable list --class redis-enterprise-cluster
```

1. Create two ResourceClaims for each available Service
```console
tanzu service claim create rmq-1-claim \
  --resource-name rmq-1 \
  --resource-namespace service-instances \
  --resource-kind RabbitmqCluster \
  --resource-api-version rabbitmq.com/v1beta1
```

```console
tanzu service claim create redis-1-claim \
  --resource-name redis-1-redis-secret \
  --resource-namespace service-instances \
  --resource-kind Secret \
  --resource-api-version v1
```

1. Create Workload
```console
tanzu apps workload create sample-app \
  --git-repo https://github.com/samze/csharp-weatherforecast \
  --git-branch rmq_redis \
  --type web \
  --label app.kubernetes.io/part-of=sample-app \
  --annotation autoscaling.knative.dev/minScale=1 \
  --service-ref="rmq=services.apps.tanzu.vmware.com/v1alpha1:ResourceClaim:rmq-1-claim" \
  --service-ref="redis=services.apps.tanzu.vmware.com/v1alpha1:ResourceClaim:redis-1-claim" \
  -y
```

> NOTE: The provided `config/workload.yaml` file uses the Git URL for this sample. When you want to modify the source, you must push the code to your own Git repository and then update the `spec.source.git` information in the `config/workload.yaml` file.

If you make modifications to the source, push these changes to your own Git repository.

## Accessing the app deployed to your cluster

Determine the URL to use for the accessing the app by running:

```
tanzu apps workload get sample-app
```

To access the deployed app use the URL shown under "Workload Knative Services" and append the endpoint `/weatherforecast` to that URL.

This depends on the TAP installation having DNS configured for the Knative ingress.
