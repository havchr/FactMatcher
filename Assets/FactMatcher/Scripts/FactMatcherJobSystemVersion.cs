using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using UnityEditor.VersionControl;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class FactMatcherJobSystemVersion : MonoBehaviour
{

    public RulesDB rules;
    public Text text;
    private NativeArray<float> factValues;

    private void Start()
    {
        RulesDB.InitAllRuleDB();
        factValues = new NativeArray<float>(FactMatcherCodeGenerator.CountNumberOfFacts(this.rules),Allocator.Persistent);
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
        //Todo - the testcase should be decent..
        //Todo - seperate out the Job from the example specific parts..
        factValues[FactMatcher_RulesDBExample_Gen.userFacts.name] = RulesDB.StringId(name);

        FactMatcher.Functions.CreateNativeRules(this.rules, out var rules, out var ruleAtoms);
        Debug.Log($"Natively! We now have {rules.Length} rules and in total {ruleAtoms.Length} atoms");
        var bestRule = new NativeArray<FactMatcher.FMRule>(1,Allocator.Persistent);
        var bestRuleMatches = new NativeArray<int>(1,Allocator.Persistent);
        
        var job = new MyJob
        {
            FactValues = factValues,
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

    private void OnDestroy()
    {
        factValues.Dispose();
    }
    
    // Using BurstCompile to compile a Job with Burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    #if BURST_ON
    [BurstCompile(CompileSynchronously = true)]
#endif
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
                        if (FactMatcher.Functions.predicate(in atom.compare,FactValues[atom.factID] ))
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