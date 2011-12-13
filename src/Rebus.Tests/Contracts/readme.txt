The Contracts namespace in the test project is meant to be used when new implementations
are made of Rebus' core interface, like e.g. ISendMessages/IReceiveMessages etc.

Maybe I should take a look at Greg Young's Grensesnitt, because this is actually what this
is - an assertion that implementors do what they promise to do, as seen from the outside.