using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FactMatcher;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public enum FactValueType
{
    String,Value	
}

#if USING_ODIN
using Sirenix.OdinInspector;
using Sirenix.Utilities;
#endif


[Serializable]
public class RuleDBFactWrite
{
    
    public int factID;
    public string factName;
    public string writeString;
    public float writeValue;
    public WriteMode writeMode = WriteMode.SetString;

    public enum WriteMode
    {
        SetString,SetValue,IncrementValue,SubtractValue
    }
    
}

[Serializable]
public class RuleDBAtomEntry
{
    public int factID;
    public string factName;
    public string matchString;
    public float matchValue;
    public Comparision compareMethod;
    public FactValueType compareType;

    public enum Comparision
    {
        Equal,NotEqual,LessThan,MoreThan,LessThanEqual,MoreThanEqual,Range
    }

    public FactMatcher.RuleCompare CreateCompare(RulesDB rules)
    {
        //used in the fact system 
        var val = compareType == FactValueType.String ?  rules.StringId(matchString) : matchValue;
        switch (compareMethod)
        {
            case Comparision.Equal:
                return FactMatcher.RuleCompare.Equals(val);
            case Comparision.NotEqual:
                return FactMatcher.RuleCompare.NotEquals(val);
            case Comparision.LessThan:
                return FactMatcher.RuleCompare.LessThan(val);
            case Comparision.LessThanEqual:
                return FactMatcher.RuleCompare.LessThanEquals(val);
            case Comparision.MoreThan:
                return FactMatcher.RuleCompare.MoreThan(val);
            case Comparision.MoreThanEqual:
                return FactMatcher.RuleCompare.MoreThanEquals(val);
        }
        return FactMatcher.RuleCompare.Equals(val);
    }
}

[Serializable]
public class RuleDBEntry
{
    public string ruleName;
    public string payload;
    public List<RuleDBFactWrite> factWrites;
    public List<RuleDBAtomEntry> atoms;
}

[CreateAssetMenu(fileName = "RulesDB", menuName = "FactMatcher/RulesDB", order = 1)]
public class RulesDB : 
    #if USING_ODIN
    SerializedScriptableObject
#else
ScriptableObject
#endif
{

    [MenuItem("Assets/Create/FactMatcher/RuleScript")]
    private static void CreateRuleScript(MenuCommand command)
    {

        var fileName = "ruleScript_";
        var dirPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var path = dirPath +"/" + fileName ;
        var assets = AssetDatabase.FindAssets(fileName,new []{dirPath});
        int fileNumber = 1;
        foreach (var assetGUID in assets)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            Debug.Log(assetPath);

            var splitted = assetPath.Split(new []{fileName},StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < splitted.Length; i++)
            {
                Debug.Log($"split {i} is {splitted[i]}");
            }
            var number = splitted[1];
            if (splitted[1].Contains("."))
            {
                number = splitted[1].Split('.')[0];
                Debug.Log($"Number is {number}");
            }
            if(int.TryParse(number, out int potentialFileNumber))
            {
                if (potentialFileNumber > fileNumber)
                {
                    fileNumber = potentialFileNumber;
                }
            }

        }
        fileNumber++;
        var finalPath = path + $"{fileNumber}.txt";
        File.WriteAllText(Application.dataPath + finalPath.Split(new []{"Assets"},StringSplitOptions.RemoveEmptyEntries)[0] , GetDefaultRuleScriptContent());
        AssetDatabase.Refresh();

        string str = "";
        //AssetDatabase.CreateAsset(new , finalPath); 
        Debug.Log($"Pressed it! {path}");
		
    }

    static string GetDefaultRuleScriptContent()
    {
        var rule1 = ".RuleName\n" +
                    "player.age > 10\n" +
                    "player.name = Johnny Lemon\n" +
                    ":Response:\n" +
                    "rule matches if age is bigger than 10 and name is Johnny Lemon\n" +
                    ":End:\n\n\n";

        var comment = "-- A comment starts with -- , but, remark,it is not handled within the response block..\n" +
                      "-- Variables start with a namespace. If none is given, it will be automatically be given global as the namespace\n" +
                      "-- The variables defined here will end up in generated c# code in FactMatcherCodeGenerator.cs\n" +
                      "-- Everything is case-sensitive..\n" +
                      "-- Deriving another rule allows you to copy all the checks of that rule. See rule below for an example\n\n\n"; 
        
        var rule2 = ".RuleName.Derived\n" +
                    "player.height > 180\n" +
                    ":Response:\n" +
                    "rule matches if base rule .RuleName matches and height is bigger than 180\n" +
                    ":End:\n\n\n";

        return rule1 + comment + rule2;
    }

	
    #if USING_ODIN
    [Button("Generate From ruleScript")]
#endif
    private void GenerateFromText()
    {
        if (generateFrom != null)
        {
            rules.Clear();
            int factID = 0;
            Dictionary<string,int> addedFactIDS = new Dictionary<string, int>();
            foreach (var ruleScript in generateFrom)
            {
                var parser = new RuleScriptParser();
                parser.GenerateFromText(ruleScript.text,rules,ref factID,ref addedFactIDS); 
            }
            GenerateFactIDS();
        }
    }

    public List<TextAsset> generateFrom;
    public List<RuleDBEntry> rules;
    
    #if USING_ODIN
    [Button("Generate Fact IDS")]
#endif
    private void GenerateFactIDS()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var generatedName = StripNameIntoCamelCase(name);
        var fileName = $"FactMatcher/Generated/{generatedName}.cs";
        FactMatcherCodeGenerator.GenerateFactIDS(fileName, GetNameSpaceName(), this);
    }

    public string GetNameSpaceName()
    {
        return $"FactMatcher_{StripNameIntoCamelCase(name)}_Gen";
    }

        
    //turns a string like "John Apple banana.power" into "JohnAppleBananaPower"
    public static string StripNameIntoCamelCase(string name)
    {
        
        var stripEm = new []{' ', '.'};
        StringBuilder genName = new StringBuilder();
        foreach (var strippy in stripEm)
        {
            genName.Clear();
            var splitted = name.Split(strippy);
            foreach (var s in splitted)
            {
                genName.Append(s.Substring(0, 1).ToUpper());
                genName.Append(s.Substring(1, s.Length - 1));
            }

            name = genName.ToString();
        }

        return name;
    }

    public bool generateSwitch = false;

    private void OnValidate()
    {
        if (generateSwitch)
        {
            generateSwitch = false;
            GenerateFromText();
        }
    }

    private Dictionary<string, int> RuleStringMap;
    
    private Dictionary<int, RuleDBEntry> RuleMap;

    public void InitRuleDB()
    {
        RuleStringMap = RuleStringIDs();
        RuleMap = CreateEntryFromIDDic();
    }
    public int StringId(string str)
    {
        if (RuleStringMap == null)
        {
            InitRuleDB();
        }
        int id = -1;
        if (!RuleStringMap.TryGetValue(str, out id))
        {
            id = -1;
        }
        Debug.Log($"For string {str} we give the id {id}");
        return id;
    }
    
    //Todo - better way to access RuleFromID since we now just store everything in a dictionary ... ? 
    //This is not exactly elegant
    public RuleDBEntry RuleFromID(int id)
    {
        if (RuleMap == null)
        {
            InitRuleDB();
        }

        RuleDBEntry rule;
        if (!RuleMap.TryGetValue(id, out rule))
        {
            rule = null;
        }
        return rule;
    }

    public static Dictionary<int, RuleDBEntry> CreateEntryFromIDDic()
    {
        Dictionary<int,RuleDBEntry>  dic = new Dictionary<int,RuleDBEntry>();
        var rulesDB =  Resources.LoadAll<RulesDB>("");

        foreach (var db in rulesDB)
        {
            foreach (var rule in db.rules)
            {
                var id = FactMatcherCodeGenerator.RuleIDReflection(rule,db.GetNameSpaceName());
                dic[id] = rule;

            }
        }
        return dic;
    }
    public static Dictionary<string, int> RuleStringIDs()
    {
        Dictionary<string, int>  dic = new Dictionary<string, int>();
        var rulesDB =  Resources.LoadAll<RulesDB>("");

        int id = 0;
        foreach (var db in rulesDB)
        {
            foreach (var rule in db.rules)
            {
                foreach (var factWrite in rule.factWrites)
                {
                    if (factWrite.writeMode == RuleDBFactWrite.WriteMode.SetString 
                    && factWrite.writeString!=null && factWrite.writeString.Length>=1)
                    {
                        
                        if (!dic.ContainsKey(factWrite.writeString))
                        {
                            dic[factWrite.writeString] = id;
                            id++;
                        }
                    }
                }

                foreach (var atom in rule.atoms)
                {
                    if (atom.compareType == FactValueType.String && atom.matchString!=null && atom.matchString.Length>=1)
                    {
                        if (!dic.ContainsKey(atom.matchString))
                        {
                            dic[atom.matchString] = id;
                            id++;
                        }
                    }
                }
            }
        }
        return dic;
    }
	
}