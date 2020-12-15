using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FactMatcherExample : MonoBehaviour
{
    
    
    public Text text;
    public FactMatcherJobSystem factMatcher;
    public string currentMap;
    public string playerName;
    public string playerConcept;
    public int playerAge;
    public int rupeesInLastPot = 0;

    //Weather values..
    public int temperature;
    public float windMs;
    public float rainMs;
    public float thunderPerSecond;

    private void Start()
    {
        factMatcher.Init();
    }

    public void OnConceptChange(Dropdown dropdown)
    {
        playerConcept = dropdown.options[dropdown.value].text;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) )
        {
           Vocalize();
        }
    }
    
    public void Vocalize()
    {
        rupeesInLastPot = UnityEngine.Random.Range(0, 1001);
        
        //75 chance of No rupees
        rupeesInLastPot = UnityEngine.Random.Range(0, 100) <= 75 ? 0 : rupeesInLastPot;
        //Todo - the testcase should be decent..
        //Todo - seperate out the Job from the example specific parts..
        factMatcher[FactMatcher_RulesDBExample_Gen.playerFacts.name] = factMatcher.StringID(playerName);
        factMatcher[FactMatcher_RulesDBExample_Gen.playerFacts.concept] = factMatcher.StringID(playerConcept);
        factMatcher[FactMatcher_RulesDBExample_Gen.playerFacts.age] = playerAge;
        factMatcher[FactMatcher_RulesDBExample_Gen.potsmashFacts.rupeesInLastPot] = rupeesInLastPot;
        
        
        factMatcher[FactMatcher_RulesDBExample_Gen.weatherFacts.temperature] = temperature;
        factMatcher[FactMatcher_RulesDBExample_Gen.weatherFacts.windMs] = windMs;
        factMatcher[FactMatcher_RulesDBExample_Gen.weatherFacts.rainMs] = rainMs;
        factMatcher[FactMatcher_RulesDBExample_Gen.weatherFacts.thunderPerSecond] = thunderPerSecond;
        factMatcher[FactMatcher_RulesDBExample_Gen.GlobalFacts.currentMap] = factMatcher.StringID(currentMap);

        RuleDBEntry rule = factMatcher.PickBestRule(); 
        if (rule != null)
        {
            text.text = rule.payload;
            
        }
        else
        {
            text.text = "No rule matched";
        }

        
    }
}
