﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FactMatcher
{
    public class RuleScriptParser  
    {
        
        public enum RuleScriptParserEnum
        {
            ParsingRule,ParsingResponse,ParsingFactWrite,LookingForRule
        }
        public void GenerateFromText(string text,List<RuleDBEntry> rules)
        {

            RuleScriptParserEnum state = RuleScriptParserEnum.LookingForRule;
            Dictionary<string,List<RuleDBAtomEntry>> parsedAtoms = new Dictionary<string, List<RuleDBAtomEntry>>();
            RuleDBEntry currentRule = null;
            StringBuilder payload = new StringBuilder();
            using (System.IO.StringReader reader = new System.IO.StringReader(text))
            {
                string line;
                while ( (line = reader.ReadLine()) !=null)
                {
                    //-- in ruleScript is a comment , except if we are parsing a response..
                    if (state != RuleScriptParserEnum.ParsingResponse)
                    {
                        if (line.StartsWith("--"))
                        {
                            continue;
                        }
                    }
	            
                    if (state == RuleScriptParserEnum.LookingForRule)
                    {
                        if (line.Length > 0 && line[0] == '.')
                        {
                            //Todo - Rules should start with .
                            //Should give error if not the case... 
                            state = RuleScriptParserEnum.ParsingRule;
                            var ruleNames = line.Split( '.');
                            var finalName = new StringBuilder();
                            var derived = new StringBuilder("");
                            bool foundDerived = false;
                            int lastIndex = ruleNames.Length;
                            //This attempts to figure out if there is any thing to derive from by searching downwards.
                            //if we have NickHit.Allies and then we have a rule that is NickHit.Allies.NearDeath.BySandwhich
                            //We should first see if we can derive from NickHit.Allies.NearDeath , if no match is found.
                            //We should see if we can derive from NickHit.Allies ... and so on.
                            while (!foundDerived && lastIndex > 1)
                            {
                            
                                finalName.Clear(); 
                                derived.Clear();
                                for(int i=1; i < lastIndex; i++)
                                {
                                    finalName.Append($"{ruleNames[i]}");
                                    if (i < lastIndex - 1)
                                    {
                                        finalName.Append(".");
                                        derived.Append($"{ruleNames[i]}");
                                        if (i < lastIndex - 2)
                                        {
                                            derived.Append(".");
                                        }
                                    }

                                }

                                if (derived.Length > 0)
                                {
                                    if (parsedAtoms.ContainsKey(derived.ToString()))
                                    {
                                        foundDerived = true;
                                    }
                                }
                                lastIndex--;
                                Debug.Log($"derived is {derived} and foundDerived is {foundDerived} and lastIndex {lastIndex} and finalName is {finalName}");
                            }

                            currentRule = new RuleDBEntry();
                            currentRule.atoms = new List<RuleDBAtomEntry>(); 
                            currentRule.ruleName = finalName.ToString();
                            Debug.Log($"Adding atoms from derived {derived}");
                            //Grab atoms from derived
                            if (derived.Length > 0)
                            {
                                if (parsedAtoms.ContainsKey(derived.ToString()))
                                {
                                
                        
                                    foreach (var atom in parsedAtoms[derived.ToString()])
                                    {
                                
                                        Debug.Log($"for rule {currentRule.ruleName} - Adding atom {atom.factName} from derived {derived}");
                                        currentRule.atoms.Add(atom); 
                                    }
                                }
                            }
                            Debug.Log($"line is {line}");
                            Debug.Log($"FinalName is {finalName} and derived is {derived}");
					
                        }
                    }

                    if (state == RuleScriptParserEnum.ParsingRule)
                    {
                        ParseRuleAtoms(line, currentRule);
                        if (line.Trim().Contains(":FactWrite:"))
                        {
                            state = RuleScriptParserEnum.ParsingFactWrite;
                        }
                        if (line.Trim().Contains(":Response:"))
                        {
                            state = RuleScriptParserEnum.ParsingResponse;
                            payload.Clear();
                        }
                    
                    }
                    else if (state == RuleScriptParserEnum.ParsingFactWrite)
                    {

                        ParseFactWrites(line, currentRule);
                        if (line.Trim().Contains(":Response:"))
                        {
                            state = RuleScriptParserEnum.ParsingResponse;
                            payload.Clear();
                        }
                    }
                    else if (state == RuleScriptParserEnum.ParsingResponse)
                    {
                        if (line.Trim().Contains(":End:"))
                        {
                            //Store rule...
                            state = RuleScriptParserEnum.LookingForRule;
                            currentRule.payload = payload.ToString();
                            if (rules.Any(entry => entry.ruleName.Equals(currentRule.ruleName)))
                            {
                               Debug.LogError($"Allreay Contains a rule named {currentRule.ruleName} - will not add"); 
                            }
                            else
                            {
                                rules.Add(currentRule);
                            }
                        
                            //adding all our atoms into the parsedAtoms array for deriving to work..
                            foreach (var atomEntry in currentRule.atoms)
                            {
                                Debug.Log($"Adding atom {atomEntry.factName} to ParsedAtoms with key {currentRule.ruleName}");
                                if (!parsedAtoms.ContainsKey(currentRule.ruleName))
                                {
                                    parsedAtoms[currentRule.ruleName] = new List<RuleDBAtomEntry>();
                                }
                                parsedAtoms[currentRule.ruleName].Add(atomEntry);
                            }
                        
                        }
                        else
                        {
                            payload.Append(line);
                        }
                    }

                }
            }
        }
        
        private static void ParseFactWrites(string line, RuleDBEntry currentRule)
        {
            var operands = new List<(string, RuleDBFactWrite.WriteMode)>
            {
                ("+=", RuleDBFactWrite.WriteMode.IncrementValue),
                ("-=", RuleDBFactWrite.WriteMode.SubtractValue),
                ("=", RuleDBFactWrite.WriteMode.SetString),
                
            };
            //Operands with multiple characters must be matched prior to operands of lower character..
            //or else , >= would first be treated as > operand ... 
            operands.Sort((s, s1) => s1.Item1.Length - s.Item1.Length);
            foreach (var operand in operands)
            {
                if (line.Trim().Contains(operand.Item1))
                {
                    var splits = line.Split(new[] {operand.Item1}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var split in splits)
                    {
                        Debug.Log($"The split contains {split}");
                    }

                    if (splits.Length != 2)
                    {
                        Debug.LogError("Error case - Operand split trouble");
                    }
                    else
                    {
                        RuleDBFactWrite factWrite = new RuleDBFactWrite();
                        
                        factWrite.writeMode = operand.Item2;
                        
                        factWrite.factName = splits[0].Trim();
                        var valueMatch = splits[1].Trim();
                        if (float.TryParse(valueMatch, out float floatVal))
                        {
                            factWrite.writeValue = floatVal;
                            if (factWrite.writeMode == RuleDBFactWrite.WriteMode.SetString)
                            {
                                factWrite.writeMode = RuleDBFactWrite.WriteMode.SetValue;
                            }
                        }
                        else
                        {

                            if (factWrite.writeMode != RuleDBFactWrite.WriteMode.SetString)
                            {
                               Debug.LogError("Using wrong operand for string. Only = supported"); 
                            }
                            factWrite.writeString = valueMatch;
                        }


                        if (currentRule.factWrites == null)
                        {
                            currentRule.factWrites = new List<RuleDBFactWrite>();
                        }
                        currentRule.factWrites.Add(factWrite);
                    }

                    break;
                }
            }
        }
        
        private static void ParseRuleAtoms(string line, RuleDBEntry currentRule)
        {
            var operands = new List<(string, RuleDBAtomEntry.Comparision)>
            {
                (">", RuleDBAtomEntry.Comparision.MoreThan),
                (">=", RuleDBAtomEntry.Comparision.MoreThanEqual),
                ("<", RuleDBAtomEntry.Comparision.LessThan),
                ("<=", RuleDBAtomEntry.Comparision.LessThanEqual),
                ("=", RuleDBAtomEntry.Comparision.Equal),
                ("!", RuleDBAtomEntry.Comparision.NotEqual)
            };
            //Operands with multiple characters must be matched prior to operands of lower character..
            //or else , >= would first be treated as > operand ... 
            operands.Sort((s, s1) => s1.Item1.Length - s.Item1.Length);
            foreach (var operand in operands)
            {
                if (line.Trim().Contains(operand.Item1))
                {
                    var splits = line.Split(new[] {operand.Item1}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var split in splits)
                    {
                        Debug.Log($"The split contains {split}");
                    }

                    if (splits.Length != 2)
                    {
                        Debug.LogError("Error case - Operand split trouble");
                    }
                    else
                    {
                        //Todo add support for strict
                        RuleDBAtomEntry atomEntry = new RuleDBAtomEntry();
                        atomEntry.compareMethod = operand.Item2;
                        atomEntry.factName = splits[0].Trim();
                        var valueMatch = splits[1].Trim();
                        atomEntry.compareType = FactValueType.String;
                        if (float.TryParse(valueMatch, out float floatVal))
                        {
                            atomEntry.matchValue = floatVal;
                            atomEntry.compareType = FactValueType.Value;
                        }
                        else
                        {
                            atomEntry.matchString = valueMatch;
                        }


                        //If we have added a rule - from derived - but are overwriting that fact-query , then delete the 
                        //derived rule , or else we would get two conflicting rule atoms in our rule...
                        if (currentRule.atoms.Any(entry => entry.factName.Equals(atomEntry.factName)))
                        {
                            currentRule.atoms.Remove(currentRule.atoms.Find(entry =>
                                entry.factName.Equals(atomEntry.factName)));
                        }

                        currentRule.atoms.Add(atomEntry);
                    }

                    break;
                }
            }
        }
    }
}