# Effect visibility and white outline evidence

User reported invisible drops/wind and requested white borders on dropped objects. Scope: existing pickup/wind sorting, barrel sorting, three authored pickup outlines and their sprite synchronization. See [the session follow-up](../../Docs/SessionLogs/2026-10-06-driving-boss-progression.md).

- Production URP/Renderer2D, current Junmo camera and actual first BeltScroll background are retained. Camera masks, materials and original pickup sizes are retained. Memory-only A/B changes Sorting Layer alone: every Default effect changes0 pixels; ForeGround reveals all three drops and wind.
- Final184 real-render/sorting/outline assertions pass. All three kinds and five flesh sprites add real white pixels. Shader support/compile checks pass and original pipeline/QualitySettings are preserved.
- White silhouettes are eight authored SpriteRenderer copies behind each body; RewardPickup.Initialize synchronizes the selected native sprite once. No original meat texture/GUID is rewritten and no presentation hierarchy is generated at runtime.
- [Before](./before-default.png), [after](./after-foreground.png), [flesh](./flesh-outlined.png), [soul](./soul-outlined.png), [fuel](./fuel-outlined.png), [render result](./render-result.txt).

The disposable project remains `outputs/drive-boss-validation/project`, with its separate save identity. Editor authoring and original-document merge preserve unrelated scene/asset blocks. Raw logs and before/after backups stay local and ignored by Git.

- Post-outline [native regression](./play-result.txt):53 lifecycle/input/physics/drop/pursuit/boss checks pass, runtime exceptions/errors0, with the existing fixture controls documented in its header/session.
- Runtime142 / existing Editor7 source compilation has errors0; render/authoring fixtures also compile in the isolated Unity import. Original Editor Play Mode and player builds remain unrun.
- [Current source/asset snapshot](./source-snapshot.json) records original/copy hashes after the follow-up. Original meat PNG/meta are preserved. No package or ProjectSettings changes are transferred.
