#!/bin/bash
exec 2>/dev/null
export LC_NUMERIC=C
input=$(cat)

if ! command -v jq &>/dev/null; then
  printf "⚠️  jq is required for the status line — install with: \033[1;33mwinget install jqlang.jq\033[0m (Windows) or \033[1;33mbrew install jq\033[0m (Mac)"
  exit 0
fi

# State directory for daily cost tracking
STATE_DIR="$HOME/.claude/statusline-state"
mkdir -p "$STATE_DIR"

# Model — strip "(1M context)" / "(200K context)" suffix
model=$(echo "$input" | jq -r '.model.display_name // "unknown"')
model=$(echo "$model" | sed 's/ ([^)]*context)$//; s/Claude //')

# CWD & transcript
cwd_full=$(echo "$input" | jq -r '.cwd // ""')
cwd=$(basename "$cwd_full")
transcript_path=$(echo "$input" | jq -r '.transcript_path // empty')

# Context window
used_pct=$(echo "$input" | jq -r '.context_window.used_percentage // empty')
ctx_window_size=$(echo "$input" | jq -r '.context_window.context_window_size // 0')
total_in=$(echo "$input" | jq -r '.context_window.total_input_tokens // 0')
total_out=$(echo "$input" | jq -r '.context_window.total_output_tokens // 0')

# Exact session cost & duration (from Claude Code, not local estimation)
session_cost=$(echo "$input" | jq -r '.cost.total_cost_usd // 0' | awk '{printf "%.2f", $1}')
session_secs=$(echo "$input" | jq -r '.cost.total_duration_ms // 0' | awk '{printf "%d", $1 / 1000}')
session_hrs=$(( session_secs / 3600 ))
session_mins=$(( (session_secs % 3600) / 60 ))
if [ "$session_hrs" -gt 0 ]; then
  session_dur="${session_hrs}h${session_mins}m"
else
  session_dur="${session_mins}m"
fi

now=$(date +%s)

# Clean up legacy state files from earlier delta tracker
find "$STATE_DIR" -name "daily_*" -mtime +2 -delete 2>/dev/null
find "$STATE_DIR" -name "prev_session_*" -mtime +2 -delete 2>/dev/null
find "$STATE_DIR" -name "session_start_*" -mtime +2 -delete 2>/dev/null

# Cumulative cached tokens — sum cache_read_input_tokens across the whole transcript
cached_total=0
if [ -n "$transcript_path" ] && [ -f "$transcript_path" ]; then
  cached_total=$(jq -r '.message.usage.cache_read_input_tokens // 0' "$transcript_path" 2>/dev/null | awk '{s+=$1} END {print int(s)}')
fi

# Rate limits
five_h=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')
five_h_resets=$(echo "$input" | jq -r '.rate_limits.five_hour.resets_at // empty')
seven_d=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty')
seven_d_resets=$(echo "$input" | jq -r '.rate_limits.seven_day.resets_at // empty')

# Output style
style=$(echo "$input" | jq -r '.output_style.name // empty')
[ "$style" = "default" ] && style=""

# Colors
RESET="\033[0m"
DIM="\033[2m"
BOLD="\033[1m"
CYAN="\033[36m"
GREEN="\033[32m"
YELLOW="\033[38;2;246;195;68m"
MAGENTA="\033[35m"
RED="\033[31m"
BRIGHT_WHITE="\033[97m"
GREY="\033[90m"

# Burn-rate-aware color for rate limits
rate_color() {
  local usage_pct=$1 window_secs=$2 remaining_secs=$3
  local elapsed_frac=$(echo "$window_secs $remaining_secs" | awk '{printf "%.4f", ($1 - $2) / $1}')
  local expected=$(echo "$elapsed_frac" | awk '{printf "%.1f", $1 * 100}')
  local ratio=$(echo "$usage_pct $expected" | awk '{if ($2 == 0) print 999; else printf "%.2f", $1 / $2}')
  local remaining_pct=$(echo "$usage_pct" | awk '{printf "%.0f", 100 - $1}')
  if [ "$remaining_pct" -le 10 ] 2>/dev/null; then
    echo "$RED"
  elif echo "$ratio" | awk '{exit ($1 >= 1.3) ? 0 : 1}'; then
    echo "$RED"
  elif echo "$ratio" | awk '{exit ($1 >= 1.0) ? 0 : 1}'; then
    echo "$YELLOW"
  else
    echo "$GREEN"
  fi
}

# Git info — repo name (main worktree) + branch + ahead/behind
repo_name=""
git_segment=""
worktree_top=""
if git -C "$cwd_full" rev-parse --is-inside-work-tree &>/dev/null 2>&1; then
  main_worktree=$(git -C "$cwd_full" worktree list --porcelain 2>/dev/null | awk '/^worktree / {print $2; exit}')
  [ -n "$main_worktree" ] && repo_name=$(basename "$main_worktree")
  worktree_top=$(git -C "$cwd_full" rev-parse --show-toplevel 2>/dev/null)
  branch=$(git -C "$cwd_full" rev-parse --abbrev-ref HEAD 2>/dev/null)
  [ "$branch" = "HEAD" ] && branch=$(git -C "$cwd_full" rev-parse --short HEAD 2>/dev/null)
  if [ -n "$branch" ]; then
    ahead=0
    behind=0
    remote_info=$(git -C "$cwd_full" rev-list --count --left-right "@{upstream}...HEAD" 2>/dev/null)
    if [ -n "$remote_info" ]; then
      behind=$(echo "$remote_info" | awk '{print $1}')
      ahead=$(echo "$remote_info" | awk '{print $2}')
    fi
    git_segment="$(printf "🌿 ${YELLOW}%s${RESET}" "$branch")"
    [ "$ahead" -gt 0 ] 2>/dev/null && git_segment="${git_segment}$(printf " ${GREEN}↑%s${RESET}" "$ahead")"
    [ "$behind" -gt 0 ] 2>/dev/null && git_segment="${git_segment}$(printf " ${RED}↓%s${RESET}" "$behind")"
  fi
fi
[ -z "$repo_name" ] && repo_name="$cwd"

# Format big numbers (k/M/B)
fmt_big() {
  echo "$1" | awk '{
    if ($1 >= 1000000000) printf "%.1fB", $1 / 1000000000;
    else if ($1 >= 1000000) printf "%.1fM", $1 / 1000000;
    else if ($1 >= 1000) printf "%.0fk", $1 / 1000;
    else printf "%d", $1
  }'
}

# --- Build segments ---
SEP="$(printf " ${DIM}|${RESET} ")"

seg_cwd="$(printf "📂 ${BOLD}${BRIGHT_WHITE}%s${RESET}" "$repo_name")"
port=""
[ -n "$worktree_top" ] && [ -f "$worktree_top/.workspace/port.txt" ] && port=$(tr -d '[:space:]' < "$worktree_top/.workspace/port.txt" | head -c 8)
seg_port=""
if [ -n "$port" ]; then
  aspire_url_file="$worktree_top/.workspace/aspire-dashboard-url.txt"
  if [ -f "$aspire_url_file" ]; then
    aspire_url=$(tr -d '[:space:]' < "$aspire_url_file")
    seg_port="$(printf "${CYAN}\033]8;;%s\007aspire\033]8;;\007${RESET}" "$aspire_url")"
  fi
fi
seg_git=""
[ -n "$git_segment" ] && seg_git="${git_segment}"
seg_model="$(printf "🤖 ${CYAN}%s${RESET}" "$model")"

# Context bar — green/yellow/red on absolute thresholds 600k/800k
seg_ctx=""
seg_ctx_compact=""
if [ -n "$used_pct" ]; then
  used_int=$(printf "%.0f" "$used_pct")
  if [ "$total_in" -ge 800000 ] 2>/dev/null; then
    ctx_color="$RED"
  elif [ "$total_in" -ge 600000 ] 2>/dev/null; then
    ctx_color="$YELLOW"
  else
    ctx_color="$GREY"
  fi

  bar_cells=16
  filled=$(echo "$used_int $bar_cells" | awk '{f = int($1 * $2 / 100 + 0.5); if (f > $2) f = $2; if (f < 0) f = 0; print f}')
  bar=""
  for ((i=0; i<filled; i++)); do bar="${bar}█"; done
  for ((i=filled; i<bar_cells; i++)); do bar="${bar}░"; done

  used_fmt=$(fmt_big "$total_in")
  window_fmt=$(fmt_big "$ctx_window_size")

  seg_ctx="$(printf "[${ctx_color}%s${RESET}] %s/%s (%d%%)" "$bar" "$used_fmt" "$window_fmt" "$used_int")"
  seg_ctx_compact="$(printf "[${ctx_color}%s${RESET}] %d%%" "$bar" "$used_int")"
fi

# Cached tokens segment (separate so it can be dropped progressively)
seg_cached=""
if [ "$cached_total" -gt 0 ] 2>/dev/null; then
  seg_cached="$(printf "Cached: ${DIM}%s${RESET}" "$(fmt_big "$cached_total")")"
fi

seg_session_cost="$(printf "💰 ${GREEN}\$%s${RESET} ${DIM}(%s)${RESET}" "$session_cost" "$session_dur")"

# Rate limits
seg_rl=""
rl=""
if [ -n "$five_h" ]; then
  five_h_int=$(printf '%.0f' "$five_h")
  five_h_color="$GREEN"
  diff_5h=0
  if [ -n "$five_h_resets" ]; then
    diff_5h=$(( five_h_resets - now ))
    [ "$diff_5h" -lt 0 ] && diff_5h=0
    five_h_color=$(rate_color "$five_h" 18000 "$diff_5h")
  fi
  rl="${rl}$(printf "5h ${five_h_color}%d%%${RESET}" "$five_h_int")"
  if [ "$diff_5h" -gt 0 ]; then
    hours_5h=$(( diff_5h / 3600 ))
    mins_5h=$(( (diff_5h % 3600) / 60 ))
    rl="${rl}$(printf " ${DIM}resets ${hours_5h}h${mins_5h}m${RESET}")"
  fi
fi
if [ -n "$seven_d" ]; then
  [ -n "$rl" ] && rl="${rl}$(printf " ${DIM}|${RESET} ")"
  seven_d_int=$(printf '%.0f' "$seven_d")
  seven_d_color="$GREEN"
  diff_7d=0
  if [ -n "$seven_d_resets" ]; then
    diff_7d=$(( seven_d_resets - now ))
    [ "$diff_7d" -lt 0 ] && diff_7d=0
    seven_d_color=$(rate_color "$seven_d" 604800 "$diff_7d")
  fi
  rl="${rl}$(printf "7d ${seven_d_color}%d%%${RESET}" "$seven_d_int")"
  if [ "$diff_7d" -gt 0 ]; then
    days=$(( diff_7d / 86400 ))
    hours=$(( (diff_7d % 86400) / 3600 ))
    if [ "$days" -gt 0 ]; then
      reset_str="${days}d${hours}h"
    else
      mins=$(( (diff_7d % 3600) / 60 ))
      reset_str="${hours}h${mins}m"
    fi
    rl="${rl}$(printf " ${DIM}resets ${reset_str}${RESET}")"
  fi
fi
[ -n "$rl" ] && seg_rl="$(printf "⚡ %b" "$rl")"

seg_style=""
[ -n "$style" ] && seg_style="$(printf "✎ %s" "$style")"

# --- Terminal width detection ---
get_display_width() {
  local stripped
  stripped=$(printf "%b" "$1" | sed 's/\x1b\[[0-9;]*m//g' 2>/dev/null || printf "%b" "$1" | sed $'s/\033\\[[0-9;]*m//g')
  printf "%s" "$stripped" | awk '{print length}' | head -1
}

get_term_width() {
  local pid=$$
  for _ in 1 2 3 4 5 6 7 8 9 10; do
    local tty_device=$(ps -o tty= -p "$pid" 2>/dev/null | tr -d ' ')
    if [ -n "$tty_device" ] && [ "$tty_device" != "?" ] && [ "$tty_device" != "??" ]; then
      local size=$(stty -F "/dev/$tty_device" size 2>/dev/null || stty -f "/dev/$tty_device" size 2>/dev/null)
      if [ -n "$size" ]; then
        echo "$size" | awk '{print $2}'
        return
      fi
    fi
    pid=$(ps -o ppid= -p "$pid" 2>/dev/null | tr -d ' ')
    [ -z "$pid" ] && break
  done
  echo "200"
}

term_width=$(get_term_width)
[[ "$term_width" =~ ^[0-9]+$ ]] || term_width=200
term_width_full=$(( term_width - 15 ))
term_width_compact=$term_width

# --- Assemble with progressive removal ---
# Drop order (least important first):
#   1. style, 2. rl_resets, 3. cached, 4. daily cost,
#   5. session cost, 6. folder, 7. git, 8. ctx full (fall back to compact bar+%)
# Always keep: model, context (at least compact form)
assemble() {
  local out=""
  local show_cwd=$1 show_git=$2 show_ctx_full=$3
  local show_session_cost=$4 show_daily_cost=$5 show_cached=$6
  local show_rl_resets=$7 show_style=$8
  local show_rl=${9:-1} show_model=${10:-1}

  [ "$show_cwd" = "1" ] && out="${out}${seg_cwd}"
  if [ -n "$seg_port" ]; then
    [ -n "$out" ] && out="${out} "
    out="${out}${seg_port}"
  fi
  if [ "$show_git" = "1" ] && [ -n "$seg_git" ]; then
    [ -n "$out" ] && out="${out} ${DIM}|${RESET} "
    out="${out}${seg_git}"
  fi
  [ -n "$out" ] && out="${out}${SEP}"

  [ "$show_model" = "1" ] && out="${out}${seg_model}"

  if [ -n "$seg_ctx" ]; then
    if [ "$show_ctx_full" = "1" ]; then
      out="${out}${SEP}${seg_ctx}"
    else
      out="${out}${SEP}${seg_ctx_compact}"
    fi
  fi

  if [ "$show_cached" = "1" ] && [ -n "$seg_cached" ]; then
    out="${out}${SEP}${seg_cached}"
  fi

  if [ "$show_rl" = "1" ] && [ -n "$seg_rl" ]; then
    if [ "$show_rl_resets" = "1" ]; then
      out="${out}${SEP}${seg_rl}"
    else
      local rl_short=""
      [ -n "$five_h" ] && rl_short="$(printf "5h ${five_h_color}%d%%${RESET}" "$five_h_int")"
      if [ -n "$seven_d" ]; then
        [ -n "$rl_short" ] && rl_short="${rl_short}$(printf " ${DIM}|${RESET} ")"
        rl_short="${rl_short}$(printf "7d ${seven_d_color}%d%%${RESET}" "$seven_d_int")"
      fi
      out="${out}${SEP}$(printf "⚡ %b" "$rl_short")"
    fi
  fi

  [ "$show_session_cost" = "1" ] && out="${out}${SEP}${seg_session_cost}"
  [ "$show_style" = "1" ] && [ -n "$seg_style" ] && out="${out}${SEP}${seg_style}"

  out=$(printf "%b" "$out" | sed 's/^ *| *//')
  printf "%b" "$out"
}

# Configs — progressive drop (most → least)
# Args: cwd git ctx_full session daily cached rl_resets style rl model
configs=(
  "1 1 1 1 1 1 1 1 1 1"
  "1 1 1 1 1 1 1 0 1 1"
  "1 1 1 1 1 1 0 0 1 1"
  "1 1 1 1 1 0 0 0 1 1"
  "1 1 1 1 0 0 0 0 1 1"
  "1 1 1 0 0 0 0 0 1 1"
  "0 1 1 0 0 0 0 0 1 1"
  "0 0 1 0 0 0 0 0 1 1"
  "0 0 0 0 0 0 0 0 1 1"
  "0 0 0 0 0 0 0 0 0 1"
)

for config in "${configs[@]}"; do
  result=$(assemble $config)
  width=$(get_display_width "$result")
  if [ "$width" -le "$term_width_full" ]; then
    printf "%b" "$result"
    exit 0
  fi
done

printf "%b" "$(assemble 0 0 0 0 0 0 0 0 0 1)"
