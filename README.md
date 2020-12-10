# FactMatcher
An implementation of picking the best rule match from a series of given facts. Useful for creating context-aware dialogue/events. 

You type dialog in a textfile with some facts defining when the dialog should be considered.
The textfiles are parsed into a set of rules - everything is converted into floats and compared with just arrays using jobSystem with or without burst.

See the example files in the project

This is by no means intended to be production ready, but can be used to play around with a FactMatching based approach to dialog selection

This is inspired by:
-How Valve did dialog for Left 4 Dead
https://t.co/LOiZt3go5P?amp=1 AI-driven Dynamic Dialog through Fuzzy Pattern Matching - Elan Ruskin 

-The Dialog system in Firewatch
https://www.youtube.com/watch?v=wj-2vbiyHnI&feature=emb_title

To use this in production you would likely need to extend the Payload to a bit more complex and to make your dialog lines live in a datbase of some sorts, as described in the Firewatch talk.

Another thing that would need to be fixed for it to be production ready, is that because of code generation - changes in scriptfiles will quite possibly make the project not compile, which is just totally annoing. More error checks, safety, robustness is def. needed.

