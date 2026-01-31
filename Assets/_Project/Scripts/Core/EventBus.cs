using System;
using UnityEngine;

namespace CounterSiege
{
    public static class EventBus
    {
        public static Action<GameObject, DamageInfo> OnPlayerDied;
        public static Action<RoundPhase> OnRoundPhaseChanged;
        public static Action<GameObject, Vector3> OnWeaponFired;
        public static Action<GameObject> OnBombPlanted;
        public static Action<GameObject> OnBombDefused;
        public static Action OnBombExploded;
        public static Action<GameObject, GameObject, string, HitZone> OnKill; // victim, killer, weapon, hitzone
        public static Action<GameObject, int> OnMoneyChanged;
        public static Action OnScoreChanged;
        public static Action<Team> OnRoundWon;
        public static Action<Team> OnMatchWon;
        public static Action<GameObject, WeaponData> OnWeaponPickedUp;
        public static Action<GameObject, WeaponData> OnWeaponDropped;
        public static Action<int, int> OnAmmoChanged; // magazine, reserve
        public static Action<int, int> OnHealthChanged; // health, armor
        public static Action<string> OnBombStateChanged;
        public static Action<float> OnBombTimerTick;
        public static Action<bool, int> OnScopeChanged; // scoped, level (0=none, 1=first, 2=second)

        public static void Reset()
        {
            OnPlayerDied = null;
            OnRoundPhaseChanged = null;
            OnWeaponFired = null;
            OnBombPlanted = null;
            OnBombDefused = null;
            OnBombExploded = null;
            OnKill = null;
            OnMoneyChanged = null;
            OnScoreChanged = null;
            OnRoundWon = null;
            OnMatchWon = null;
            OnWeaponPickedUp = null;
            OnWeaponDropped = null;
            OnAmmoChanged = null;
            OnHealthChanged = null;
            OnBombStateChanged = null;
            OnBombTimerTick = null;
            OnScopeChanged = null;
        }
    }
}
