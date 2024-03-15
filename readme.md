# Automata
Automata is a simple scripting language. It is meant to be implemented in mods / modding engines to support easy scripting and implementation for custom features. Below you may see the `.amtascript` (AutoMaTA Script) API

# Expressions
Expressions are evaluated without order of operation (no PEMDAS). Therefor, you will need to encapsulate your expressions with parentheses
### Supported operators for numbers
```
Addition:
<expr> + <expr>

eg.
5 + 2
$var1 + $var2
```
```
Subtraction:
<expr> - <expr>

eg.
5 - 2
$var1 - $var2
```
```
Multiplication:
<expr> * <expr>

eg.
5 * 2
$var1 * $var2
```
```
Division:
<expr> / <expr>

eg.
5 / 2 (this returns 2.5)
$var1 / $var2
```
```
Flooring:
_(<expr>)

eg.
_(5 / 2) (this returns 2)
```
Numbers can also be treated as booleans. 0.0 represents false, anything else represents true. Default backbone will use 1.0 as true for expression results
```
Logical AND:
<expr1> & <expr2>
Logical OR:
<expr1> | <expr2>
Logical NOT:
!(<expr>)
```
### Supported comporators for numbers
```
Less Than
<expr1> < <expr2>
Greater Than
<expr1> > <expr2>
Equals
<expr1> ? <expr2>

Using logical not, you can build all other expresions
Not Equal
!(<expr1> ? <expr2>)
Less Than or Equal To
!(<expr1> > <expr2>)
Greater Than or Equal To
!(<expr1> < <expr2>)
```
If you want to safely compare two logical expresions, make sure to negate both of them, to bring them into backbone default `0.0` / `1.0`.
```
!(<expr1>) ? !(<expr2>)
```
### Supported operators for strings
```
Appending:
<string> ~ <string>

eg.
"Hello " ~ "World!" (this returns "Hello World!")
$str1 ~ $str2
```
```
Int to string:
s(<int>)

eg.
s(5 + 2) (this returns "7")
```
```
Lowercase all:
l(<string>)

eg.
l("HeLlO wOrLd!") (this returns "hello world!")
```
### Supported comparators for strings
```
Equals
<string1> ' <string2>
Case indifferent equals:
l(<string1>) ' l(<string2>)

Using logical not, you can build all other operators
Different:
!(<string1> ' <string2>)
Case indifferent different:
!(l(<string1>) ' l(<string2>))
```

### Order of processing (old)
All expressions are parsed right to left, disregarding and operation order (aka PEMDAS). In order to have predictable outcomes, please use parenthesis all around your expressions, especially when comparing.

# Logical Flow
### If
```
if <expr>
<block>
fi

if <expr>
<block if true>
el
<block if false>
fi
```
### While
```
while <expr>
<block>
ewhil
```

# Variables
All variables are global, and have no scope / shadowing. Variables are loosly typed, and can only be two types: Numbers (double) and strings. It is left to the backbone / implementor to cast these
### Variable Delcaration
```
$<variable_name> = <constant / expression>

eg.
$my_variable = 5
$other_variable = $my_variable + 2
```
### Variable Deletion
It's best practice to remove argument variables at the end of your function. This way backbone will throw an internal exception if your function gets called without settings parameters.

This also helps free up memory from the backbone program scope.
```
delete$<variable_name>

eg.
delete$my_function_0
```

# Functions
Functions are declared as follows:
```
@<function_name>
<function block>
@
```
Functions are called as follows:
```
!<function_name>
```
For passing parameters and return values you will need to use variables
```
@add_two_numbers
$add_two_numbers_result = $add_two_numbers_p1 + $add_two_numbers_p2
@

calling the function:
$add_two_numbers_p1 = 5
$add_two_numbers_p2 = 2
!add_two_numbers

printing the result to the console:
$print_string = s($add_two_numbers_result)
!print
```

# Script running
### Library importing
Library importing is a feature supported by the backbone which allows you to import pre-made functions / macros
You can only import functions at the top of your code, like follows:
```
+library1
+library_name_2
```
NOTE: only the first lines that consecutively have + prepended to it will be processed as library imports. Adding comments before / between these lines will lead to some libraries not being imported

### Main function / global scope
You are not allowed to have code in the global scope. The processor will error out if any code blocks lays in the global scope.

You can only define functions to be called, and library imports.

Depending on the backbone, one or more functions may be called, leading to script running.
In the vanilla AutomataProcessor, the function `main` is called.

### Comments
You can leave comments in your code with `#`. They can only be placed on their own line, you can't in-line them with other parts of code.

### Predefined functions
The vanilla backbone has a single pre-impleneted function, `print`. You may call it like this:
```
$print_0 = "Hello, World!"
!print
```
### Functions creation with `+basic`
```
+basic

@add_two_numbers:
$add_two_numbers_ret = $add_two_numbers_0 + $add_two_numbers_1
$add_two_numbers_0 = delete
$add_two_numbers_1 = delete
# best practice to delete arguments.
@

$result = ^call_ret(add_two_numbers)[5, 2]
^call(print)[s($result)]
```
This code gets expanded to
```
@add_two_numbers:
$add_two_numbers_ret = $add_two_numbers_0 + $add_two_numbers_1
@

$add_two_numbers_0 = 5
$add_two_numbers_1 = 2
!add_two_numbers
$result = $add_two_numbers_ret
$print_0 = s($result)
!print
# using call_ret on print might result in an error, if $print_ret is not assigned. Depends on the backbone (vanilla will throw)
```
NOTE: default backbone `print` implementation supports both `$print_string` and `$print_0` argument types.