using Auxide;
using Rust;

public class HPVE : RustScript
{
    // Work in progress, and very early...
    // May replace with a large portion of NextGenPVE here
    private static ConfigData configData;

    public HPVE()
    {
        Author = "RFC1920";
        Version = new VersionNumber(1, 0, 1);
    }

    public class ConfigData
    {
        public bool debug;
        public bool allowAdminPVP;
        public bool allowFriendlyFire;
        public bool allowDamageToNPC;
    }

    public override void Initialize()
    {
        Utils.DoLog("HPVE loading...");
        LoadConfig();
    }

    public void LoadConfig()
    {
        if (config.Exists())
        {
            configData = config.ReadObject<ConfigData>();
            return;
        }
        LoadDefaultConfig();
    }

    public void LoadDefaultConfig()
    {
        configData = new ConfigData()
        {
            debug = false,
            allowAdminPVP = false,
            allowFriendlyFire = false,
            allowDamageToNPC = true
        };

        config.WriteObject(configData);
    }

    public object OnTakeDamage(BaseCombatEntity entity, HitInfo info)
    {
        if (info == null) return null;
        if (entity == null) return null;
        if (info.damageTypes.GetMajorityDamageType() == DamageType.Decay) return null;

        if (info?.WeaponPrefab != null)// && info?.WeaponPrefab is TimedExplosive)
        {
            BaseEntity te = info?.WeaponPrefab;// as TimedExplosive;
                                               //BasePlayer attacker = info?.InitiatorPlayer;
            BasePlayer attacker = info?.InitiatorPlayer;// as BasePlayer;
            BasePlayer attacked = info?.HitEntity as BasePlayer;
            //DamageCheck(__instance, info, attacker, attacked, te);
            DamageCheck(entity, info, attacker, attacked, te);
        }
        else if (info?.Initiator is FireBall || info?.Initiator is FlameTurret || info?.Initiator is FlameThrower)
        {
            BasePlayer attacked = info?.HitEntity as BasePlayer;
            // Shortcut for post-bradley fire
            try
            {
                if (info.HitEntity.ShortPrefabName.Equals("servergibs_bradley") || info.HitEntity.ShortPrefabName.Equals("bradley_crate")) return null;
            }
            catch { }
            DamageCheck(entity, info, null, attacked, null);
        }
        else if (info?.InitiatorPlayer != null)
        {
            //BasePlayer attacker = info?.Initiator as BasePlayer;
            BasePlayer attacker = info?.InitiatorPlayer;// as BasePlayer;
            BasePlayer attacked = info?.HitEntity as BasePlayer;
            DamageCheck(entity, info, attacker, attacked);
        }
        return null;
    }

    static void DamageCheck(BaseCombatEntity entity, HitInfo info, BasePlayer attacker, BasePlayer attacked, BaseEntity te = null)
    {
        bool isFriend = false;
        bool isFriend2 = false;
        if (attacker != null && entity != null && info?.Initiator != null)
        {
            isFriend = Utils.IsFriend(attacker.userID, entity.OwnerID);
            isFriend2 = Utils.IsFriend(info.Initiator.OwnerID, entity.OwnerID);
        }
        string weapon = te != null ? " using " + te?.GetType() : "";
        //if (configData.debug) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({attacker?.displayName}) to {entity?.ShortPrefabName}({attacked?.displayName}){weapon}");
        //if (configData.debug) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({(info?.Initiator as BasePlayer)?.displayName}) to {entity?.GetType()}({attacked?.displayName}){weapon}");
        if (configData.debug) Utils.DoLog($"DamageCheck for {info?.Initiator?.GetType()}({attacker?.displayName}) to {entity?.GetType()}({attacked?.displayName}){weapon}");
        if (attacked != null)
        {
            // Attacked is a player, but are they a real player and a friend, etc.
            if (attacker != null && attacked?.userID != attacker?.userID && !isFriend)// && attacked?.userID > 76560000000000000L)
            {
                if (attacked?.userID < 76560000000000000L)
                {
                    if (configData.allowDamageToNPC)
                    {
                        Utils.DoLog($"Allowing PVP damage by {attacker?.displayName}{weapon} to NPC");
                        return;
                    }
                    Utils.DoLog($"Blocking PVP damage by {attacker?.displayName}{weapon} to NPC");
                    info.damageTypes.ScaleAll(0);
                    return;
                }

                // Attacker is a player
                if (attacker.IsAdmin && configData.allowAdminPVP)
                {
                    if (configData.debug) Utils.DoLog($"Allowing admin damage by {attacker?.displayName}{weapon} to '{attacked?.displayName}'");
                    return;
                }
                if (attacker.userID > 76560000000000000L)
                {
                    Utils.DoLog($"Blocking PVP damage by {attacker?.displayName}{weapon} to '{attacked?.displayName}'");
                    if (te is TimedExplosive) te?.Kill();
                    info.damageTypes.ScaleAll(0);
                }
            }
            else if (!(info?.Initiator is BasePlayer) && !isFriend && attacker.userID > 76560000000000000L)
            {
                // Attacker is not a player
                Utils.DoLog($"Blocking PVP damage by {info?.Initiator?.GetType()}{weapon} to '{attacked?.displayName}'");
                if (te is TimedExplosive) te?.Kill();
                info.damageTypes.ScaleAll(0);
            }
        }
        else if (attacker != null && entity?.OwnerID != attacker?.userID && entity?.OwnerID != 0 && !isFriend)
        {
            // Attacker is a player, attacked is null, but victim is entity
            BasePlayer owner = BasePlayer.Find(entity?.OwnerID.ToString());
            if (attacker.IsAdmin && configData.allowAdminPVP)
            {
                if (configData.debug) Utils.DoLog($"Allowing admin damage by {attacker?.displayName}{weapon} to '{entity?.ShortPrefabName}' owned by {owner?.displayName}");
                return;
            }
            if (attacker.userID > 76560000000000000L)
            {
                Utils.DoLog($"Blocking PVP damage by {attacker?.displayName}{weapon} to '{entity?.ShortPrefabName}' owned by {owner?.displayName}");
                if (te is TimedExplosive) te?.Kill();
                info.damageTypes.ScaleAll(0);
            }
        }
        else if (entity?.OwnerID != info?.Initiator?.OwnerID && entity?.OwnerID != 0 && info?.Initiator?.OwnerID != 0 && !isFriend2)
        {
            // Attacker is an owned entity and victim is an owned entity
            BasePlayer owner = BasePlayer.Find(entity?.OwnerID.ToString());
            BasePlayer attackr = BasePlayer.Find(info?.Initiator?.OwnerID.ToString());
            if (attackr != null && owner != null && attackr.IsAdmin && configData.allowAdminPVP)
            {
                if (configData.debug) Utils.DoLog($"Allowing admin damage by {attackr?.displayName}{weapon} to '{entity?.ShortPrefabName}' owned by {owner?.displayName}");
                return;
            }
            if (attacker.userID > 76560000000000000L)
            {
                Utils.DoLog($"Blocking PVP damage from {info?.Initiator?.ShortPrefabName} owned by {attackr?.displayName}{weapon} to '{entity?.ShortPrefabName}'");// owned by {owner?.displayName}");
                if (te is TimedExplosive) te?.Kill();
                info.damageTypes.ScaleAll(0);
            }
        }
    }
}
//if (entity != null && hitInfo.damageTypes.Has(DamageType.Decay) && (configData.blockBuildingDecay || configData.blockDeployableDecay))
//{
//    if (configData.debug) Utils.DoLog($"Decay called for {entity.GetType().Name}({entity.ShortPrefabName})");
//    {
//        if ((entity is BuildingBlock) && entity?.OwnerID != 0 && configData.blockBuildingDecay)
//        {
//            if (configData.debug) Utils.DoLog($"Blocking building block decay on {entity?.ShortPrefabName}");
//            // Scale amount
//            hitInfo.damageTypes.Scale(DamageType.Decay, 0);
//            //return true;
//        }
//        else if (entity?.OwnerID != 0 && configData.blockDeployableDecay)
//        {
//            if (configData.debug) Utils.DoLog($"Blocking deployable decay on {entity?.ShortPrefabName}");
//            // Scale amount
//            hitInfo.damageTypes.Scale(DamageType.Decay, 0);
//            //return true;
//        }
//    }
//}
