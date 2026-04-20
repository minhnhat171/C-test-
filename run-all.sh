#!/bin/zsh

set -euo pipefail
unsetopt BG_NICE 2>/dev/null || true

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
RUN_DIR="$ROOT_DIR/.run"
API_DIR="$ROOT_DIR/VKFoodAPI"
WEB_DIR="$ROOT_DIR/CTest.WebAdmin"

API_PORT=5287
WEB_PORT=5088

mkdir -p "$RUN_DIR"

API_PID_FILE="$RUN_DIR/api.pid"
API_LOG_FILE="$RUN_DIR/api.log"
WEB_PID_FILE="$RUN_DIR/webadmin.pid"
WEB_LOG_FILE="$RUN_DIR/webadmin.log"

is_pid_running() {
  local pid="${1:-}"
  [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null
}

is_port_listening() {
  local port="$1"
  lsof -t -nP -iTCP:$port -sTCP:LISTEN >/dev/null 2>&1
}

listener_summary() {
  local port="$1"
  lsof -nP -iTCP:$port -sTCP:LISTEN 2>/dev/null | tail -n +2
}

start_service() {
  local name="$1"
  local workdir="$2"
  local port="$3"
  local pid_file="$4"
  local log_file="$5"

  if [[ -f "$pid_file" ]]; then
    local existing_pid
    existing_pid="$(<"$pid_file")"
    if is_pid_running "$existing_pid"; then
      echo "$name da dang chay (PID $existing_pid)"
      return 0
    fi

    rm -f "$pid_file" 2>/dev/null || true
  fi

  if is_port_listening "$port"; then
    echo "$name da listen san o http://localhost:$port"
    listener_summary "$port"
    return 0
  fi

  (
    cd "$workdir"
    exec dotnet run --urls "http://localhost:$port"
  ) >"$log_file" 2>&1 &!

  local pid=$!
  echo "$pid" >"$pid_file"

  for ((i = 1; i <= 20; i++)); do
    if ! is_pid_running "$pid"; then
      echo "$name khoi dong that bai. Xem log: $log_file"
      rm -f "$pid_file"
      return 1
    fi

    if is_port_listening "$port"; then
      echo "$name dang chay tai http://localhost:$port (PID $pid)"
      echo "Log: $log_file"
      return 0
    fi

    sleep 1
  done

  echo "$name dang khoi dong nen chua mo cong kip. Theo doi log: $log_file"
}

start_service "VKFoodAPI" "$API_DIR" "$API_PORT" "$API_PID_FILE" "$API_LOG_FILE"
start_service "CTest.WebAdmin" "$WEB_DIR" "$WEB_PORT" "$WEB_PID_FILE" "$WEB_LOG_FILE"

echo
echo "API dang san sang tai: http://localhost:$API_PORT/api/pois"
echo "WebAdmin dang san sang tai: http://localhost:$WEB_PORT"
