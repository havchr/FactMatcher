# FactMatcher
An implementation of picking the best rule match from a series of given facts. Useful for creating context-aware dialogue/events. 

You type dialog in a textfile with some facts defining when the dialog should be considered.
The textfiles are parsed into a set of rules - everything is converted into floats and compared with just arrays using jobSystem with or without burst.

An example of a rulescript dealing with a character called Lemon smashing up pots looking for rupees follows.

.LemonPotSmash
player.name = Lemon
player.concept = onPotSmash
potsmash.rupeesInLastPot = 0
:FactWrite:
commentator.potSmashCount = 1
:Response:
Lemon Smashes a pot to see if there's any coin in it.
:End:

.LemonPotSmash.Cont2
commentator.potSmashCount = 1
:FactWrite:
commentator.potSmashCount += 1
:Response:
Lemon is reckless.
:End:

.LemonPotSmash.Cont3
commentator.potSmashCount = 2
:FactWrite:
commentator.potSmashCount += 1
:Response:
Rupees , ruupes, is all Lemon wants.
:End:

.LemonPotSmash.Cont4
commentator.potSmashCount = 3
:FactWrite:
commentator.potSmashCount += 1
:Response:
Rupees , ruupes, is all Lemon wants.
:End:


.LemonPotSmash.smallPayout
potsmash.rupeesInLastPot Range(0,5]
:FactWrite:
commentator.potSmashCount = 0
:Response:
Always just a little trinket for Lemon. Never big rupees.
:End:

.LemonPotSmash.smallPayout2
potsmash.rupeesInLastPot  Range(5,50]
:FactWrite:
commentator.potSmashCount = 0
:Response:
Wow - finally at least some rupees. Maybe Lemon could buy an Orange.
:End:
