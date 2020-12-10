using Unity.Collections;

namespace FactMatcher_RulesDBExample_Gen
{
    public static class RuleIDs
    {
    	public const int Welcome = 0;
    	public const int WelcomeLemon = 1;
    	public const int WelcomeLemon_Cold = 2;
    	public const int WelcomeLemon_ColdRainNorway = 3;
    	public const int WelcomeLemon_ColdRainWindy = 4;
    	public const int WelcomeLemon_ColdRainWindy_Thunderstorm = 5;
    	public const int WelcomeLemon_ColdRainWindy_DistantThunder = 6;
    	public const int WelcomeLemon_WarmThunderstormYoung = 7;
    	public const int WelcomeLemon_WarmThunderstormOld = 8;
    	public const int LemonSteals = 9;
    	public const int LemonPotSmash = 10;
    	public const int LemonPotSmash_Cont2 = 11;
    	public const int LemonPotSmash_Cont3 = 12;
    	public const int LemonPotSmash_Cont4 = 13;
    	public const int LemonPotSmash_smallPayout = 14;
    	public const int LemonPotSmash_smallPayout2 = 15;
    	public const int LemonPotSmash_mediumPayout = 16;
    	public const int LemonPotSmash_BigPayout = 17;
    	public const int LemonPotSmash_GigaPayout = 18;
    	public const int LemonPotSmash_BigPayout_Cont4 = 19;
    	public const int LemonDeath = 20;
    	public const int LemonDeath_Old = 21;
    	public const int LemonDeath_Young = 22;
    }

    public static class playerFacts
    {
        public const int concept = 0;
        public const int name = 1;
        public const int age = 7;
    }

    public static class weatherFacts
    {
        public const int temperature = 2;
        public const int rainMs = 4;
        public const int windMs = 5;
        public const int thunderPerSecond = 6;
    }

    public static class GlobalFacts
    {
        public const int currentMap = 3;
    }

    public static class commentatorFacts
    {
        public const int youngAgeCommented = 8;
        public const int oldAgeCommented = 9;
        public const int potSmashCount = 11;
    }

    public static class potsmashFacts
    {
        public const int rupeesInLastPot = 10;
    }

    public static class FactMatcherData
    {
        public static NativeArray<float> CreateFactValues()
    {
        return new NativeArray<float>(12,Allocator.Persistent);
    }
    }
}
