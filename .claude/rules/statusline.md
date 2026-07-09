# Statusline

Load this when changing the Claude Code statusline script.

## Repo Copy Is the Source, Global Is the Deploy Target

The statusline script exists in two places:

- `.claude/statusline.sh` in this repo — the source of truth, used by sessions in this repo via `.claude/settings.json`.
- `~/.claude/statusline.sh` — the global copy, used by all other projects via `~/.claude/settings.json`.

Always make changes to the repo copy only, so they can be verified live in this repo first. Never edit the global copy directly. Only copy the repo script over the global one when the user explicitly asks to deploy it.

## Testing

Test changes by piping a representative JSON payload into the script and checking the rendered output, for example:

```
printf '%s' '{"model":{"display_name":"Claude Opus 4.8 (1M context)"},"cwd":"<repo path>","context_window":{"used_percentage":41,"context_window_size":1000000,"total_input_tokens":410000},"cost":{"total_cost_usd":3.21,"total_duration_ms":18000000},"rate_limits":{"five_hour":{"used_percentage":6,"resets_at":<epoch+9000>},"seven_day":{"used_percentage":3,"resets_at":<epoch+400000>}}}' | COLUMNS=200 bash .claude/statusline.sh
```

Test at several `COLUMNS` values to verify the progressive segment dropping. Watch for width miscounting: invisible sequences (colors, OSC 8 hyperlinks) must be stripped before measuring, and character counting must not fall back to byte counting (macOS `awk length` counts bytes; the block-drawing bar characters and emoji are multi-byte).
