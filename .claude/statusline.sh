#!/bin/bash
exec 2>/dev/null
export LC_NUMERIC=C
input=$(cat)

# State directory for cost tracking
STATE_DIR="$HOME/.claude/statusline-state"
mkdir -p "$STATE_DIR"

# Model
model=$(echo "$input" | jq -r '.model.display_name // "unknown"')
model_id=$(echo "$input" | jq -r '.model.id // "unknown"')

# Shorten model display: "Opus 4.6 (1M context)" -> "Opus 4.6 (1M)"
model=$(echo "$model" | sed 's/ context)/)/; s/Claude //')

# CWD — show only the current folder name
cwd_full=$(echo "$input" | jq -r '.cwd // ""')
cwd=$(basename "$cwd_full")

# Context window usage
used_pct=$(echo "$input" | jq -r '.context_window.used_percentage // empty')
ctx_window_size=$(echo "$input" | jq -r '.context_window.context_window_size // 0')

# Session token totals
total_in=$(echo "$input" | jq -r '.context_window.total_input_tokens // 0')
total_out=$(echo "$input" | jq -r '.context_window.total_output_tokens // 0')

# Cost estimation based on model pricing (per million tokens)
# Prices: Opus $15/$75, Sonnet $3/$15, Haiku $0.80/$4
case "$model_id" in
  *opus*)   in_price=15;    out_price=75 ;;
  *sonnet*) in_price=3;     out_price=15 ;;
  *haiku*)  in_price=0.80;  out_price=4 ;;
  *)        in_price=3;     out_price=15 ;;
esac

session_cost=$(echo "$total_in $total_out $in_price $out_price" | awk '{printf "%.2f", ($1 * $3 + $2 * $4) / 1000000}')

# Session duration tracking
session_id=$(echo "$input" | jq -r '.session_id // empty')
if [ -z "$session_id" ]; then
  session_id="default"
fi
session_start_file="$STATE_DIR/session_start_${session_id}"
now=$(date +%s)
if [ ! -f "$session_start_file" ]; then
  echo "$now" > "$session_start_file"
fi
session_start=$(cat "$session_start_file")
session_secs=$(( now - session_start ))
session_hrs=$(( session_secs / 3600 ))
session_mins=$(( (session_secs % 3600) / 60 ))
if [ "$session_hrs" -gt 0 ]; then
  session_dur="${session_hrs}h${session_mins}m"
else
  session_dur="${session_mins}m"
fi

# Daily cost tracking — accumulate deltas across sessions
today=$(date +%Y-%m-%d)
daily_file="$STATE_DIR/daily_${today}.txt"
prev_file="$STATE_DIR/prev_session_${session_id}.txt"

prev_cost=0
if [ -f "$prev_file" ]; then
  prev_cost=$(cat "$prev_file")
fi

delta=$(echo "$session_cost $prev_cost" | awk '{printf "%.4f", $1 - $2}')
echo "$session_cost" > "$prev_file"

daily_total=0
if [ -f "$daily_file" ]; then
  daily_total=$(cat "$daily_file")
fi
daily_total=$(echo "$daily_total $delta" | awk '{v = $1 + $2; if (v < 0) v = 0; printf "%.4f", v}')
echo "$daily_total" > "$daily_file"

# Clean up old state files (older than 2 days)
find "$STATE_DIR" -name "daily_*" -mtime +2 -delete 2>/dev/null
find "$STATE_DIR" -name "session_start_*" -mtime +2 -delete 2>/dev/null
find "$STATE_DIR" -name "prev_session_*" -mtime +2 -delete 2>/dev/null

# Format token counts with k suffix
fmt_tokens() {
  local n=$1
  if [ "$n" -ge 1000 ] 2>/dev/null; then
    echo "$n" | awk '{printf "%.1fk", $1 / 1000}'
  else
    printf "%s" "$n"
  fi
}

in_fmt=$(fmt_tokens "$total_in")
out_fmt=$(fmt_tokens "$total_out")

# Rate limits
five_h=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')
five_h_resets=$(echo "$input" | jq -r '.rate_limits.five_hour.resets_at // empty')
seven_d=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty')
seven_d_resets=$(echo "$input" | jq -r '.rate_limits.seven_day.resets_at // empty')

# Output style (omit if default)
style=$(echo "$input" | jq -r '.output_style.name // empty')
[ "$style" = "default" ] && style=""

# Colors
RESET="\033[0m"
DIM="\033[2m"
BOLD="\033[1m"
CYAN="\033[36m"
GREEN="\033[32m"
YELLOW="\033[33m"
MAGENTA="\033[35m"
RED="\033[31m"
BLUE="\033[34m"
BRIGHT_WHITE="\033[97m"

# Rate limit color based on burn rate vs remaining time
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

# Git info
git_segment=""
if git -C "$cwd_full" rev-parse --is-inside-work-tree &>/dev/null 2>&1; then
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

# --- Build individual segments ---
SEP="$(printf " ${DIM}|${RESET} ")"

seg_cwd="$(printf "📂 ${BOLD}${BRIGHT_WHITE}%s${RESET}" "$cwd")"

seg_git=""
[ -n "$git_segment" ] && seg_git="${git_segment}"

seg_model="$(printf "🤖 ${CYAN}%s${RESET}" "$model")"

seg_tokens="$(printf "📥 ${GREEN}%s${RESET} 📤 ${MAGENTA}%s${RESET}" "$in_fmt" "$out_fmt")"

seg_ctx=""
if [ -n "$used_pct" ]; then
  used_int=$(printf "%.0f" "$used_pct")
  if [ "$used_int" -ge 80 ]; then
    ctx_color="$RED"
  elif [ "$used_int" -ge 50 ]; then
    ctx_color="$YELLOW"
  else
    ctx_color="$GREEN"
  fi
  remaining_tokens=$(echo "$ctx_window_size $used_pct" | awk '{printf "%.0f", $1 * (100 - $2) / 100}')
  if [ "$remaining_tokens" -ge 1000000 ] 2>/dev/null; then
    remaining_fmt=$(echo "$remaining_tokens" | awk '{printf "%.1fM", $1 / 1000000}')
  elif [ "$remaining_tokens" -ge 1000 ] 2>/dev/null; then
    remaining_fmt=$(echo "$remaining_tokens" | awk '{printf "%.0fK", $1 / 1000}')
  else
    remaining_fmt="${remaining_tokens}"
  fi
  seg_ctx="$(printf "🧠 ${ctx_color}%d%% (%s left)${RESET}" "$used_int" "$remaining_fmt")"
fi

seg_session_cost="$(printf "💰 ${GREEN}\$%s${RESET} ${DIM}(%s)${RESET}" "$session_cost" "$session_dur")"
seg_daily_cost="$(printf "📅 ${GREEN}\$%s${RESET} ${DIM}today${RESET}" "$(printf '%.2f' "$daily_total")")"

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
  # wc -L is GNU only; use awk for cross-platform display width
  printf "%s" "$stripped" | awk '{print length}' | head -1
}

# Walk up process tree to find a real TTY and query its width
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
# 1. output style
# 2. context remaining "(XXK left)" — keeps percentage
# 3. rate limit reset times — keeps percentages
# 4. git info
# 5. folder name
# 6. daily cost
# 7. session cost
# Always keep: model, tokens, context %, rate limit %

assemble() {
  local out=""
  # Args: cwd git ctx_rem session daily rl_resets style rl ctx tokens model
  local show_cwd=$1 show_git=$2 show_ctx_remaining=$3
  local show_session_cost=$4 show_daily_cost=$5 show_rl_resets=$6 show_style=$7
  local show_rl=${8:-1} show_ctx=${9:-1} show_tokens=${10:-1} show_model=${11:-1}

  [ "$show_cwd" = "1" ] && out="${out}${seg_cwd}"
  if [ "$show_git" = "1" ] && [ -n "$seg_git" ]; then
    if [ -n "$out" ]; then
      out="${out} ${DIM}|${RESET} "
    fi
    out="${out}${seg_git}"
  fi
  [ -n "$out" ] && out="${out}${SEP}"

  [ "$show_model" = "1" ] && out="${out}${seg_model}"

  if [ "$show_ctx" = "1" ] && [ -n "$seg_ctx" ]; then
    if [ "$show_ctx_remaining" = "1" ]; then
      out="${out}${SEP}${seg_ctx}"
    else
      out="${out}${SEP}$(printf "🧠 ${ctx_color}%d%%${RESET}" "$used_int")"
    fi
  fi

  [ "$show_tokens" = "1" ] && out="${out}${SEP}${seg_tokens}"

  if [ "$show_rl" = "1" ] && [ -n "$seg_rl" ]; then
    if [ "$show_rl_resets" = "1" ]; then
      out="${out}${SEP}${seg_rl}"
    else
      local rl_short=""
      if [ -n "$five_h" ]; then
        rl_short="$(printf "5h ${five_h_color}%d%%${RESET}" "$five_h_int")"
      fi
      if [ -n "$seven_d" ]; then
        [ -n "$rl_short" ] && rl_short="${rl_short}$(printf " ${DIM}|${RESET} ")"
        rl_short="${rl_short}$(printf "7d ${seven_d_color}%d%%${RESET}" "$seven_d_int")"
      fi
      out="${out}${SEP}$(printf "⚡ %b" "$rl_short")"
    fi
  fi

  [ "$show_session_cost" = "1" ] && out="${out}${SEP}${seg_session_cost}"
  [ "$show_daily_cost" = "1" ] && out="${out}${SEP}${seg_daily_cost}"

  [ "$show_style" = "1" ] && [ -n "$seg_style" ] && out="${out}${SEP}${seg_style}"

  # Clean leading separator if model was hidden
  out=$(printf "%b" "$out" | sed 's/^ *| *//')

  printf "%b" "$out"
}

# Try full output, then progressively drop lowest-priority segments
# Drop order: style, rl resets, git, folder, daily cost, session cost
# ctx_remaining is always kept (most important after model/tokens/ctx%)
# Progressive removal (least important dropped first):
# 1. style, 2. rl resets, 3. daily cost, 4. session cost,
# 5. folder, 6. git, 7. rate limits, 8. tokens, 9. ctx remaining
# Never drop: model, context %
#
# Args: cwd git ctx_rem session daily rl_resets style rl ctx tokens model
configs=(
  "1 1 1 1 1 1 1 1 1 1 1"
  "1 1 1 1 1 1 0 1 1 1 1"
  "1 1 1 1 1 0 0 1 1 1 1"
  "1 1 1 1 0 0 0 1 1 1 1"
  "1 1 1 0 0 0 0 1 1 1 1"
  "0 1 1 0 0 0 0 1 1 1 1"
  "0 0 1 0 0 0 0 1 1 1 1"
  "0 0 1 0 0 0 0 0 1 1 1"
  "0 0 1 0 0 0 0 0 1 0 1"
)

for config in "${configs[@]}"; do
  result=$(assemble $config)
  width=$(get_display_width "$result")
  # Use full reserve (30 chars) once tokens are shown (arg 10), compact otherwise
  local_tw=$term_width_compact
  show_tokens=$(echo "$config" | awk '{print $10}')
  [ "$show_tokens" = "1" ] && local_tw=$term_width_full
  if [ "$width" -le "$local_tw" ]; then
    printf "%b" "$result"
    exit 0
  fi
done

# Always show model + context with remaining, even if it doesn't fit
printf "%b" "$(assemble 0 0 1 0 0 0 0 0 1 0 1)"
