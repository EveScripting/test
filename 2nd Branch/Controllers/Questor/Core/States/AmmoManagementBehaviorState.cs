namespace EVESharpCore.Questor.States
{
    public enum AmmoManagementBehaviorState
    {
        Default,
        Idle,
        Start,
        Monitor,
        HandleWeaponsNeedReload,
        HandleWeaponsNeedSpecialReload,
        HandleOverrideTargetIfFoundAndAmmoChangeNeeded,
        HandleWeaponsDoNotHaveAllTheSameAmmoTypeLoaded,
        HandleWrongAmmoDamageType,
        HandleWrongAmmoNotEnoughRange,
        HandleWrongAmmoOtherAmmoDoesMoreDamageAndIsInRange,
        HandleWrongAmmoShorterRangeAmmoStillHits,
        HandleWrongAmmoNeedsBetterTracking,
        HandleWrongAmmoOutOfBestAmmoUseWhatWeHave,
        HandleChangeToDefaultAmmo,
        ChangeToEMAmmo,
        ChangeToT2EMAmmo,
        //
        // Needs to take into account all the types of T2 Ammo? Rage, Javelin for missiles... projectile? Crystals?
        //
        ChangeToFactionEMAmmo,
        ChangeToExplosiveAmmo,
        ChangeToT2ExplosiveAmmo,
        ChangeToFactionExplosiveAmmo,
        ChangeToThermalAmmo,
        ChangeToT2ThermalAmmo,
        ChangeToFactionThermalAmmo,
        ChangeToKineticAmmo,
        ChangeToT2KineticAmmo,
        ChangeToFactionKineticAmmo,
        OutOfAmmo,
        Paused,
    }
}