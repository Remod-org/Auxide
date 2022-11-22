using Auxide;
using Rust;

public class HNoDecay : RustScript
{
    private static ConfigData configData;

    public HNoDecay()
    {
        Author = "RFC1920";
        Version = new VersionNumber(1, 0, 2);
    }

    public class ConfigData
    {
        public bool debug;
        public bool blockBuildingDecay;
        public bool blockDeployableDecay;
        public bool blockWoodFromTC;
        public bool blockStoneFromTC;
        public bool blockFragsFromTC;
        public bool blockMetalFromTC;
    }

    public override void Initialize()
    {
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
            blockBuildingDecay = true,
            blockDeployableDecay = true
        };

        config.WriteObject(configData);
    }

    //private ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
    //{
    //    BaseEntity cup = container?.entityOwner;
    //    if (cup == null) return null;
    //    if (!(cup is BuildingPrivlidge)) return null;

    //    if (!(configData.blockWoodFromTC || configData.blockStoneFromTC || configData.blockFragsFromTC || configData.blockMetalFromTC)) return null;

    //    string res = item?.info?.shortname;
    //    if (
    //        (res.Equals("wood") && configData.blockWoodFromTC)
    //        || (res.Equals("stones") && configData.blockStoneFromTC)
    //        || (res.Equals("metal.fragments") && configData.blockFragsFromTC)
    //        || (res.Equals("metal.refined") && configData.blockMetalFromTC))
    //    {
    //        if (configData.debug) Utils.DoLog($"Player blocked from adding {res} to a cupboard!");
    //        return ItemContainer.CanAcceptResult.CannotAcceptRightNow;
    //    }

    //    return null;
    //}

    public object OnTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
    {
        if (hitInfo == null) return null;
        if (entity != null && hitInfo.damageTypes.Has(DamageType.Decay) && (configData.blockBuildingDecay || configData.blockDeployableDecay))
        {
            if (configData.debug) Utils.DoLog($"Decay called for {entity.GetType().Name}({entity.ShortPrefabName})");
            {
                if ((entity is BuildingBlock) && entity?.OwnerID != 0 && configData.blockBuildingDecay)
                {
                    if (configData.debug) Utils.DoLog($"Blocking building block decay on {entity?.ShortPrefabName}");
                    // Scale amount
                    hitInfo.damageTypes.Scale(DamageType.Decay, 0);
                    //return true;
                }
                else if (entity?.OwnerID != 0 && configData.blockDeployableDecay)
                {
                    if (configData.debug) Utils.DoLog($"Blocking deployable decay on {entity?.ShortPrefabName}");
                    // Scale amount
                    hitInfo.damageTypes.Scale(DamageType.Decay, 0);
                    //return true;
                }
            }
        }
        return null;
    }
}