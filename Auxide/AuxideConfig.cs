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
            public bool useInternalCompiler;
            public bool useWestWindCompiler;
            public bool disableTCWarning;
            public AuxideMinimal minimal;
        }

        public class AuxideMinimal
        {
            public bool blockTCMenu;
            public bool allowPVP;
            public bool allowAdminPVP;
            public bool blockBuildingDecay;
            public bool blockDeployablesDecay;
            public bool protectLoot;
            public bool protectMount;
        }

        public AuxideConfig(string filename) : base(filename)
        {
            Options = new AuxideOptions()
            {
                full = false,
                verbose = false,
                useInternalCompiler = true,
                disableTCWarning = false,
                minimal = new AuxideMinimal()
                {
                    blockTCMenu = false,
                    allowPVP = true,
                    allowAdminPVP = true,
                    blockBuildingDecay = false,
                    blockDeployablesDecay = false,
                    protectLoot = false,
                    protectMount = false
                }
            };
        }
    }
}
