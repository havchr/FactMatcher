﻿using System;
using System.Collections.Generic;
using FactMatcher;
using UnityEngine;

public enum FactValueType
{
    String,Value	
}

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
    public int RuleID;
    public string ruleName;
    public string payload;
    public List<RuleDBFactWrite> factWrites;
    public List<RuleDBAtomEntry> atoms;
}

[CreateAssetMenu(fileName = "RulesDB", menuName = "FactMatcher/RulesDB", order = 1)]
public class RulesDB : ScriptableObject
{
    
    private Dictionary<string, int> RuleStringMap;
    private Dictionary<int, RuleDBEntry> RuleMap;
    public List<TextAsset> generateFrom;
    public List<RuleDBEntry> rules;
    
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
        return id;
    }
    
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
                var id = rule.RuleID;
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
    
	public int CountNumberOfFacts()
	{
		int topFactID = 0;
		foreach (var rule in rules)
		{

			foreach (var atom in rule.atoms)
			{
				if (atom.factID > topFactID)
				{
					topFactID = atom.factID;
				}
			}
			
			foreach (var factWrite in rule.factWrites)
			{
				if (factWrite.factID > topFactID)
				{
					topFactID = factWrite.factID;
				}
			}
		}
		return topFactID + 1;
	}
    
	
}