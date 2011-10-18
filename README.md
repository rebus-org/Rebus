What?
====

Rebus is a lean service bus implementation, similar in nature to [NServiceBus][3] and [MassTransit][4].

In fact, its goal is to copy NServiceBus in almost every aspect, only deviating by having

* a simpler and more intuitive configuration story
* not quite as many options
* no doodleware
* dependencies only on .NET 4 BCL and Log4net
* much better error messages

Oh, and Rebus is free as in beer and speech.

Why?
====

Motivation
==

I am a happy NServiceBus user, and I still am. It just puzzles me that 

* NServiceBus is 60 KLOC spread across 200+ projects - the code is hard to read
* NServiceBus went from being absolutely free to be licensed

I realize that NServiceBus is pretty cheap when you think of all the good things it can do for you, but to many people I think the license fee is an annoyance that will hinder them in introducing NServiceBus in all of their awesome projects - Which is a shame!

Therefore *Rebus == FreeBusToTheMasses*.

More info coming soon at http://mookid.dk/oncode/rebus

One day, maybe I'll tweet something as well... [@mookid8000][2]

License
====

Rebus is [Beer-ware][1].

[1]: http://en.wikipedia.org/wiki/Beerware
[2]: http://twitter.com/#!/mookid8000
[3]: http://nservicebus.com/
[4]: http://masstransit-project.com/