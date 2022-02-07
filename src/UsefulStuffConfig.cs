namespace UsefulStuff
{
    public class UsefulStuffConfig
    {
        public static UsefulStuffConfig Loaded { get; set; } = new UsefulStuffConfig();

        //Shield
        public float ShieldDownAfterAttack { get; set; } = 0.25f;

        public float ShieldFatigueMultiplier { get; set; } = 1f;

        public float ShieldRecoveryRate { get; set; } = 3f;
        //Sluice Settings
        public float SluiceEfficiency { get; set; } = 1f;

        public float SluiceSiftTime { get; set; } = 0.25f;

        public int SluiceSiftPerBlock { get; set; } = 1;

        public bool SlucieGiveRocks { get; set; } = true;

        //Climbing Pick
        public int ClimbingPickDamageRate { get; set; } = 100;

        public bool ClimbingPickDisabledInProtected { get; set; } = true;

        //Glider
        public int GliderDamageRate { get; set; } = 100;

        public double GliderDescentRModifier { get; set; } = 0.75;

        public double GliderMaxStall { get; set; } = 0.6;

        public double GliderMinStall { get; set; } = -0.7;

        public double GliderThrustModifier { get; set; } = 0.02;

        public double GliderWindPushModifier { get; set; } = 0.005;

        public double GliderBackwardsAt { get; set; } = 0.2;

        public bool GliderNoCaveDiving { get; set; } = true;

        //Quenching
        public string[] QuenchBonusMats { get; set; } = { "iron", "steel", "meteoriciron" };

        public float QuenchBonusMult { get; set; } = 0.2f;

        //Tent Settings
        public int TentRadius { get; set; } = 3;

        public int TentHeight { get; set; } = 3;

        public bool TentKeepContents { get; set; } = false;

        public float TentBuildEffort { get; set; } = 100f;

        //Pot Kiln settings

        public double PotKilnBurnHours { get; set; } = 12;

        //Chisel Bench

        public int ChiselBenchRestoreCost { get; set; } = 10;


        #region Control Content

        public bool ShieldsEnabled { get; set; } = true;

        public bool ClimbingRopeEnabled { get; set; } = true;

        public bool ClimbingPickEnabled { get; set; } = true;

        public bool SluiceEnabled { get; set; } = true;

        public bool TentbagEnabled { get; set; } = true;

        public bool GliderEnabled { get; set; } = true;

        public bool LanternClipOnEnabled { get; set; } = true;

        public bool FireArrowEnabled { get; set; } = true;

        public bool ExplosiveArrowEnabled { get; set; } = true;

        public bool CardiacArrowEnabled { get; set; } = true;

        public bool TranqArrowEnabled { get; set; } = true;

        public bool BeenadeArrowEnabled { get; set; } = true;

        public bool QuenchEnabled { get; set; } = true;

        public bool OmnichuteEnabled { get; set; } = true;

        public bool PotKilnEnabled { get; set; } = true;

        public bool RockCrushingEnabled { get; set; } = true;

        public bool ToolRecyclingEnabled { get; set; } = true;

        public bool ClothesRecyclingEnabled { get; set; } = true;

        public bool ShearDecorEnabled { get; set; } = true;

        public bool ChiselBenchEnabled { get; set; } = true;

        public bool TradersEnabled { get; set; } = true;

        public bool GasMaskEnabled { get; set; } = true;
        #endregion
    }
}
