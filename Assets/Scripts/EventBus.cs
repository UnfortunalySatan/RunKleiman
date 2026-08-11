using UnityEngine;
using System;
public static class EventBus
{
    public static Action isPlay;
    public static Action isWallHit;
    public static Action isRestart;
    public static Action isContitue;
    public static Action isPauseMenu;
    public static Action<float> soundVolumeChanged;
}
