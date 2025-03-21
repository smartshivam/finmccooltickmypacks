#!/bin/bash
set -e

echo "Applying database migrations..."
dotnet ef database update --no-build

echo "Starting application..."
exec dotnet MyToursApi.dll
