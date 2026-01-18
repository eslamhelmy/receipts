---
description: Review PR changes between main and feature branch
argument-hint: <feature-branch-name>
allowed-tools: Bash(git fetch:*), Bash(git diff:*), Bash(git log:*), Read, Grep
---

# PR Code Review

Reviewing branch: $ARGUMENTS

## Steps

1. First run: `git fetch origin`
2. Get commits: `git log --oneline origin/main...origin/$ARGUMENTS`
3. Get diff: `git diff origin/main...origin/$ARGUMENTS`

Then analyze the changes.

---

## Review Instructions

Senior .NET Tech Lead review. Focus on **impact and risk**, not style.

### Priority Areas

1. **🚨 Breaking Changes** - API signatures, DB schema, config, contracts, events
2. **🔒 Security** - Validation, auth, secrets
3. **⚡ Performance** - Missing async, N+1, blocking calls
4. **🐛 Bugs** - Null refs, exception handling, logic errors
5. **🏗️ C#** - Async correctness, LINQ misuse, IDisposable

---

## Output

### ⚠️ Breaking Changes

### 🔴 Must Fix

### 🟡 Should Consider

### 📊 Summary
| Approval | Risk | Breaking Changes |
|----------|------|------------------|
|          |      |                  |