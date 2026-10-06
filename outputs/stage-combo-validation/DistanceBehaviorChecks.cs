using System;

public static class DistanceBehaviorChecks
{
    private static int checks;
    private static void Check(bool condition, string label)
    {
        checks++;
        if (!condition) throw new Exception(label);
    }

    public static string Run()
    {
        checks = 0;
        var progress = new StageDistanceProgress();
        Check(progress.StageLength == 57600f && progress.StageNumber == 1 && progress.StageDistance == 0f, "new run starts at route zero");
        progress.Advance(320f, 45f, true);
        Check(progress.StageDistance == 14400f && progress.NormalizedProgress == 0.25f, "base-speed first event at 45 seconds");
        progress.Advance(320f, 45f, true);
        Check(progress.StageDistance == 28800f && progress.NormalizedProgress == 0.5f, "base-speed second event at 90 seconds");
        progress.Advance(320f, 45f, true);
        Check(progress.StageDistance == 43200f && progress.NormalizedProgress == 0.75f, "base-speed third event at 135 seconds");
        progress.Advance(320f, 45f, true);
        Check(progress.StageDistance == 57600f && progress.NormalizedProgress == 1f, "base-speed route complete at 180 seconds");
        progress.Advance(690f, 100f, true);
        Check(progress.StageDistance == 57600f, "route completion clamps overshoot");

        progress.BeginNextStage(57600f);
        Check(progress.StageNumber == 2 && progress.StageDistance == 0f, "new stage resets distance");
        progress.BeginNextStage(57600f);
        Check(progress.StageNumber == 3, "stage ordinal continues beyond two repeating backgrounds");
        progress.Advance(690f, 60f, true);
        Check(progress.StageDistance == 41400f, "dash speed advances distance faster than base");
        progress.Reset(57600f);
        progress.Advance(160f, 180f, true);
        Check(progress.StageDistance == 28800f, "slower speed does not complete route at fixed 180 seconds");
        progress.Advance(160f, 180f, true);
        Check(progress.StageDistance == 57600f, "slower speed reaches the same endpoint later");

        progress.Reset(57600f);
        progress.Advance(320f, 1f, true);
        float beforePause = progress.StageDistance;
        for (int i = 0; i < 4; i++)
        {
            progress.Advance(690f, 100f, false);
            Check(progress.StageDistance == beforePause, "non-playing gameplay state freezes route");
        }
        progress.Advance(-320f, 10f, true);
        Check(progress.StageDistance == beforePause, "negative speed cannot reverse route");
        progress.Advance(0f, 100f, true);
        Check(progress.StageDistance == beforePause, "zero speed stops route");
        progress.Advance(320f, -1f, true);
        Check(progress.StageDistance == beforePause, "negative frame delta is ignored");
        progress.Advance(320f, 0f, true);
        Check(progress.StageDistance == beforePause, "zero frame delta is ignored");
        progress.Reset(57600f);
        for (int i = 0; i < 10800; i++) progress.Advance(320f, 1f / 60f, true);
        Check(progress.NormalizedProgress == 1f, "180-second route is stable under 60 Hz frame accumulation");
        progress.Reset(0f);
        Check(progress.StageLength == 1f && progress.StageNumber == 1 && progress.StageDistance == 0f, "invalid length receives safe nonzero default and run reset");

        var events = new StageEventSchedule();
        events.BeginStage(1);
        Check(!events.IsDue(14399f, 57600f), "first event not early");
        Check(events.IsDue(14400f, 57600f), "first event at exact 25 percent");
        Check(events.IsDue(14400f, 57600f), "failed spawn remains retryable before commit");
        events.RecordSpawn();
        Check(!events.IsDue(14400f, 57600f), "first event cannot spawn twice");
        Check(!events.IsDue(28799f, 57600f), "second event not early");
        Check(events.IsDue(28800f, 57600f), "second event at exact 50 percent");
        events.RecordSpawn();
        Check(!events.IsDue(43199f, 57600f), "third event not early");
        Check(events.IsDue(43200f, 57600f), "third event at exact 75 percent");
        events.RecordSpawn();
        Check(events.SpawnedEvents == 3 && !events.IsDue(57600f, 57600f), "route endpoint cannot add a fourth event");
        events.RecordSpawn();
        Check(events.SpawnedEvents == 3, "encounter commit cannot exceed three");
        events.BeginStage(3);
        Check(events.StageNumber == 3 && events.SpawnedEvents == 0, "event stage follows run ordinal");
        int caughtUp = 0;
        while (events.IsDue(50000f, 57600f)) { events.RecordSpawn(); caughtUp++; }
        Check(caughtUp == 3, "large frame catches all three crossed boundaries once");
        Check(!events.IsDue(50000f, 57600f), "catch-up never repeats");
        events.BeginStage(0);
        Check(events.StageNumber == 1, "invalid event stage ordinal is normalized");
        Check(!events.IsDue(100f, 0f), "event schedule rejects zero-length route");

        for (int encounter = 1; encounter <= 3; encounter++)
        {
            Check(StageEventSchedule.IsUpgradeEncounter(1, encounter, true, 0f, 1f / 3f) == (encounter == 2), "stage-one test upgrade selection preserved");
            Check(!StageEventSchedule.IsUpgradeEncounter(1, encounter, false, 0f, 1f / 3f), "stage-one upgrade test can be disabled");
            Check(StageEventSchedule.IsUpgradeEncounter(2, encounter, true, 0.99f, 1f / 3f) == (encounter == 1), "stage-two first upgrade selection preserved");
        }
        Check(StageEventSchedule.IsUpgradeEncounter(3, 1, true, 0.2f, 1f / 3f), "later-stage upgrade probability accepts low roll");
        Check(!StageEventSchedule.IsUpgradeEncounter(3, 1, true, 0.5f, 1f / 3f), "later-stage upgrade probability rejects high roll");
        Check(!StageEventSchedule.IsUpgradeEncounter(3, 1, true, 0f, -1f), "upgrade probability lower clamp");
        Check(StageEventSchedule.IsUpgradeEncounter(3, 1, true, 0.99f, 2f), "upgrade probability upper clamp");

        var bosses = new StageBossSchedule();
        Check(bosses.GetDueRequest(true, 1, 0f, 57600f, 180f, 900f) == StageBossRequest.None, "fixed 180-second boss trigger is removed");
        Check(bosses.GetDueRequest(true, 1, 57599f, 57600f, 899f, 900f) == StageBossRequest.None, "ordinary boss requires route endpoint");
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 90f, 900f) == StageBossRequest.Stage, "early fast route can request ordinary boss");
        bosses.RecordRequest(StageBossRequest.Stage, 1);
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 90f, 900f) == StageBossRequest.None, "same stage cannot request boss twice");
        Check(bosses.GetDueRequest(false, 2, 57600f, 57600f, 90f, 900f) == StageBossRequest.None, "boss state does not request another route boss");
        Check(bosses.GetDueRequest(true, 2, 0f, 57600f, 90f, 900f) == StageBossRequest.None, "new route starts without boss");
        Check(bosses.GetDueRequest(true, 2, 57600f, 57600f, 200f, 900f) == StageBossRequest.Stage, "next stage can request one ordinary boss");
        Check(bosses.GetDueRequest(false, 2, 57600f, 57600f, 900f, 900f) == StageBossRequest.None, "final boss waits for Playing state");
        Check(bosses.GetDueRequest(true, 2, 100f, 57600f, 900f, 900f) == StageBossRequest.Final, "15-minute final boss overrides incomplete distance");
        Check(bosses.GetDueRequest(true, 2, 57600f, 57600f, 900f, 900f) == StageBossRequest.Final, "same-frame distance and ending request only final boss");
        bosses.RecordRequest(StageBossRequest.Final, 2);
        Check(bosses.GetDueRequest(true, 2, 57600f, 57600f, 901f, 900f) == StageBossRequest.None, "final request cannot repeat");
        Check(bosses.GetDueRequest(true, 3, 57600f, 57600f, 901f, 900f) == StageBossRequest.None, "final request blocks later ordinary bosses");
        bosses.Reset();
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 180f, 900f) == StageBossRequest.Stage, "scene restart resets ordinary request state");
        Check(bosses.GetDueRequest(true, 1, 0f, 57600f, 900f, 900f) == StageBossRequest.Final, "scene restart resets final request state");
        Check(bosses.GetDueRequest(true, 1, 100f, 0f, 1f, 900f) == StageBossRequest.None, "ordinary boss rejects zero-length route");
        bosses.RecordRequest(StageBossRequest.Stage, 1);
        bosses.CancelRequest(StageBossRequest.Stage, 2);
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 180f, 900f) == StageBossRequest.None, "another stage cannot cancel a held route boss request");
        bosses.CancelRequest(StageBossRequest.Stage, 1);
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 180f, 900f) == StageBossRequest.Stage, "cancelled warning releases the route boss for retry");
        bosses.RecordRequest(StageBossRequest.Stage, 1);
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 180f, 900f) == StageBossRequest.None, "retry reserves the route boss once again");
        bosses.RecordRequest(StageBossRequest.Final, 3);
        bosses.CancelRequest(StageBossRequest.Final, 2);
        Check(bosses.GetDueRequest(true, 3, 100f, 57600f, 900f, 900f) == StageBossRequest.None, "another stage cannot cancel a final boss reservation");
        bosses.CancelRequest(StageBossRequest.Final, 3);
        Check(bosses.GetDueRequest(true, 3, 100f, 57600f, 900f, 900f) == StageBossRequest.Final, "cancelled final warning remains retryable at 900 seconds");
        bosses.CancelRequest(StageBossRequest.None, 1);
        Check(bosses.GetDueRequest(true, 1, 57600f, 57600f, 180f, 900f) == StageBossRequest.None, "manual warning cancellation does not release an unrelated route reservation");
        return checks + " distance/event/boss behavior checks passed. Pure production classes extracted from StageManager.cs, EventObjectSpawner.cs and Spawner.cs; Unity MonoBehaviour lifecycle and coroutine execution excluded.";
    }
}
