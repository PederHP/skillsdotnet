---
name: code-review
description: Perform thorough code reviews with best practices
license: MIT
compatibility: [claude, cursor, copilot]
metadata:
  author: skillsdotnet
  version: "1.0"
---

# Code Review Skill

Review code changes for correctness, security, performance, and maintainability.

## Process

1. Read the diff or file(s) provided
2. Check against the [review checklist](references/checklist.md)
3. Report findings grouped by severity (critical, warning, suggestion)

## Output Format

For each finding:
- **File** and **line** (if applicable)
- **Severity**: critical | warning | suggestion
- **Description**: what the issue is and why it matters
- **Fix**: concrete suggestion for how to resolve it
