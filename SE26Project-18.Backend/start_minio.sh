#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DATA_DIR="${MINIO_DATA_DIR:-$SCRIPT_DIR/minio/data}"

export MINIO_ROOT_USER="${MINIO_ROOT_USER:-minioadmin}"
export MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-minioadmin}"

mkdir -p "$DATA_DIR"
exec minio server "$DATA_DIR" --address ":9000" --console-address ":9001"
