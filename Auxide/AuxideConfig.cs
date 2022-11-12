using Auxide.Scripting;

namespace Auxide
{
    public class AuxideConfig : ConfigFile
    {
        public AuxideOptions Options { get; set; }

        public class AuxideOptions
        {
            public bool full; // If false, additonally use settings in minimal
            public bool verbose;
            //public bool cSharpScripts;
            //public bool useInternalCompiler;
            //public bool useWestWindCompiler;
            public bool disableTCWarning;
            public bool hideGiveNotices;
            public AuxideMinimal minimal;
        }

        public class AuxideMinimal
        {
            public bool blockTCMenu { get; set; }
            public bool allowPVP { get; set; }
            public bool allowAdminPVP { get; set; }
            public bool blockBuildingDecay { get; set; }
            public bool blockDeployablesDecay { get; set; }
            public bool protectLoot { get; set; }
            public bool protectCorpse { get; set; }
            public bool protectSleeper { get; set; }
            public bool protectMount { get; set; }
            public bool allowDamageToNPC { get; set; }
        }

        public AuxideConfig(string filename) : base(filename)
        {
            Options = new AuxideOptions()
            {
                full = false,
                verbose = false,
                //cSharpScripts = false,
                //useInternalCompiler = true,
                disableTCWarning = false,
                hideGiveNotices = false,
                minimal = new AuxideMinimal()
                {
                    blockTCMenu = false,
                    allowPVP = true,
                    allowAdminPVP = true,
                    blockBuildingDecay = false,
                    blockDeployablesDecay = false,
                    protectLoot = false,
                    protectCorpse = false,
                    protectSleeper = false,
                    protectMount = false,
                    allowDamageToNPC = true
                }
            };
        }
    }
}
