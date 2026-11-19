# Contributing to Fugui

Thanks for your interest in Fugui! Contributions are welcome, through pull requests.

## How it works

- `main` is maintained by Kévin Bouetard ([@Keksls](https://github.com/Keksls)). Only the maintainer merges.
- Fork the repository, create a branch, and open a pull request against `main`.
- Every pull request is reviewed. It may be changed, postponed or declined to keep Fugui consistent, stable and fast.

## Before opening a pull request

- One topic per pull request, with a short description of what and why.
- Test it in Unity 6 with URP, and on mobile if your change touches input or rendering.
- Keep the existing code style (namespaces `Fu` / `Fu.Framework`, XML summaries on public APIs).
- Performance matters: avoid per-frame allocations in the UI and rendering paths.

## Bugs and ideas

Open an issue with your Unity version, render pipeline, platform, and steps or a screenshot to reproduce.
