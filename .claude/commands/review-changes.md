---
description: Review PR changes between master and feature branch
argument-hint: <feature-branch-name>
allowed-tools: Bash(git fetch:*), Bash(git diff:*), Bash(git log:*), Read, Grep
---

# PR Code Review

!`git fetch origin`

## Commits
!`git log --oneline origin/master...origin/$1`

## Changes
!`git diff origin/master...origin/$1`

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