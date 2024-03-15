# Macro definitions
Macros are definitions that allow replacing lines with something else. This can be mainly used for calling functions.
Defining macros:
```
MACRO <macro_expr> => <expanded_string>
```
Macro expresions act like somewhat like regexes. You can pass handle parameters with %param_name%. You will need to pass a name though. Look at examples.


You can also pass an arbitrary number of parameters, by utilising the `[%varname_..%]` structure.
These can be extended with `[]`. You are also provided the `%..%` macro.

If you need to expand the macro into multiple lines, you can use `\n`

If you want to prepend code to the line containing the macro, you can use `{<expansion>}`, which automaticaly expands the macro to the lines before, and replaces the occurance with `<expansion>`. If you don't include in-place expansion with `{}`, then the whole line will get replaced with the macro expansion.

For automata to know where a macro is being called, you need to prepend your macro call with `^`

Also note, you can only have one macro expansion per line. You can't recursively expand a macro, or pass a macro as an argument to another macro.

Here's a complex macro, included in `+basic` that allows parameter inlining for calling functions
```
MACRO call(%function_name%)[%params_..%] => [$%function_name%_%..% = %params_..%\n]!%function_name%
MACRO call_ret(%function_name%)[%params_..%] => [$%function_name%_%..% = %params_..%\n]!%function_name%{$%function_name%_ret}
```
Example calling print
```
^call(print)["Hello, world!"]

gets expanded to:
$print_0 = "Hello, world!"
!print
```

Creating functions for this macro requires handling parameters as `$<function_name>_<idx>`, where `<idx>` starts from 0

## Function declaration with macros
If you want your function to be called more nicely, you can use macros to implement a `call`-like structure, as follows:
```
MACRO add_two_numbers(%a%, %b%) => $add_two_numbers_0 = %a%\n$add_two_numbers_1 = %b%\n!add_two_numbers\n{$add_two_numbers_ret}
@add_two_numbers
$add_two_numbers_ret = $add_two_numbers_0 + $add_two_numbers_1
$add_two_numbers_0 = delete
$add_two_numbers_1 = delete
# delete the variables, in case function gets called without setting parameters. This will determine a backbone exception
@

call like:
$result = ^add_two_numbers(5, 2)
or:
$var1 = 5
$var2 = 2
$result = ^add_two_numbers($var1, $var2)
or:
$result = ^add_two_numbers($var1, 2)
(you can mix var / expressions / consts up. macro doesn't care)

last example would get expanded to:
$var1 = 5
$add_two_numbers_0 = $var1
$add_two_numbers_1 = 2
!add_two_numbers
$result = $add_two_numbers_ret
```