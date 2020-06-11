```
p1 => vertex<x,y,z>(1,2,3);
p2 => vertex<x,y,z>(4,5,6);
p3 => vertex<x,y,z>(7,8,9);

test1: p4 => vertex<x,y,z>(10,11,12);

// comment

t1: triangle(p1,p2,p3);

triangle(p4,p2, vertex<x,y,z>(13,14,15));

list1: (1, "testing 123", vertex<x,y>(200,300));

// equivalent to: add[3,2,1];
add[1,2,3];

// AC list
[1,2,3,4];

```
- Selecting data with labels
- full json mapping
- tagging labels on with rewrite rules, creating a string
- AC terms with []
- Lists with ()
- Calling locally registered methods or default constructors with term name
- Mapping object fields with <>
- terms have associated objects

basic "types" are:
- strings in double quotes `"`, with `"` being escaped as `\"`
- numbers written using floating point notation. Currently the size limits comes from the underlying platform.
- identifiers, same as C or C#, but also allowing `.`, i.e. `Constants.PI` and `point_1` are both valid

```
vertex<x,y,z>(10,11,12);
```
calls the default constructor for `vertex` and maps arguments to
```
{
    x: 10,
    y: 11,
    z: 12
}
```
creation of label strings via rewrite rules for data selection, ex.:

```
l1 : a1 => a2;
l2: a2 => a3;
l3: a4 => vertex(1,2,3);

l4: a1;

```
This will result in a term database with `vertex(1,2,3)` having the _label string_ `l1 l2 l3 l4` which can be used to select it as a data query. The term label will always be the last think in the list, if it is present.

In future work regular expressions will be used to identify terms.

Terms are unique and represented by integers in the back-end. These integers are used for equality and hashing, with no duplicates allowed. Writing
```
a(1); a(1); a(1);
```
Is equivalent to writing
```
a(1);
```
AC terms are flattened. Therefore, writing
```
add[1,add[2,3]];
```
Is equavalent to all of these:
```
add[1,2,3]; add[add[1,2], 3]; add[3,2,1]; // ... etc.
```
<!-- Lists are syntactic sugar, therefore these are equivalent
```
list(1,2,3,4); (1,2,3,4)
``` -->
AC (associative commutative)  lists are flattenned, therefore these are all equivalent:
```
[1,2,3]; [1,[2,3]]; [[3,2 ], [1]]
```
This means that AC lists can't be nested because of the semantics and they function as mathematical "bags"

Normal lists can however be nested, therefore this is valid:
```
identity_3x3_row_form => ((1,0,0), (0,1,0), (0,0,1))
```
Rewrite rules (or arrows) functions as pointers at this point. In future versions variables and full term rewriting will be instroduced. At this point the head can only be an identifier, serving as a pointer. Therefore a model with two triangles sharing the same point can be written as:

```
p1 => point(1,2);
p1 => point(2,2);
p1 => point(3,4);
p4 => point(5,6);

// Share p1
triangle1: triangle(p1,p2,p3);
triangle2: triangle(p1,p2,p3);
```
