# FactMatcher
An implementation of picking the best rule match from a series of given facts. Useful for creating context-aware dialogue/events. 

You type dialog in a textfile with some facts defining when the dialog should be considered.
The textfiles are parsed into a set of rules - everything is converted into floats and compared with just arrays using jobSystem with or without burst.

See the example files in the project

This is by no means intended to be production ready, but can be used to play around with a FactMatching based approach to dialog selection
