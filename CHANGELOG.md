# Changelog

## < 0.30.0

* Made Rebus - BAM!1

## 0.30.0

* Changed batch API to use `IEnumerable` instead of `params object[]` because the params thing could easily blur which would actually end up as a logical message.

## 0.30.1

* Made Rebus control bus messages `[Serializable]` because for some reason `BinaryFormatter` just likes it that way - applying the attribute makes instances of the class MUCH more serializable.