set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$(cd -- "${script_dir}/.." && pwd)"
project_file="${project_dir}/SE26Project-18.Api.csproj"

usage() {
  cat <<'EOF'
Usage: init-db.sh

Applies the API's committed EF Core migrations to an externally provisioned
MariaDB database. The DDL connection string must be supplied through:

  ConnectionStrings__Default

Example:

  export ConnectionStrings__Default='Server=localhost;Port=3306;Database=se26proj_18;User=se26proj_18_ddl;Password=replace-me;'
  bash ./SE26Project-18.Api/scripts/init-db.sh

The script does not create the database or users and does not seed business data.
EOF
}

if [[ ${1:-} == "--help" || ${1:-} == "-h" ]]; then
  usage
  exit 0
fi

if [[ $# -ne 0 ]]; then
  usage >&2
  exit 2
fi

if [[ -z ${ConnectionStrings__Default:-} ]]; then
  echo "Error: ConnectionStrings__Default must contain the DDL user's connection string." >&2
  exit 2
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: dotnet is not available in PATH." >&2
  exit 127
fi

if ! dotnet ef --version >/dev/null 2>&1; then
  echo "Error: dotnet-ef is not installed or not available in PATH." >&2
  exit 127
fi

echo "Checking that the EF model is represented by committed migrations..."
dotnet ef migrations has-pending-model-changes --project "${project_file}"

echo "Current migration status:"
dotnet ef migrations list --project "${project_file}"

echo "Applying database migrations..."
dotnet ef database update --project "${project_file}"

echo "Database initialization completed. No business data was seeded."
