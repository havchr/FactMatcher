using Unity.Collections;

namespace FactMatcher_RulesDBExample_Gen
{
    public static class RuleIDs
    {
    	public const int RuleName = 0;
    	public const int RuleName_Derived = 1;
    }

    public static class playerFacts
    {
        public const int age = 0;
        public const int name = 1;
        public const int height = 2;
    }

    public static class userFacts
    {
        public const int loginAttempt = 3;
        public const int name = 4;
    }

    public static class FactMatcherData
    {
        public static NativeArray<float> factValues = new NativeArray<float>(5,Allocator.Persistent);
    }
}
