# Tunnel camera verification — 2026-10-06

Status: **Native verification not executed.** No fixture result or PNG was produced.

The copied-project fixture `TunnelCameraValidation.cs` was prepared for Unity 6000.2.3f1. It retains the production QualitySettings URP asset, Renderer2D and sprite materials. The intended probes use actual StageManager transition references and DOTween milestones at spawn, 6-second arrival and 7.5-second entry, at camera-follow offsets 0, 100 and 1000. These are planned checks, not passed checks.

The initial launcher returned without producing an Editor log. A retained `Start-Process -WindowStyle Hidden` / `WaitForExit` launcher produced `native-retained.log`, but Unity startup repeatedly failed to connect to `LicenseClient-nadom`. The log records a 60-second channel timeout, licensing initialization failure after 74.83 seconds, unsuccessful reconnection and `com.unity.editor.headless` not found. AssetDatabase initial refresh appeared later, but the fixture never produced execution evidence. Root stopped the retained launcher with Ctrl-C (exit 1).

No additional launch, license action, package installation, original Editor action or original asset/code edit was attempted by this verification agent. The original and copied scene/runtime/tunnel/pipeline files inspected for this snapshot are recorded in `source-clone-sync.json`.

Compilation/import of the new fixture, tween assertions, world render, Play Mode and player build remain unverified. The source scene's six X-coordinate changes and their static geometry are assessed separately by the parent task.
