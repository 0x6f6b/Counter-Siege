namespace CounterSiege
{
    public enum Team { Terrorist, CounterTerrorist }

    public enum RoundPhase { Warmup, FreezeTime, Live, PostRound }

    public enum WeaponSlot { Melee, Pistol, Primary, Bomb, Grenade }

    public enum WeaponType { Knife, Pistol, Rifle, Sniper, Grenade }

    public enum HitZone { Head, Chest, Stomach, Legs }

    public enum BombState { Carried, Planting, Planted, Defusing, Defused, Exploded }

    public enum BotDifficulty { Easy, Normal, Hard }

    public enum TeamRestriction { Both, TerroristOnly, CounterTerroristOnly }
}
