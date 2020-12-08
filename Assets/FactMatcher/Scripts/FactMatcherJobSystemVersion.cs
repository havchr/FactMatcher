#if JAPPPA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Agens;
using FactMatcherGenData;
//using FactMatcherGenData;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using UnityEditor.VersionControl;
using Debug = UnityEngine.Debug;

public class FactMatcherJobSystemVersion : MonoBehaviour
{

    public string who;
    public string concept;
    public string hitBy;
    public int nearAllies;
    public int health;
    public float swimDuration;
    public float temperature=13.0f;
    public string curMap;

    public bool nearAlisa;
    
    public int numFacts;
    public int numRules;

    public TextMeshProUGUI text;

    private void Start()
    {
        RulesDB.InitAllRuleDB();
    }

    // Start is called before the first frame update
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) )
        {
           Jazzo();
        }
    }
    

    public void Jazzo()
    {

        /*
         * Writing facts! 
         */
        FactMatcherData.factValues[PlayerFacts.who] = RulesDB.StringId(who);
        FactMatcherData.factValues[PlayerFacts.concept] = RulesDB.StringId(concept);
        FactMatcherData.factValues[PlayerFacts.hitBy] = RulesDB.StringId(hitBy);
        FactMatcherData.factValues[PlayerFacts.nearAllies] = nearAllies;
        FactMatcherData.factValues[PlayerFacts.health] = health;
        FactMatcherData.factValues[PlayerFacts.nearAlisa] = nearAlisa ? 1 : 0;
        FactMatcherData.factValues[GlobalFacts.curMap] = RulesDB.StringId(curMap);
        FactMatcherData.factValues[PlayerFacts.swimDurationSeconds] = swimDuration;
        FactMatcherData.factValues[WeatherFacts.temperature] = temperature;
        
        var rulesDB =  Resources.LoadAll<RulesDB>("");
        
        foreach (var db in rulesDB)
        {
            CreateNativeRules(db, out var rules, out var ruleAtoms);
            
            Debug.Log($"Natively! We now have {rules.Length} rules and in total {ruleAtoms.Length} atoms");
            var bestRule = new NativeArray<FactMatcher.FMRule>(1,Allocator.Persistent);
            var bestRuleMatches = new NativeArray<int>(1,Allocator.Persistent);
	
            var job = new MyJob
            {
                FactValues = FactMatcherData.factValues,
                RuleAtoms = ruleAtoms,
                Rules = rules,
                BestRule = bestRule,
                BestRuleMatches = bestRuleMatches
            };
            var sw = new Stopwatch();
            sw.Start();
            job.Schedule().Complete();
            sw.Stop();

            RuleDBEntry rule = RulesDB.RuleFromID(bestRule[0].ruleFiredEventId);
            if (rule != null)
            {
                Debug.Log($"The result of the best match is: {rule.payload} with {bestRuleMatches[0]} matches and it took {sw.ElapsedMilliseconds} ms");
                text.text = rule.payload;
                //rule.
            }
            else
            {
                text.text = "No rule matched";
            }
            Debug.Log($"The result of the best match is: {bestRule[0].ruleFiredEventId} with {bestRuleMatches[0]} matches and it took {sw.ElapsedMilliseconds} ms");

            ruleAtoms.Dispose();
            rules.Dispose();
            bestRule.Dispose();
            bestRuleMatches.Dispose();
        }
        
    }

    private void OnDestroy()
    {
        FactMatcherData.factValues.Dispose();
    }

    private static void CreateNativeRules(RulesDB db, out NativeArray<FactMatcher.FMRule> rules, out NativeArray<FactMatcher.RuleAtom> ruleAtoms)
    {
        rules = new NativeArray<FactMatcher.FMRule>(db.rules.Count, Allocator.Persistent);

        int numOfAtoms = 0;
        foreach (var ruleDBEntry in db.rules)
        {
            foreach (var atom in ruleDBEntry.atoms)
            {
                numOfAtoms++;
            }
        }

        ruleAtoms = new NativeArray<FactMatcher.RuleAtom>(numOfAtoms, Allocator.Persistent);
        int atomIndex = 0;
        
            
        Debug.Log($"Creating native version of this many rules {db.rules.Count}");
        for (int i = 0; i < db.rules.Count; i++)
        {
            //Todo replace with real eventID

            Debug.Log($"Creating native version of rule {db.rules[i].ruleName} with {db.rules[i].atoms} atoms");
            var ruleNameField = FactMatcherCodeGenerator.GenRuleName(db.rules[i]);
            var ruleFiredEventID = (int) Type.GetType($"FactMatcherGenData.RuleIDs").GetField(ruleNameField).GetRawConstantValue();
            rules[i] = new FactMatcher.FMRule(ruleFiredEventID, atomIndex, db.rules[i].atoms.Count);
            foreach (var atom in db.rules[i].atoms)
            {
                Debug.Log($"For factName in atom {atom.factName} we try to extract the factID");
                string[] factNameSplit = atom.factName.Split('.');
                //FactMatcherGenData
                var factNamespace = "Global";
                var factName = factNameSplit[0];
                if (factNameSplit.Length >= 2)
                {
                    
                
                    Debug.Log($"Trying to get {factNameSplit[0]} and {factNameSplit[1]}");
                    factNamespace = factNameSplit[0];
                    factName= factNameSplit[1];
                }
                //todo not really an elegant way of doing this.. haha, at least since the generator chooses how names are generated..
                //at least the generator should be responsible for getting this back out.. 
                int factID = (int) Type.GetType($"FactMatcherGenData.{factNamespace}Facts").GetField(factName)
                    .GetRawConstantValue();
                ruleAtoms[atomIndex] = new FactMatcher.RuleAtom(factID, i, atom.CreateCompare());
                atomIndex++;
            }
        }
    }

    public void DoTheJazz()
    {


        FactMatcher.CreateMockRules(numFacts,numRules,out var rules,out var atoms, out var facts);
        var nativeRules = new NativeArray<FactMatcher.FMRule>(rules.Count, Allocator.Persistent);
        var nativeAtoms= new NativeArray<FactMatcher.RuleAtom>(atoms.Count, Allocator.Persistent);
        var nativeFacts = new NativeArray<float>(facts.Count, Allocator.Persistent);
        for (int i=0; i < nativeRules.Length; i++)
        {
            nativeRules[i] = rules[i];
        }
        for (int i=0; i < nativeAtoms.Length; i++)
        {
            nativeAtoms[i] = atoms[i];
        }
        for (int i=0; i < nativeFacts.Length; i++)
        {
            nativeFacts[i] = facts[i];
        }
        
        var bestRule = new NativeArray<FactMatcher.FMRule>(1,Allocator.Persistent);
        var bestRuleMatches = new NativeArray<int>(1,Allocator.Persistent);
    
        var job = new MyJob
        {
            FactValues = nativeFacts,
            RuleAtoms = nativeAtoms,
            Rules = nativeRules,
            BestRule = bestRule,
            BestRuleMatches = bestRuleMatches
        };
        var sw = new Stopwatch();
        sw.Start();
        job.Schedule().Complete();
        sw.Stop();
    
        Debug.Log($"The result of the best match is: {bestRule[0].ruleFiredEventId} with {bestRuleMatches[0]} matches and it took {sw.ElapsedMilliseconds} ms");
        //Debug.Log("The result of the sum is: " + output[0]);
        nativeFacts.Dispose();
        nativeRules.Dispose();
        nativeAtoms.Dispose();
        bestRule.Dispose();
        bestRuleMatches.Dispose();
    }
    
    
    // Using BurstCompile to compile a Job with Burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    [BurstCompile(CompileSynchronously = true)]
    private struct MyJob : IJob
    {
            
        [ReadOnly]
        public NativeArray<float> FactValues;
            
        [ReadOnly]
        public NativeArray<FactMatcher.FMRule> Rules;
        
        [ReadOnly]
        public NativeArray<FactMatcher.RuleAtom> RuleAtoms;
    
        [WriteOnly]
        public NativeArray<FactMatcher.FMRule> BestRule;
        [WriteOnly]
        public NativeArray<int> BestRuleMatches;

    
        public void Execute()
        {
                
            float factValue = 0;
            int ruleI = 0;
            int currentBestMatch = 0;
            int bestRuleIndex = -1;
                    
            Debug.Log($"Natively! We have {Rules.Length} rules to loop");
            for (ruleI = 0; ruleI < Rules.Length; ruleI++)
            {
                var rule = Rules[ruleI];
                int howManyAtomsMatch = 0;
                if (rule.numOfAtoms > currentBestMatch)
                {
                    
                        
                    Debug.Log($"for rule {ruleI} with ruleFireID {rule.ruleFiredEventId} we are checking atoms from {rule.atomIndex} to {rule.atomIndex + rule.numOfAtoms} ");
                    for (int j=rule.atomIndex; j < (rule.atomIndex + rule.numOfAtoms); j++)
                    {
                        var atom = RuleAtoms[j];
                        Debug.Log($"for rule {ruleI} with ruleFireID {rule.ruleFiredEventId} , comparing factID {atom.factID} with value {FactValues[atom.factID]} with atom.compare.lowerBound {atom.compare.lowerBound} and upperBound {atom.compare.upperBound} ");
                        if (FactMatcher.predicate(in atom.compare,FactValues[atom.factID] ))
                        {
                            howManyAtomsMatch++;
                        }else if (atom.strict)
                        {
                            howManyAtomsMatch = 0;
                            break;
                        }
                    
                    }
                    if (howManyAtomsMatch > currentBestMatch)
                    {
                        currentBestMatch = howManyAtomsMatch;
                        bestRuleIndex = ruleI;
                    }
                }
            }

            if (bestRuleIndex != -1)
            {
                BestRuleMatches[0] = currentBestMatch;
                BestRule[0] = Rules[bestRuleIndex];
            }
            else
            {
                BestRuleMatches[0] = -1;
                BestRule[0] = emptyRule;
            }
        }
        
        static FactMatcher.FMRule emptyRule = new FactMatcher.FMRule(-1,0,0);
    }
}
#endif