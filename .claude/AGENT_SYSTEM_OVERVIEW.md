# Multi-Agent System - Single-Threaded Workflow

## System Architecture

```mermaid
graph TD
    C[Coordinator] --> T1[Task 0001]
    T1 --> B[Backend Agent]
    B --> T2[Task 0002]
    T2 --> C
    C --> T3[Task 0003]
    T3 --> F[Frontend Agent]
    F --> T4[Task 0004]
    T4 --> C
    C --> T5[Task 0005]
    T5 --> BR[Backend Reviewer]
    BR --> T6[Task 0006]
    T6 --> C
    C --> A[Archive]

    subgraph "CLI Commands"
        PP1[pp claude-agent coordinator]
        PP2[pp claude-agent backend --bypass-permissions]
        PP3[pp claude-agent frontend --bypass-permissions]
        PP4[pp claude-agent backend-reviewer --bypass-permissions]
    end

    subgraph "Monitoring"
        M1[/monitor - 2hr timeout]
        M2[pp claude-agent-process-message-queue]
        M3[File detection & processing]
    end
```

## Workflow Rules

### **1. Single-Task Rule**
- Only ONE task file exists in the system at any time
- Tasks flow sequentially: 0001 → 0002 → 0003 → etc.
- Each agent increments the number when passing task forward

### **2. Agent Startup**
```bash
# Coordinator (interactive)
pp claude-agent coordinator --bypass-permissions

# Workers (auto-monitor)
pp claude-agent backend --bypass-permissions --color
pp claude-agent frontend --bypass-permissions --color
pp claude-agent backend-reviewer --bypass-permissions --color
pp claude-agent frontend-reviewer --bypass-permissions --color
```

### **3. Task Flow**
1. **Coordinator** creates `task_0001_description.md` → Backend
2. **Backend** processes, increments to `task_0002_description.md` → Coordinator
3. **Coordinator** reads, decides next step, creates `task_0003_description.md` → Frontend
4. **Frontend** processes, increments to `task_0004_description.md` → Coordinator
5. **Coordinator** archives completed task

### **4. Structured Response Template**
Every agent MUST use this template:
```markdown
**agent-name** - date

## Summary
[What was accomplished]

## Problems
[Issues found or remaining]

## Next Action
[Which agent should work next and what they should do]
```

### **5. File Naming Convention**
- `task_0001_fix_warnings.md` (descriptive name)
- `task_0002_fix_warnings.md` (same name, incremented number)
- Sequential numbering preserves task thread

### **6. CLI Components**
- **AgentCommand.cs**: Starts agents with proper priming
- **ProcessMessageQueueCommand.cs**: Detects files with 2-hour timeout
- **Monitor slash command**: Processes files with structured template
- **Auto-cleanup**: Keep-alive messages, todo list clearing

### **7. Agent Specialization**
- **Backend**: Uses `.claude/rules/backend/` rules
- **Frontend**: Uses `.claude/rules/frontend/` rules
- **Backend-reviewer**: Uses backend rules + `backend-code-reviewer.md`
- **Frontend-reviewer**: Uses frontend rules + `frontend-code-reviewer.md`
- **Coordinator**: Uses `coordinator.md` + `quality-gate-committer.md`

### **8. Key Features**
- ✅ **Single-threaded**: One task at a time
- ✅ **Sequential numbering**: Clear task progression
- ✅ **Full context**: Each agent sees complete history
- ✅ **Structured responses**: Consistent format
- ✅ **Autonomous monitoring**: 2-hour cycles with keep-alive
- ✅ **Role-specific rules**: Each agent follows specialized guidelines
- ✅ **Visual indicators**: Colors and active agent notifications

## Error Handling
- Multiple tasks detected → CLI shows warning
- Keep-alive messages → Auto-deleted by monitoring
- Todo lists → Cleared before task handoff
- Process cleanup → Auto-kill orphaned processes

This system ensures clear, single-threaded task progression with full context preservation and autonomous agent operation.