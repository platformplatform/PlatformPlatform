#!/bin/bash

ENVIRONMENT="staging"
LOCATION="WestEurope"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy-environment.sh
