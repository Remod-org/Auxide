using Auxide;
using Rust;

public class HNoDecay : RustScript
{
    private static VersionNumber Version = new VersionNumber(1, 0, 1);
    private static ConfigData configData;

    public class ConfigData
    {
        public bool debug;
        public bool blockBuildingDecay;
        public bool blockDeployableDecay;
    }

    //static void Main()
    //{
    //    //Utils.DoLog("HNoDecay loading...");
    //}

    public override void Initialize()
    {
        LoadConfig();
    }

    public override void Dispose()
    {
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