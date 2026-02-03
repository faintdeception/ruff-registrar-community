# AKS deployment (scaffold)

This repo uses Aspire for local orchestration. For production on AKS, the intended flow is:

1. Generate an Aspire deployment manifest
2. Build and push container images
3. Apply Kubernetes resources (Deployments/Services/Ingress/Secrets) that wire the app together

A GitHub Actions scaffold exists at [.github/workflows/deploy-aks.yml](../../.github/workflows/deploy-aks.yml). It currently:

- Generates the Aspire manifest (`dist/aspire/aspire-manifest.json`)
- Builds and pushes the API + frontend images to ACR (when secrets are provided)
- Leaves the final `kubectl apply` step as a TODO

## Current status

This workflow is **not production-ready** yet. It does **not** deploy to a cluster without additional Kubernetes manifests or a manifest-to-Kubernetes conversion step.

## Required GitHub secrets

- `AZURE_CREDENTIALS`: Output of `az ad sp create-for-rbac --sdk-auth ...` (JSON)
- `AKS_RESOURCE_GROUP`: Resource group of the AKS cluster
- `AKS_CLUSTER_NAME`: AKS cluster name
- `ACR_LOGIN_SERVER`: e.g. `myregistry.azurecr.io`

## Required follow-up work

- Define Kubernetes manifests in deploy/aks (or add a tool to convert the Aspire manifest to Kubernetes manifests).
- Wire a `kubectl apply` step using those manifests.
- Decide how secrets and config are injected at deploy time.

## Notes

- Keycloak production hardening should run it in non-dev mode (`start`, not `start-dev`) and back it with a persistent Postgres DB.
- The frontend uses a public SPA client for browser login; production hardening (PKCE/BFF, stricter CORS/redirects) is still recommended.
