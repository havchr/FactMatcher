using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using FactMatcher;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FactMatcherJobSystem : MonoBehaviour
{
    public RulesDB ruleDB;
    private NativeArray<float> _factValues;
    private NativeArray<FMRule> _rules;
    private NativeArray<RuleAtom> _ruleAtoms;
    
    private void Start()
    {
        ruleDB.InitRuleDB();
        _factValues = new NativeArray<float>(FactMatcherCodeGenerator.CountNumberOfFacts(this.ruleDB),Allocator.Persistent);
        FactMatcher.Functions.CreateNativeRules(this.ruleDB, out _rules, out _ruleAtoms);
    }

    public float this[int i]
    {
        get { return _factValues[i]; }
        set { _factValues[i] = value; }
    }

    public int StringID(string str)
    {
        return ruleDB.StringId(str);
    }

    public RuleDBEntry PickBestRule()
    {
        Debug.Log($"Natively! We now have {_rules.Length} rules and in total {_ruleAtoms.Length} atoms");
        var bestRule = new NativeArray<FactMatcher.FMRule>(1,Allocator.Persistent);
        var bestRuleMatches = new NativeArray<int>(1,Allocator.Persistent);
        
        var job = new FactMatcherMatch() 
        {
            FactValues = _factValues,
            RuleAtoms = _ruleAtoms,
            Rules = _rules,
            BestRule = bestRule,
            BestRuleMatches = bestRuleMatches
        };
        var sw = new Stopwatch();
        sw.Start();
        job.Schedule().Complete();
        sw.Stop();

        RuleDBEntry rule = ruleDB.RuleFromID(bestRule[0].ruleFiredEventId);
        if (rule != null)
        {
            Debug.Log($"The result of the best match is: {rule.payload} with {bestRuleMatches[0]} matches and it took {sw.ElapsedMilliseconds} ms");
            
            //Handle fact writes.
            foreach (var factWrite in rule.factWrites)
            {
                switch (factWrite.writeMode)
                {
                    
                    case RuleDBFactWrite.WriteMode.IncrementValue:
                        Debug.Log($"increment value {factWrite.writeValue} to fact {factWrite.factName} with factID {factWrite.factID} , was {_factValues[factWrite.factID]}");
                        _factValues[factWrite.factID] += factWrite.writeValue;
                        Debug.Log($"increment value {factWrite.writeValue} to fact {factWrite.factName} with factID {factWrite.factID} , was {_factValues[factWrite.factID]}");
                        break;
                    case RuleDBFactWrite.WriteMode.SubtractValue:
                        Debug.Log($"subtracting value {factWrite.writeValue} from fact {factWrite.factName} with factID {factWrite.factID} , was {_factValues[factWrite.factID]}");
                        _factValues[factWrite.factID] -= factWrite.writeValue;
                        Debug.Log($"subtracting value {factWrite.writeValue} from fact {factWrite.factName} with factID {factWrite.factID} , became {_factValues[factWrite.factID]}");
                        break;
                    case RuleDBFactWrite.WriteMode.SetValue:
                        Debug.Log($"Writing value {factWrite.writeValue} into fact {factWrite.factName} with factID {factWrite.factID}");
                        _factValues[factWrite.factID] = factWrite.writeValue;
                        break;
                    case RuleDBFactWrite.WriteMode.SetString:
                        Debug.Log($"Writing String {factWrite.writeString} into fact {factWrite.factName} with factID {factWrite.factID}");
                        _factValues[factWrite.factID] = ruleDB.StringId(factWrite.writeString);
                        break;
                } 
            }
        }
        Debug.Log($"The result of the best match is: {bestRule[0].ruleFiredEventId} with {bestRuleMatches[0]} matches and it took {sw.ElapsedMilliseconds} ms");

        bestRule.Dispose();
        bestRuleMatches.Dispose();
        return rule;
    }
    
    private void OnDestroy()
    {
        _factValues.Dispose();
        _rules.Dispose();
    }

    #if FACTMATCHER_BURST
    [BurstCompile(CompileSynchronously = true)]
#endif
    private struct FactMatcherMatch : IJob
    {

        [ReadOnly] public NativeArray<float> FactValues;

        [ReadOnly] public NativeArray<FactMatcher.FMRule> Rules;

        [ReadOnly] public NativeArray<FactMatcher.RuleAtom> RuleAtoms;

        [WriteOnly] public NativeArray<FactMatcher.FMRule> BestRule;
        [WriteOnly] public NativeArray<int> BestRuleMatches;


        public void Execute()
        {

            float factValue = 0;
            int ruleI = 0;
            int currentBestMatch = 0;
            int bestRuleIndex = -1;

            //Debug.Log($"Natively! We have {Rules.Length} rules to loop");
            for (ruleI = 0; ruleI < Rules.Length; ruleI++)
            {
                var rule = Rules[ruleI];
                int howManyAtomsMatch = 0;
                if (rule.numOfAtoms > currentBestMatch)
                {


                    //Debug.Log($"for rule {ruleI} with ruleFireID {rule.ruleFiredEventId} we are checking atoms from {rule.atomIndex} to {rule.atomIndex + rule.numOfAtoms} ");
                    for (int j = rule.atomIndex; j < (rule.atomIndex + rule.numOfAtoms); j++)
                    {
                        var atom = RuleAtoms[j];
                        //Debug.Log($"for rule {ruleI} with ruleFireID {rule.ruleFiredEventId} , comparing factID {atom.factID} with value {FactValues[atom.factID]} with atom.compare.lowerBound {atom.compare.lowerBound} and upperBound {atom.compare.upperBound} ");
                        if (FactMatcher.Functions.predicate(in atom.compare, FactValues[atom.factID]))
                        {
                            howManyAtomsMatch++;
                        }
                        else if (atom.strict)
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

    }

    static FactMatcher.FMRule emptyRule = new FactMatcher.FMRule(-1,0,0);
}
