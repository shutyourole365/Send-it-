# Advanced Git Commands

This document covers advanced Git commands that are useful for managing your workflow and repository history. Each command includes a description and practical examples.

---

## Table of Contents

1. [git stash](#git-stash)
2. [git cherry-pick](#git-cherry-pick)
3. [git revert](#git-revert)
4. [git reset](#git-reset)

---

## git stash

`git stash` temporarily shelves (stashes) changes you've made to your working tree so you can work on something else and come back to apply them later.

### Common Usage

```bash
# Stash your current uncommitted changes
git stash

# Stash with a descriptive message
git stash push -m "WIP: tweaking suspension physics parameters"

# List all stashes
git stash list

# Apply the most recent stash (keeps the stash in the list)
git stash apply

# Apply a specific stash by index
git stash apply stash@{2}

# Pop the most recent stash (applies it and removes it from the list)
git stash pop

# Drop (delete) a specific stash
git stash drop stash@{0}

# Clear all stashes
git stash clear
```

### Example Scenario

You are in the middle of tuning the engine physics parameters, but need to urgently fix a bug on a different branch:

```bash
# Save your in-progress work
git stash push -m "WIP: engine torque curve adjustments"

# Switch to the bug-fix branch
git checkout fix/physics-collision-bug

# ... fix the bug, commit it ...

# Return to your original branch
git checkout feature/engine-tuning

# Restore your stashed work
git stash pop
```

---

## git cherry-pick

`git cherry-pick` applies the changes introduced by one or more existing commits onto the current branch. This is useful when you want to bring a specific commit from one branch into another without merging the entire branch.

### Common Usage

```bash
# Apply a single commit to the current branch
git cherry-pick <commit-hash>

# Apply multiple commits
git cherry-pick <commit-hash-1> <commit-hash-2>

# Apply a range of commits (exclusive of the first, inclusive of the last)
git cherry-pick <start-commit>^..<end-commit>

# Cherry-pick without automatically committing (stage changes only)
git cherry-pick --no-commit <commit-hash>

# Abort an in-progress cherry-pick (e.g., if conflicts arise)
git cherry-pick --abort

# Continue after resolving conflicts
git cherry-pick --continue
```

### Example Scenario

A critical tire grip fix was committed to `feature/tire-physics`, and you want to bring it into the `main` branch without merging the whole feature branch:

```bash
# Find the commit hash of the fix
git log feature/tire-physics --oneline

# Output example:
# a3f8c21 Fix: correct tire slip angle calculation
# b1d4e09 WIP: refactor grip model

# Cherry-pick just the fix commit into main
git checkout main
git cherry-pick a3f8c21
```

---

## git revert

`git revert` creates a new commit that undoes the changes made by a previous commit. Unlike `git reset`, it does not rewrite history, making it safe to use on shared or public branches.

### Common Usage

```bash
# Revert the most recent commit
git revert HEAD

# Revert a specific commit by hash
git revert <commit-hash>

# Revert without automatically creating a commit (stage changes only)
git revert --no-commit <commit-hash>

# Revert a merge commit (must specify the parent to revert to)
git revert -m 1 <merge-commit-hash>
```

### Example Scenario

A change to the aerodynamics calculation introduced a regression. You need to undo that commit while keeping the history intact:

```bash
# Identify the bad commit
git log --oneline

# Output example:
# 9c2b1a7 Refactor: update drag coefficient formula
# 4f1e883 Add downforce calculations

# Revert the problematic commit
git revert 9c2b1a7

# Git will open an editor for the revert commit message.
# Save and close to complete the revert.
```

The repository history will now show both the original commit and the new revert commit, preserving full traceability.

---

## git reset

`git reset` moves the current branch pointer (HEAD) to a specified commit and optionally modifies the staging area and working directory. It is a powerful command for rewriting **local** history.

> **Warning:** Avoid using `git reset` on commits that have already been pushed to a shared remote branch, as it rewrites history and can cause problems for other collaborators. Use `git revert` instead for shared branches.

### Modes

| Mode       | HEAD | Index (Stage) | Working Directory |
|------------|------|---------------|-------------------|
| `--soft`   | ✅ Moved | ❌ Unchanged | ❌ Unchanged |
| `--mixed`  | ✅ Moved | ✅ Reset      | ❌ Unchanged |
| `--hard`   | ✅ Moved | ✅ Reset      | ✅ Reset      |

### Common Usage

```bash
# Soft reset: move HEAD back one commit, keep changes staged
git reset --soft HEAD~1

# Mixed reset (default): move HEAD back, unstage changes, keep in working directory
git reset HEAD~1
git reset --mixed HEAD~1

# Hard reset: discard all changes (staged and unstaged) back to a commit
git reset --hard HEAD~1

# Reset to a specific commit hash
git reset --hard <commit-hash>

# Unstage a specific file (without changing the working directory)
git reset HEAD <filename>
```

### Example Scenarios

**Scenario 1 – Undo a commit but keep your changes staged:**

You committed vehicle suspension parameters too early and want to amend the commit:

```bash
# Undo the last commit, keeping changes staged
git reset --soft HEAD~1

# Now amend your staged changes and recommit
git commit -m "feat: finalize suspension spring stiffness values"
```

**Scenario 2 – Unstage a file accidentally added:**

```bash
# Accidentally staged a debug file
git add Assets/Scripts/Physics/DebugTemp.cs

# Unstage it (file remains in the working directory)
git reset HEAD Assets/Scripts/Physics/DebugTemp.cs
```

**Scenario 3 – Completely discard local experimental changes:**

```bash
# You've been experimenting with tire wear simulation and want to start fresh
git reset --hard HEAD
```

---

## Summary

| Command | Purpose | Rewrites History? | Safe on Shared Branches? |
|---|---|---|---|
| `git stash` | Temporarily save uncommitted changes | No | Yes |
| `git cherry-pick` | Apply specific commits to current branch | No | Yes |
| `git revert` | Undo a commit by creating a new commit | No | ✅ Yes |
| `git reset` | Move HEAD and optionally rewrite index/working dir | Yes (for pushed commits) | ⚠️ No |
