using System;

public static class RespawnEvents
{
    public static event Action Respawned;

    public static void RaiseRespawned()
    {
        Respawned?.Invoke();
    }
}
