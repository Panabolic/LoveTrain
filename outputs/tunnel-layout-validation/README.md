# Tunnel placement geometry

See [the implementation record](../../Docs/SessionLogs/2026-10-06-tunnel-camera-layout.md) for the six Junmo Transform positions shifted with the size16→20.8 camera's right-edge difference.

`geometry-result.json` records actual tunnel PNG alpha bounds and source/math projection at camera offsets0/100/1000. Both tunnel arrivals preserve the old right-edge alignment, their initial sprites remain outside the view, and the core train's entry destination clears the right edge. This is static geometry evidence, not Unity import, physics, Tween playback or rendered-image validation.

The before-scene backup is local. The [separate native attempt](../tunnel-camera-validation/README.md) failed during licensing startup before producing fixture results or PNGs. Runtime stage timing, camera-follow offset code, prefab scale and vertical rail alignment were left intact.
