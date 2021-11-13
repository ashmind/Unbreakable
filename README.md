### Deprecated

This library is no longer actively maintained.

### History

Unbreakable was a .NET sandboxing library that was designed to run untrusted code in-process.

It was only used by SharpLab and was never officially pen-tested, which is why every version was marked `-untrusted`.

While some ideas from the codebase might be still useful (e.g. stack overflow prevention), its code security model of allowing "safe" APIs was not scalable and required continuous effort to maintain.

As an alternative, a better security model is provided by either full container sandboxing or process-level sandboxing (e.g. Chrome sandbox).