---
kind: luat
scope: du-an
verified: 2026-09-08
---

# Superseded — prototype captures

These four PNGs were captured from the deleted prototype, which was
**frozen as historical reference on 2026-08-22**.

They do **not** show the shipped Angular dashboard. Keep them only for
before/after comparison; never cite them as evidence of current behaviour.

Sibling capture, also historical: `../dashboard--empty--desktop-1440.png` was
captured on **2026-08-22** from the then-live app at `/dashboard` on the Angular
dev server, against an empty database. That route was **removed on 2026-08-29**
together with the `DtiWeekly` module, so the shot cannot be reproduced and shows
nothing that runs today. `grep -n 'dashboard' src/FE/src/app/app.routes.ts`
returns no route.

> 🔄 **SỬA 2026-09-08.** This line was written in the present tense — *"Current
> capture … live app at `http://localhost:4201/dashboard`"* — and so read as an
> instruction to go and look at a URL that has not existed for ten days. The
> file it names is real; only the tense and the "current" label were wrong.

There is **no** current capture of `/trang-chu` under this folder. The design
that will replace this screen at that URL is `Screens/01-dashboard.md`, and it
has never been built, so nothing can be captured of it yet.
