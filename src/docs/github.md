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

## Committing
- Stage and commit locally:
  git add -p          # review hunks before staging
  git commit          # message per Conventional Commits (see AGENTS.md)
- Amending is fine until pushed; after pushing, fix with a new commit or PR review.

## Pushing & PRs
- git push -u origin &lt;branch&gt;
- gh pr create --fill          # uses the commit message body
- gh pr checks --watch         # wait for green before merging
- gh pr merge --squash         # squash-merge keeps main history clean

## Misc
- gh auth status to verify auth; gh issue list / gh issue view for issue work.
- Never paste tokens; gh handles auth.