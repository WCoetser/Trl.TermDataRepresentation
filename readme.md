# Description

The overall aim of this project is to create a term rewriting system that could be useful in everyday programming, and to represent  data in a way that roughly correspond to the definition of a _term_ in formal logic. (see [Wikipedia](https://en.wikipedia.org/wiki/Term_(logic))). _Terms_ should be familiar to any programmer because they are basically constants, variables, and function symbols.

In order to achieve this goal, four projects were created and hosted on github:
* [Trl.IntegerMapper](https://github.com/WCoetser/Trl.Serialization) - Defines a core data structures that are used to map unique expression subtrees to unique integers for use in equality and hashing.
* [Trl.PegParser](https://github.com/WCoetser/Trl.IntegerMapper) - Parser that is used to parse the term rewriting language using Parsing Expression Grammers.
* _Trl.TermDataRepresentation (This project)_ - Ties everything together and contains the main logic.
* [Trl.Serialization](https://github.com/WCoetser/Trl.Serialization) - Contains all code relating to reflection and binding terms to C# classes and structs.

One of the reasons why the serialization was separated from the Trl.TermDataRepresentation project is that serialization requires reflection, and not all programming languages have .NET style reflection. This separation could be handy if the project is eventually ported to C++ or C, and creates the possibility of compiling to web assembly, and then implementing just the serialization in JavaScript, Typescript, or whatever is popular at the time.

This project is a spiritual successor to another similar system that I implemented in around 2014 to 2016 that can be found [here](https://github.com/WCoetser/TRL).

# Overview of the Term Rewriting Language

The TRL (Term Rewriting Language) system revolves around an in-memory _term database_. This differs from a graph database in the sense that it is designed to store terms.

Defining a term is quite simple, here are examples:

```C
date(2020, 10, 13);
greeting("Hello");
house(wall(1), wall(2), wall(3), roof());
12.34;
"Cheesecake is tasty";
```

Sometimes _identifiers_ are also handy. This is also a valid term:

```C
mul(Pi, 20, 20);
```

Identifiers and term names follow the same rules as identifiers in most programming languages and can be `.` delimited, ex. `Math.Pi` is valid, but `123HaveSomeTee` is not.

Collections of objects are stored as lists. Internally, lists are viewed as terms with no names. Therefore, they are basically just lists of arguments:

```C
(1,2,3);
("Mozart", "Bach", "Beethoven");
```

In order to make it easy to store and retrieve terms, they can be assigned labels, for example:

```C#
location_socrates: Location("Athens", "Greece");
```

Labels can be a comma separated list. It is also possible to assign metadata to terms. This is like listing record members in SQL:

```C#
Location<City, Country>("Athens", "Greece");
```

TRL also supports substitutions using an arrow syntax. This will match the value in the head of the arrow and replace it with the value in the tail, for example:

```C#
(wall(1), wall(2), wall(3));
wall(2) => window;
```

This will replace `wall(2)` with `window` when executed. Labels are copied across substitutions to make it easy to get specific substitution results, for example:

```C#
root: a;
a => b;
```

This will result in:

```C#
root: b;
a => b;
```

It is also possible to use variables, represented with a colon in front of an identifier, for example:

```C#
:x;
```

When this is used with a substitution, syntactic unification (see [Wikipedia](https://en.wikipedia.org/wiki/Unification_(computer_science)#Syntactic_unification_of_first-order_terms)) is used to calculate values for the variables involved that will unify the substitution rule head with a given term. These substitutions are used in the tail (right hand side of the arrow) of the variable to create a substitute term.

For example, this input:

```C#
t(1);
t(:x) => s(:x);
```

Will result in this output:

```C#
s(1);
t(:x) => s(:x);
```

Labels are copied accross during subtitution, making it easy to find the result. For example:

```C#
root: t(1);
t(:x) => s(:x);
```

Will result in:

```C#
root: s(1);
t(:x) => s(:x);
```

There is one special case for the use of variables in substitutions: When the rule head is a variable, it will be treated as a normal syntactic substitution, and unification will not be used. This will prevent the rewrite rule from being applied to each and every given term.

Unification also works on terms with class field mappings, and lists. This is useful for the serialization discussed earlier, ex.

```C#
point<x,y>(1,2);
point<x,y>(:x,:y) => point<y,x>(:y, :x);
(1,2,3);
(:x,:y,:z) => (:x, :y);
```

# Simple example - Loading terms and running substitutions

Let us say that you want to process the following system of terms and substitutions (also referred to as _rewrite rules_):

```C#
0;
0 => inc(0);
```

First the parser must be used to parse terms:

```C#
var parser = new TrlParser();
var parseResult = parser.ParseToAst(input);
if (!parseResult.Succeed)
{
    Console.WriteLine("Syntax error.");
    return;
}
```

Then the parse results can be loaded into the `TermDatabase` class:

```C#
var termDatabase = new TermDatabase();
termDatabase.Writer.StoreStatements(parseResult.Statements);
```

Now the substitution `0 => inc(0);` can be applied. Substitutions are usually applied until no further terms are changed. Some scenarios could involve cyclical or non-terminating combinations of substitutions. In order to cater for this, a repetition limit of 4 is passed in.

```C#
termDatabase.ExecuteRewriteRules(4);
```

The output can now be retrieved into a string and printed to the console. First the results are retrieved as an AST (abstract syntax tree) representation, which is then converted into source code using the `ToSourceCode` function.

```C#
var output = termDatabase.Reader.ReadCurrentFrame();
Console.WriteLine(output.ToSourceCode(true));
```

The _current frame_ in `ReadCurrentFrame` refers to the collection of terms which forms the currently rewritten terms in the term database. Output of this is:

```C#
inc(inc(inc(inc(0))));
0 => inc(0);
```

# Installation via Nuget

See [https://www.nuget.org/packages/Trl.TermDataRepresentation/](https://www.nuget.org/packages/Trl.TermDataRepresentation/) for nuget package.

# Unit Test Code Coverage

Unit tests can be run using the `.\test.ps1` script. This will generate a code coverage report in the `.\UnitTestCoverageReport` folder using [Coverlet](https://github.com/tonerdo/coverlethttps://github.com/tonerdo/coverlet) and [ReportGenerator](https://github.com/danielpalme/ReportGenerator).

![Code Coverage](code_coverage.PNG)

# Licence

Trl.TermDataRepresentation is released under the MIT open source licence. See LICENCE.txt in this repository for the full text.
