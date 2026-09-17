# Git & GitHub workflow

## Branching
- Sync and cut a branch before any change:
  git switch main
  git pull
  git switch -c <type>/<short-description>
- Branch from latest `main` only; rebase if `main` moves before merging:
  git fetch origin
  git rebase origin/main
- One branch per issue/change. Delete after merge (squash-merge does this automatically on GitHub).
- PR body must be meaningful: summary, motivation, and what changed.
    Never open a PR with a body that is only the commit list or the
    co-author trailer.

## Committing
- Stage and commit locally:
  git add -p          # review hunks before staging
  git commit          # message per Conventional Commits (see AGENTS.md)
- Amending is fine until pushed; after pushing, fix with a new commit or PR review.

## Pushing & PRs
- git push -u origin <branch>
- gh pr create --fill          # uses the commit message body
- gh pr merge --squash         # squash-merge keeps main history clean
- Open the PR with an explicit title and body (do not rely on --fill):
  gh pr create --title "feat(api): add invoice line validation" --body "<body>"
- PR body template:
  ## Summary
  2-4 sentences: what changed and why.
  ## Changes
  - Bullet list of notable changes, one per commit or logical unit.
  ## Testing
  How it was verified (dotnet test, npm run build, manual checks).
- gh pr merge --squash         # squash-merge keeps main history clean

## Misc
- gh auth status to verify auth; gh issue list / gh issue view for issue work.
- Never paste tokens; gh handles auth.