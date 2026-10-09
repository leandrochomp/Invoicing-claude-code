# Git & GitHub workflow

## Commit style

- Conventional Commits — `type(scope): imperative summary`.
- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `build`.
- Example: `feat(api): add invoice line item validation`
- Subject line: ≤ 72 chars, lowercase, no trailing period.
- One logical change per commit.
- Include regression/behavior tests in the same commit as the change.
- Never commit secrets, connection strings, or generated build artifacts.
- Keep the `Co-Authored-By` trailer as the last line of the commit body.
- Never leave a body that is only the AI co-author trailer.
- Run `dotnet build` / `npm run build` before committing (per repo constraints).

## Branching

- All work happens on a feature branch.
- Never commit directly to `main`.
- Sync and cut a branch before any change:

    ```sh
    git switch main
    git pull
    git switch -c <type>/<short-description>
    ```

- Branch naming: `type/short-description`
- Examples: `feat/invoice-line-validation`, `fix/tax-calc-rounding`, `docs/api-readme`.
- Use the same type as the commit it belongs to.
- Include the issue number when one exists (`feat/12-invoice-validation`).
- One branch per issue / logical unit of work.
- Branches are short-lived: cut from latest `main`, push, PR, merge, delete.
- Branch from latest `main` only.
- Rebase if `main` moves before merging:

    ```sh
    git fetch origin
    git rebase origin/main
    ```

- Delete the branch after merge (squash-merge does this automatically on GitHub).

## Committing

- Stage and commit locally:

    ```sh
    git add -p          # review hunks before staging
    git commit          # message per Conventional Commits (see above)
    ```

- Always commit locally first, then push.
- Never `--force` to `main`.
- Amending is fine until pushed.
- After pushing, fix with a new commit or PR review.

## Pushing & PRs

- Push to a feature branch:

    ```sh
    git push -u origin <branch>
    ```

- Use `gh` for all GitHub operations.
- No new GitHub-related dependencies.
- Open the PR with an explicit title and body.
- Do not rely on `--fill`.

    ```sh
    gh pr create --title "feat(api): add invoice line validation" --body "<body>"
    ```

- `--fill` uses the commit message body automatically.
- Never open a PR with a body that is only the commit list or the co-author trailer.
- PR body must be meaningful.
- Explain *what* changed and *why*, not *how* (2-4 sentences).
- PR body template:

    ```markdown
    ## Summary
    2-4 sentences: what changed and why.

    ## Changes
    - Bullet list of notable changes, one per commit or logical unit.

    ## Testing
    How it was verified (dotnet test, npm run build, manual checks).
    ```

- PRs must pass builds.
- PRs must link the issue they close (e.g. `Closes #12`).
- Squash-merge keeps `main` history clean:

    ```sh
    gh pr merge --squash
    ```

## Misc

- `gh auth status` to verify auth.
- `gh issue list` / `gh issue view` for issue work.
- Never paste tokens; `gh` handles auth.
