# Azure Container Apps deployment (side-by-side)

This repo can deploy to **Azure Container Apps (ACA)** via `aspire deploy`.

The GitHub Actions workflow is: [.github/workflows/deploy-aca.yml](../../.github/workflows/deploy-aca.yml)

## How it works

- The AppHost supports an opt-in ACA mode via the env var `DEPLOYMENT_PLATFORM=aca`.
- In ACA mode (and only during publish/deploy), the AppHost publishes resources as Azure Container Apps.

## Current status

This workflow is **not production-ready** yet. It assumes the AppHost’s ACA publish/deploy wiring is complete and correct (container builds, endpoints, secrets, and public URLs). If that wiring is incomplete, the workflow will fail or deploy partially.

## Required GitHub secrets

- `AZURE_CREDENTIALS`: JSON for `azure/login@v2` (service principal). For example:
  - `az ad sp create-for-rbac --name <name> --role contributor --scopes /subscriptions/<subId> --sdk-auth`

## Default deploy parameters

In ACA mode, the AppHost defines defaults:

- `location`: `eastus`
- `environment`: `aca`
- `resourceGroupName`: `studentregistrar-aca`

Override these defaults by changing the parameter defaults in the AppHost, or (if supported by your Aspire CLI version) passing deploy parameters via the CLI.

## Required follow-up work

- Ensure the AppHost’s ACA publish/deploy wiring builds the frontend as a container and exposes the correct public endpoints.
- Verify required secrets and parameters are set for deploy (Keycloak admin password, client secret, Postgres password, hostnames).
- Decide on resource group naming and lifecycle (dev vs. prod).

## Notes / limitations

- This is intended for side-by-side evaluation with AKS.
- Keycloak is made externally reachable in ACA mode so the browser frontend can reach it.
- The current frontend auth flow uses a browser-exposed Keycloak client secret, which is not production-safe; hardening that is a separate task.
