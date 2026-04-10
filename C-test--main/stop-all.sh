#!/bin/zsh

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
RUN_DIR="$ROOT_DIR/.run"

API_PORT=5287

API_PID_FILE="$RUN_DIR/api.pid"

is_pid_running() {
  local pid="${1:-}"
  [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null
}

is_port_listening() {
  local port="$1"
  lsof -t -nP -iTCP:$port -sTCP:LISTEN >/dev/null 2>&1
}

wait_for_port_release() {
  local name="$1"
  local port="$2"

  for ((i = 1; i <= 10; i++)); do
    if ! is_port_listening "$port"; then
      echo "$name da nhả cong $port"
      return 0
    fi

    sleep 1
  done

  if is_port_listening "$port"; then
    echo "$name van con giu cong $port. Ban co the chay lai ./stop-all.sh hoac tu kill thu cong."
  fi
}

stop_pid_file_process() {
  local name="$1"
  local pid_file="$2"

  if [[ ! -f "$pid_file" ]]; then
    return 0
  fi

  local pid
  pid="$(<"$pid_file")"

  if is_pid_running "$pid"; then
    kill "$pid"

    for ((i = 1; i <= 10; i++)); do
      if ! is_pid_running "$pid"; then
        break
      fi

      sleep 1
    done

    if is_pid_running "$pid"; then
      echo "$name van chua dung han. Ban co the tu kill -9 $pid neu can."
    else
      echo "$name da dung (PID $pid)"
    fi
  fi

  rm -f "$pid_file"
}

stop_port_processes() {
  local name="$1"
  local port="$2"
  local pids

  pids="$(lsof -t -nP -iTCP:$port -sTCP:LISTEN 2>/dev/null || true)"

  if [[ -z "$pids" ]]; then
    return 0
  fi

  for pid in ${(f)pids}; do
    if is_pid_running "$pid"; then
      kill "$pid"
      echo "$name da gui lenh dung toi PID $pid"
    fi
  done

  wait_for_port_release "$name" "$port"
}

stop_pid_file_process "VKFoodAPI" "$API_PID_FILE"

stop_port_processes "VKFoodAPI" "$API_PORT"

echo "Da xu ly stop cho API."
