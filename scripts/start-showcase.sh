#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG_FILE="$ROOT_DIR/.env.showcase"
cd "$ROOT_DIR"

RESET=false
if [[ "${1:-}" == "--reset" ]]; then
  RESET=true
elif [[ -n "${1:-}" ]]; then
  printf 'Usage: %s [--reset]\n' "$0" >&2
  exit 2
fi

command -v docker >/dev/null 2>&1 || { echo "Docker Desktop is required." >&2; exit 1; }
docker compose version >/dev/null 2>&1 || { echo "Docker Compose is required." >&2; exit 1; }

if [[ -e "$CONFIG_FILE" || -L "$CONFIG_FILE" ]]; then
  if [[ -L "$CONFIG_FILE" || ! -f "$CONFIG_FILE" || ! -O "$CONFIG_FILE" ]]; then
    echo ".env.showcase must be a regular file owned by the current user. Remove it and try again." >&2
    exit 1
  fi
  chmod 600 "$CONFIG_FILE"
fi

read_secret() {
  local prompt="$1" first second
  while true; do
    read -r -s -p "$prompt: " first
    printf '\n'
    read -r -s -p "Confirm password: " second
    printf '\n'
    if [[ "$first" != "$second" ]]; then
      echo "Passwords do not match. Try again." >&2
      continue
    fi
    if (( ${#first} < 12 )) || [[ ! "$first" =~ [A-Z] ]] || [[ ! "$first" =~ [a-z] ]] || [[ ! "$first" =~ [0-9] ]] || [[ ! "$first" =~ [@%_+=:,.-] ]]; then
      echo "Use 12+ characters with uppercase, lowercase, a number and one of @%_+=:,.-" >&2
      continue
    fi
    if [[ ! "$first" =~ ^[A-Za-z0-9@%_+=:,.-]+$ ]]; then
      echo "For portable Docker configuration, use only letters, numbers and @%_+=:,.-" >&2
      continue
    fi
    REPLY="$first"
    return
  done
}

if [[ "$RESET" == true && -f "$CONFIG_FILE" ]]; then
  echo "Resetting containers and PostgreSQL data..."
  docker compose --env-file "$CONFIG_FILE" down --volumes --remove-orphans
  rm -f "$CONFIG_FILE"
elif [[ "$RESET" == true ]]; then
  echo "No existing showcase configuration was found; creating a fresh stack."
fi

if [[ ! -f "$CONFIG_FILE" ]]; then
  read -r -p "Administrator email [admin@opsdesk.local]: " ADMIN_IDENTIFIER
  ADMIN_IDENTIFIER="${ADMIN_IDENTIFIER:-admin@opsdesk.local}"
  if [[ ! "$ADMIN_IDENTIFIER" =~ ^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]]; then
    echo "Enter a valid administrator email." >&2
    exit 2
  fi

  read_secret "PostgreSQL password"
  POSTGRES_PASSWORD="$REPLY"
  read_secret "Administrator login password"
  ADMIN_PASSWORD="$REPLY"
  unset REPLY

  if command -v openssl >/dev/null 2>&1; then
    JWT_SIGNING_KEY="$(openssl rand -base64 48 | tr -d '\n')"
  else
    JWT_SIGNING_KEY="$(python3 -c 'import secrets; print(secrets.token_urlsafe(48))')"
  fi

  umask 077
  TEMP_CONFIG="$(mktemp "${CONFIG_FILE}.tmp.XXXXXX")"
  trap 'rm -f "${TEMP_CONFIG:-}"' EXIT
  {
    printf 'POSTGRES_PASSWORD=%s\n' "$POSTGRES_PASSWORD"
    printf 'ADMIN_IDENTIFIER=%s\n' "$ADMIN_IDENTIFIER"
    printf 'ADMIN_PASSWORD=%s\n' "$ADMIN_PASSWORD"
    printf 'JWT_SIGNING_KEY=%s\n' "$JWT_SIGNING_KEY"
    printf 'JWT_ISSUER=internal-operations-api\n'
    printf 'JWT_AUDIENCE=internal-operations-web\n'
  } > "$TEMP_CONFIG"
  mv "$TEMP_CONFIG" "$CONFIG_FILE"
  TEMP_CONFIG=""
  trap - EXIT
  chmod 600 "$CONFIG_FILE"
  unset POSTGRES_PASSWORD ADMIN_PASSWORD JWT_SIGNING_KEY
  echo "Created private local configuration: .env.showcase"
else
  ADMIN_IDENTIFIER=""
  while IFS='=' read -r key value; do
    if [[ "$key" == "ADMIN_IDENTIFIER" ]]; then
      ADMIN_IDENTIFIER="$value"
      break
    fi
  done < "$CONFIG_FILE"
  if [[ -z "$ADMIN_IDENTIFIER" ]]; then
    echo "Invalid .env.showcase. Run with --reset to recreate it." >&2
    exit 1
  fi
  echo "Using the existing private .env.showcase configuration."
fi

API_PORT="${API_PORT:-8080}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"

echo "Building and starting PostgreSQL, migrations, .NET API and Next.js..."
docker compose --env-file "$CONFIG_FILE" up --build --detach --remove-orphans

echo "Waiting for the API to become ready..."
ready=false
for _ in {1..60}; do
  if curl --silent --fail "http://localhost:${API_PORT}/health/ready" >/dev/null 2>&1; then
    ready=true
    break
  fi
  sleep 2
done

if [[ "$ready" != true ]]; then
  echo "The API did not become ready. Recent logs:" >&2
  docker compose --env-file "$CONFIG_FILE" logs --tail=100 api migrator database >&2
  exit 1
fi

echo "Waiting for the frontend..."
frontend_ready=false
for _ in {1..30}; do
  if curl --silent --fail "http://localhost:${FRONTEND_PORT}" >/dev/null 2>&1; then
    frontend_ready=true
    break
  fi
  sleep 2
done
if [[ "$frontend_ready" != true ]]; then
  echo "The frontend did not become ready. Recent logs:" >&2
  docker compose --env-file "$CONFIG_FILE" logs --tail=100 frontend >&2
  exit 1
fi

printf '\nOpsDesk is ready.\n'
printf '  Frontend: http://localhost:%s\n' "$FRONTEND_PORT"
printf '  API health: http://localhost:%s/health/ready\n' "$API_PORT"
printf '  Login: %s\n' "$ADMIN_IDENTIFIER"
printf '\nCredentials are stored only in the ignored, mode-600 .env.showcase file on this machine.\n'
printf 'Stop:    docker compose --env-file .env.showcase stop\n'
printf 'Resume:  docker compose --env-file .env.showcase start\n'
printf 'Reset:   ./scripts/start-showcase.sh --reset\n'
