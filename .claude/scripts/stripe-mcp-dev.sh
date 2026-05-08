#!/usr/bin/env bash
set -euo pipefail

# Reads the Stripe dev sandbox key from dotnet user-secrets and execs @stripe/mcp.
# Today this uses sk_test_* (full perms, test-mode only). Swap to a read-only
# rk_test_* restricted key by changing the secret name below.
SECRET_NAME="Parameters:stripe-api-key"

STRIPE_SECRET_KEY=$(dotnet user-secrets list --project application/AppHost/AppHost.csproj | sed -n "s/^${SECRET_NAME} = //p")

if [ -z "$STRIPE_SECRET_KEY" ]; then
  echo "Stripe MCP: secret '${SECRET_NAME}' not found in dotnet user-secrets." >&2
  exit 1
fi

export STRIPE_SECRET_KEY
exec npx -y @stripe/mcp
