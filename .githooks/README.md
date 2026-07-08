# Git hooks

Version-controlled hooks for this repository. They are **not** active until you point git
at this directory (git does not run tracked hooks automatically):

```bash
git config core.hooksPath .githooks
```

## `pre-commit` — commit attribution policy

Every commit in this repository must be authored by **phmatray@gmail.com**. The hook reads
the author identity git is about to record and rejects the commit if the email differs.

If a commit is rejected, set the author email and try again:

```bash
git config user.email phmatray@gmail.com
```
