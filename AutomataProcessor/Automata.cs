using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using AutomataProcessor.Backbone;
using AutomataProcessor.Parser;

namespace AutomataProcessor
{

    public static class Config
    {
        public static int MaxWhileLoops = 1000;
        public static bool PrintDebugMessages = false;
    }

    public static class Logger
    {
        public static Action<string> logFunction = Console.WriteLine;

        public static void Debug(string str)
        {
            if (!Config.PrintDebugMessages) return;
            logFunction(str);
        }

        public static void Print(string str)
        {
            logFunction(str);
        }
    }

    namespace Backbone
    {
        public class Variable : IEvaluable
        {
            public string name;
            public string? str_value;
            public double? num_value;
            public VariableType var_type;

            public Variable(string name, string value)
            {
                this.name = name;
                str_value = value;
                num_value = null;
                var_type = VariableType.String;
            }
            public Variable(string name, double value)
            {
                this.name = name;
                str_value = null;
                num_value = value;
                var_type = VariableType.Number;
            }
            public Variable(string name, bool value)
            {
                this.name = name;
                str_value = null;
                num_value = value ? 1 : 0;
                var_type = VariableType.Number;
            }
            public Variable(string name, Variable copyFrom)
            {
                this.name = name;
                if (copyFrom.var_type == VariableType.String)
                {
                    str_value = copyFrom.str_value!;
                    num_value = null;
                    var_type = VariableType.String;
                }
                if (copyFrom.var_type == VariableType.Number)
                {
                    str_value = null;
                    num_value = copyFrom.num_value!.Value;
                    var_type = VariableType.Number;
                }
            }

            public void Assign(string value)
            {
                str_value = value;
                num_value = null;
                var_type = VariableType.String;
            }
            public void Assign(double value)
            {
                str_value = null;
                num_value = value;
                var_type = VariableType.Number;
            }
            public void Assign(bool value)
            {
                str_value = null;
                num_value = value ? 1 : 0;
                var_type = VariableType.Number;
            }
            public void Assign(Variable copyFrom)
            {
                if (copyFrom.var_type == VariableType.String)
                    Assign(copyFrom.str_value!);
                if (copyFrom.var_type == VariableType.Number)
                    Assign(copyFrom.num_value!.Value);
            }

            public enum VariableType
            {
                String = 0,
                Number = 1
            };

            public Variable Evaluate(ProgramScope scope) => this;

            public override string ToString()
            {
                return $"Var(Name: {name}, Type: {var_type}, StrVal: {str_value}, NumVal: {num_value})";
            }
        }

        public class StringConstant : IEvaluable
        {
            string val;

            public StringConstant(string val)
            {
                this.val = val;
            }

            public Variable Evaluate(ProgramScope scope)
            {
                return new Variable("_Parser.StringConstant", val);
            }

            public override string ToString()
            {
                return $"StrConst({val})";
            }
        }
        public class NumberConstant : IEvaluable
        {
            double val;

            public NumberConstant(double val)
            {
                this.val = val;
            }

            public Variable Evaluate(ProgramScope scope)
            {
                return new Variable("_Parser.NumberConstant", val);
            }
            public override string ToString()
            {
                return $"NumConst({val})";
            }
        }
        public class VariableResolver : IEvaluable
        {
            public Variable Evaluate(ProgramScope scope)
            {
                Variable? var = scope.GetVariable(var_name);
                if (var == null)
                {
                    throw new VariableDoesntExistException($"[ProgramScope: {scope.name}] Tried resolving unknown variable {var_name}");
                }
                return var;
            }

            string var_name;
            public VariableResolver(string var_name)
            {
                this.var_name = var_name;
            }

            [System.Serializable]
            public class VariableDoesntExistException : System.Exception
            {
                public VariableDoesntExistException(string message) : base(message) { }
            }

            public override string ToString()
            {
                return $"VarResolver({var_name})";
            }
        }

        public interface IEvaluable
        {
            Variable Evaluate(ProgramScope scope);
        }

        public class Expression : IEvaluable
        {

            public IEvaluable LHS;
            public IEvaluable? RHS;
            public ExpressionType type;

            public Expression(IEvaluable LHS, ExpressionType type, IEvaluable? RHS)
            {
                this.LHS = LHS;
                this.type = type;
                this.RHS = RHS;
            }

            public Variable Evaluate(ProgramScope scope)
            {
                return LogicEngine.Evaluate(this, scope);
            }

            public enum ExpressionType
            {
                NumberAddition,
                NumberSubtraction,
                NumberMultiplication,
                NumberDivision,
                NumberFlooring,
                BooleanAnd,
                BooleanOr,
                BooleanNot,
                NumberLessThan,
                NumberGreaterThan,
                NumberEquals,
                StringAppending,
                IntToString,
                StringLowercase,
                StringEquals
            };

            public override string ToString()
            {
                return $"Expr(LHS: {LHS} Type: {type} RHS: {RHS})";
            }
        }

        public static class LogicEngine
        {
            public static Variable Evaluate(Expression expr, ProgramScope scope)
            {
                switch (expr.type)
                {
                    case Expression.ExpressionType.NumberAddition:
                        return new Variable("_LogicEngine.NumberAdditionResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) + HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.NumberSubtraction:
                        return new Variable("_LogicEngine.NumberSubtractionResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) - HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.NumberMultiplication:
                        return new Variable("_LogicEngine.NumberMultiplicationResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) * HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.NumberDivision:
                        return new Variable("_LogicEngine.NumberDivisionResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) / HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.NumberFlooring:
                        return new Variable("_LogicEngine.NumberFlooringResult", Math.Floor(HandleNumberVariable(expr.LHS.Evaluate(scope))));
                    case Expression.ExpressionType.BooleanAnd:
                        return new Variable("_LogicEngine.BooleanAndResult", DoubleToBool(HandleNumberVariable(expr.LHS.Evaluate(scope))) && DoubleToBool(HandleNumberVariable(HandleOptional(expr.RHS, scope))));
                    case Expression.ExpressionType.BooleanOr:
                        return new Variable("_LogicEngine.BooleanOrResult", DoubleToBool(HandleNumberVariable(expr.LHS.Evaluate(scope))) || DoubleToBool(HandleNumberVariable(HandleOptional(expr.RHS, scope))));
                    case Expression.ExpressionType.BooleanNot:
                        return new Variable("_LogicEngine.BooleanNotResult", !DoubleToBool(HandleNumberVariable(expr.LHS.Evaluate(scope))));
                    case Expression.ExpressionType.NumberLessThan:
                        return new Variable("_LogicEngine.NumberLessThanResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) < HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.NumberGreaterThan:
                        return new Variable("_LogicEngine.NumberGreaterThanResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) > HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.NumberEquals:
                        return new Variable("_LogicEngine.NumberEqualsResult", HandleNumberVariable(expr.LHS.Evaluate(scope)) == HandleNumberVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.StringAppending:
                        return new Variable("_LogicEngine.StringAppendingResult", HandleStringVariable(expr.LHS.Evaluate(scope)) + HandleStringVariable(HandleOptional(expr.RHS, scope)));
                    case Expression.ExpressionType.IntToString:
                        return new Variable("_LogicEngine.IntToStringResult", HandleNumberVariable(expr.LHS.Evaluate(scope)).ToString());
                    case Expression.ExpressionType.StringLowercase:
                        return new Variable("_LogicEngine.StringLowercaseResult", HandleStringVariable(expr.LHS.Evaluate(scope)).ToLower());
                    case Expression.ExpressionType.StringEquals:
                        return new Variable("_LogicEngine.StringAppendingResult", HandleStringVariable(expr.LHS.Evaluate(scope)) == HandleStringVariable(HandleOptional(expr.RHS, scope)));
                }
                throw new UnknownOperatorException("The operator " + expr.type + " is unknown / not yet supported.");
            }

            public static bool DoubleToBool(double val)
            {
                if (val == 0) return false;
                return true;
            }

            public static Variable HandleOptional(IEvaluable? side, ProgramScope scope)
            {
                if (side == null)
                {
                    throw new NullExpressionSideException();
                }
                return side.Evaluate(scope);
            }

            public static string HandleStringVariable(Variable? var)
            {
                if (var == null)
                {
                    throw new NullVariableException();
                }
                if (var.var_type != Variable.VariableType.String)
                {
                    throw new InvalidVariableTypeException("Expected variable " + var.name + " to be String");
                }
                return var.str_value!;
            }

            public static double HandleNumberVariable(Variable? var)
            {
                if (var == null)
                {
                    throw new NullVariableException();
                }
                if (var.var_type != Variable.VariableType.Number)
                {
                    throw new InvalidVariableTypeException("Expected variable " + var.name + " to be Number");
                }
                return var.num_value!.Value;
            }

            [System.Serializable]
            public class NullVariableException : System.Exception
            {
                public NullVariableException() { }
            }
            [System.Serializable]
            public class InvalidVariableTypeException : System.Exception
            {
                public InvalidVariableTypeException(string message) : base(message) { }
            }
            [System.Serializable]
            public class NullExpressionSideException : System.Exception
            {
                public NullExpressionSideException() { }
            }
            [System.Serializable]
            public class UnknownOperatorException : System.Exception
            {
                public UnknownOperatorException(string message) : base(message) { }
            }
        }

        public class ProgramScope
        {
            public Dictionary<string, Variable> variables;
            public Dictionary<string, ICallable> functions;

            public Dictionary<string, object> native;
            public string name;

            public ProgramScope(string name)
            {
                this.name = name;
                variables = new Dictionary<string, Variable>();
                functions = new Dictionary<string, ICallable>();
                native = new Dictionary<string, object>();
            }

            public void RegisterVariable(Variable variable)
            {
                if (variables.ContainsKey(variable.name))
                {
                    throw new VariableAlreadyExistsException($"[ProgramScope: {name}] Tried registering variable {variable.name} but already exists.");
                }
                variables.Add(variable.name, variable);
            }
            public Variable? GetVariable(string name)
            {
                if (!variables.ContainsKey(name))
                    return null;
                return variables[name];
            }
            public void UnregisterVariable(string name)
            {
                if (!variables.ContainsKey(name))
                    throw new VariableDoesntExistException($"[ProgramScope: {this.name}] Tried deleting unknown variable {name}");
                variables.Remove(name);
            }

            public void RegisterFunction(string name, ICallable body)
            {
                if (functions.ContainsKey(name))
                {
                    throw new FunctionAlreadyExistsException($"[ProgramScope: {this.name}] Tried registering function {name} but already exists.");
                }
                functions.Add(name, body);
            }
            public ICallable? GetFunction(string name)
            {
                if (!functions.ContainsKey(name))
                    return null;
                return functions[name];
            }
            public void ImportParser(ProgramParser parser)
            {
                if (parser.functions == null)
                    return; // TODO: throw exception
                foreach (var function in parser.functions)
                {
                    RegisterFunction(function.name, function);
                }
            }

            [System.Serializable]
            public class VariableAlreadyExistsException : System.Exception
            {
                public VariableAlreadyExistsException(string message) : base(message) { }
            }
            [System.Serializable]
            public class FunctionAlreadyExistsException : System.Exception
            {
                public FunctionAlreadyExistsException(string message) : base(message) { }
            }
            [System.Serializable]
            public class VariableDoesntExistException : System.Exception
            {
                public VariableDoesntExistException(string message) : base(message) { }
            }

            public override string ToString()
            {
                string ret = $"[ProgramScope: {name}]\nVariables:\n";
                foreach (var variable in variables)
                    ret += " " + variable.ToString() + "\n";
                ret += "Functions:\n";
                foreach (var function in functions)
                    ret += " " + function.ToString() + "\n";
                return ret;
            }
        }

        public interface ICodeLine
        {
            void Execute(ProgramScope scope);
            CodeLineType type();
            public enum CodeLineType
            {
                VariableAssign,
                VariableDeletion,
                IfBlocks,
                WhileBlocks,
                FunctionCall,
            };
        }
        public interface ICallable
        {
            void Call(ProgramScope scope);
        }

        namespace CodeLines
        {
            public class VariableAssign : ICodeLine
            {
                public ICodeLine.CodeLineType type() => ICodeLine.CodeLineType.VariableAssign;
                public void Execute(ProgramScope scope)
                {
                    Variable? var = scope.GetVariable(var_name);
                    if (var != null)
                        var!.Assign(value.Evaluate(scope));
                    else
                        scope.RegisterVariable(new Variable(var_name, value.Evaluate(scope)));
                }
                string var_name;
                IEvaluable value;
                public VariableAssign(string var_name, IEvaluable value)
                {
                    this.var_name = var_name;
                    this.value = value;
                }

                public override string ToString()
                {
                    return $"Line(Type: {type()} Var: {var_name} Val: {value})";
                }
            }
            public class VariableDeletion : ICodeLine
            {
                public ICodeLine.CodeLineType type() => ICodeLine.CodeLineType.VariableDeletion;
                public void Execute(ProgramScope scope)
                {
                    scope.UnregisterVariable(var_name);
                }

                string var_name;
                public VariableDeletion(string var_name)
                {
                    this.var_name = var_name;
                }

                public override string ToString()
                {
                    return $"Line(Type: {type()} Var: {var_name})";
                }
            }
            public class IfBlocks : ICodeLine
            {
                public ICodeLine.CodeLineType type() => ICodeLine.CodeLineType.IfBlocks;

                public void Execute(ProgramScope scope)
                {
                    bool evalResult = LogicEngine.DoubleToBool(LogicEngine.HandleNumberVariable(expr.Evaluate(scope)));
                    if (evalResult)
                        trueBlock.Call(scope);
                    else if (falseBlock != null)
                        falseBlock.Call(scope);
                }

                IEvaluable expr;
                CodeBlock trueBlock;
                CodeBlock? falseBlock;
                public IfBlocks(IEvaluable expr, CodeBlock trueBlock, CodeBlock? falseBlock)
                {
                    this.expr = expr;
                    this.trueBlock = trueBlock;
                    this.falseBlock = falseBlock;
                }

                public override string ToString()
                {
                    return $"Line(Type: {type()} Expr: {expr} TrueBlock: {trueBlock} FalseBlock: {falseBlock})";
                }
            }
            public class WhileBlocks : ICodeLine
            {
                public ICodeLine.CodeLineType type() => ICodeLine.CodeLineType.WhileBlocks;
                public void Execute(ProgramScope scope)
                {
                    int steps = 0;
                    while (steps < Config.MaxWhileLoops && LogicEngine.DoubleToBool(LogicEngine.HandleNumberVariable(expr.Evaluate(scope))))
                    {
                        block.Call(scope);
                        ++steps;
                    }
                }

                IEvaluable expr;
                CodeBlock block;
                public WhileBlocks(IEvaluable expr, CodeBlock block)
                {
                    this.expr = expr;
                    this.block = block;
                }

                public override string ToString()
                {
                    string ret = $"WhileBlocks(Expr: {expr}; Name: {block.name}; Lines: \n";
                    foreach (var line in block.lines)
                        ret += " " + line.ToString() + "\n";
                    return ret + ")";
                }
            }
            public class FunctionCall : ICodeLine
            {
                public ICodeLine.CodeLineType type() => ICodeLine.CodeLineType.FunctionCall;
                public void Execute(ProgramScope scope)
                {
                    ICallable? function = scope.GetFunction(fun_name);
                    if (function == null)
                    {
                        throw new FunctionDoesntExistException($"[ProgramSpace: {scope.name}] Tried calling unknown function {fun_name}");
                    }
                    function!.Call(scope);
                }

                string fun_name;
                public FunctionCall(string fun_name)
                {
                    this.fun_name = fun_name;
                }

                [System.Serializable]
                public class FunctionDoesntExistException : System.Exception
                {
                    public FunctionDoesntExistException(string message) : base(message) { }
                }

                public override string ToString()
                {
                    return $"Line(Type: {type()} Function: {fun_name})";
                }
            }
        }

        public class CodeBlock : ICallable
        {
            public string name;
            public List<ICodeLine> lines;

            public void Call(ProgramScope scope)
            {
                foreach (var line in lines)
                    line.Execute(scope);
            }

            public CodeBlock(string name, List<ICodeLine> lines)
            {
                this.name = name;
                this.lines = lines;
            }

            public override string ToString()
            {
                string ret = $"CodeBlock(Name: {name}; Lines: \n";
                foreach (var line in lines)
                    ret += " " + line.ToString() + "\n";
                return ret + ")";
            }
        }

        public class NativeFunction : ICallable
        {
            public void Call(ProgramScope scope)
            {
                fun(scope);
            }

            Action<ProgramScope> fun;

            public NativeFunction(Action<ProgramScope> fun)
            {
                this.fun = fun;
            }
        }
    }

    namespace Parser
    {
        public class ProgramParser
        {
            string amtascript;
            public List<CodeBlock> functions;
            List<string>? raw_lines;

            public void ImportLibraries()
            {
                string libraryCompile = "";
                int importedLines = 0;
                foreach (var rawline in amtascript.Split('\n'))
                {
                    string line = rawline.Trim();
                    if (!line.StartsWith('+')) break;
                    string lib = LibraryData.GetLibrary(line[1..]);
                    libraryCompile += lib + "\n";
                    ++importedLines;
                }
                amtascript = libraryCompile + string.Join('\n', amtascript.Split('\n')[importedLines..]);
            }

            public void ExpandMacros()
            {
                // pre-process macros
                Dictionary<string, Macro> macros = new Dictionary<string, Macro>();
                string strippedMacros = "";
                foreach (var rawline in amtascript.Split('\n'))
                {
                    string line = rawline.Trim();
                    if (!line.StartsWith("MACRO "))
                    {
                        if (line.StartsWith('#')) continue; // strip comments
                        strippedMacros += rawline + "\n";
                        continue;
                    }
                    Macro macro = new Macro(line);
                    macros.Add("^" + macro.key, macro);
                }
                amtascript = strippedMacros;
                Logger.Debug("Stripped script:\n" + amtascript);
                foreach (var macro in macros)
                {
                    Logger.Debug(macro.ToString());
                }

                // expand macros

                int cnt = 1;
                while (amtascript.Contains('^') && cnt > 0)
                {
                    cnt = 0;
                    foreach (var macro in macros)
                    {
                        while (amtascript.Contains(macro.Key))
                        {
                            // get line containing macro
                            int macroIdx = amtascript.IndexOf(macro.Key);
                            int lineStart = amtascript[..macroIdx].LastIndexOf('\n');
                            int lineEnd = amtascript.IndexOf('\n', macroIdx);
                            string line = amtascript[(lineStart + 1)..lineEnd];
                            string expansion = macro.Value.Expand(line);
                            if (expansion == "") break; // eronous expansion, skip
                            Logger.Debug("Expanding line\n" + line + "\nto\n" + expansion);
                            amtascript = amtascript[..(lineStart + 1)] + expansion + amtascript[lineEnd..];
                            ++cnt;
                        }
                    }
                }
                Logger.Debug("Fully expanded script:\n" + amtascript);
            }

            public void ConvertToLines()
            {
                raw_lines = amtascript.Split('\n').Select(x => x.Trim()).ToList();
            }

            public void ParseFunctions()
            {
                if (raw_lines == null)
                    return; // TODO: throw exception
                int sz = raw_lines.Count;
                for (int i = 0; i < sz; ++i)
                {
                    if (raw_lines[i].StartsWith('@'))
                    {
                        string functionName = raw_lines[i][1..];
                        i++;
                        string functionBlock = "";
                        while (raw_lines[i] != "@")
                        {
                            functionBlock += raw_lines[i] + "\n";
                            i++;
                        }
                        functions.Add(new CodeBlock(functionName, CodeBlockParser.ParseCodeBlock(functionBlock)));
                    }
                }
            }

            public void Parse()
            {
                ImportLibraries();
                ExpandMacros();
                ConvertToLines();
                ParseFunctions();
            }

            public ProgramParser(string amtascript)
            {
                this.amtascript = amtascript;
                functions = new List<CodeBlock>();
                raw_lines = null;
            }

            [System.Serializable]
            public class LibraryDoesnExistException : System.Exception
            {
                public LibraryDoesnExistException(string message) : base(message) { }
            }
        }

        public static class CodeBlockParser
        {
            public static List<ICodeLine> ParseCodeBlock(string block)
            {
                List<string> lines = block.Split('\n').ToList();
                Logger.Debug("Parsing code block:\n" + block);
                List<ICodeLine> codeLines = new List<ICodeLine>();
                int sz = lines.Count;
                for (int i = 0; i < sz; ++i)
                {
                    if (lines[i].StartsWith('$'))
                        codeLines.Add(LineParser.ParseVariableAssign(lines[i]));
                    if (lines[i].StartsWith("delete$"))
                    {
                        codeLines.Add(LineParser.ParseVariableDeletion(lines[i]));
                    }
                    if (lines[i].StartsWith('!'))
                        codeLines.Add(LineParser.ParseFunctionCall(lines[i]));
                    if (lines[i].StartsWith("if"))
                    {
                        // extract whole if block
                        bool hasElse = false;
                        int depth = 1;
                        int start = i;
                        while (depth > 0 && i < sz)
                        {
                            ++i;
                            if (lines[i].StartsWith("if"))
                                ++depth;
                            if (lines[i].StartsWith("el") && depth == 1)
                                hasElse = true;
                            if (lines[i].StartsWith("fi"))
                                --depth;
                        }
                        codeLines.Add(LineParser.ParseIfBlocks(lines.ToArray()[start..(i + 1)], hasElse));
                    }
                    if (lines[i].StartsWith("while"))
                    {
                        // extract whole while block
                        int depth = 1;
                        int start = i;
                        while (depth > 0 && i < sz)
                        {
                            ++i;
                            if (lines[i].StartsWith("while"))
                                ++depth;
                            if (lines[i].StartsWith("ewhil"))
                                --depth;
                        }
                        codeLines.Add(LineParser.ParseWhileBlocks(lines.ToArray()[start..(i + 1)]));
                    }
                }
                return codeLines;
            }
        }

        public static class LineParser
        {
            public static ICodeLine ParseVariableAssign(string line)
            {
                int equalSign = line.IndexOf('=');
                string var = line[0..equalSign].Trim()[1..];
                string val = line[(equalSign + 1)..].Trim();
                return new Backbone.CodeLines.VariableAssign(var, ExpressionParser.ParseExpression(val));
            }
            public static ICodeLine ParseVariableDeletion(string line)
            {
                return new Backbone.CodeLines.VariableDeletion(line["delete$".Length..]);
            }
            public static ICodeLine ParseFunctionCall(string line)
            {
                return new Backbone.CodeLines.FunctionCall(line[1..]);
            }
            public static ICodeLine ParseIfBlocks(string[] lines, bool hasElse)
            {
                if (!hasElse)
                {
                    return new Backbone.CodeLines.IfBlocks(ExpressionParser.ParseExpression(lines[0][3..]), new CodeBlock("_if_true_branch", CodeBlockParser.ParseCodeBlock(string.Join('\n', lines[1..(lines.Length - 1)]))), null);
                }
                // find el
                int i = 0;
                int depth = 1;
                while (i < lines.Length && depth > 0)
                {
                    ++i;
                    if (lines[i].StartsWith("if"))
                        ++depth;
                    if (lines[i].StartsWith("el") && depth == 1)
                        break;
                    if (lines[i].StartsWith("fi"))
                        --depth;
                }
                return new Backbone.CodeLines.IfBlocks(ExpressionParser.ParseExpression(lines[0][3..]), new CodeBlock("_if_true_branch", CodeBlockParser.ParseCodeBlock(string.Join('\n', lines[1..i]))), new CodeBlock("_if_false_branch", CodeBlockParser.ParseCodeBlock(string.Join('\n', lines[(i + 1)..(lines.Length - 1)]))));
            }
            public static ICodeLine ParseWhileBlocks(string[] lines)
            {
                Logger.Debug("Parsing while blocks:\n" + string.Join('\n', lines));
                return new Backbone.CodeLines.WhileBlocks(ExpressionParser.ParseExpression(lines[0][6..]), new CodeBlock("_while_block", CodeBlockParser.ParseCodeBlock(string.Join('\n', lines[1..(lines.Length - 1)]))));
            }
        }

        public static class ExpressionParser
        {
            public static IEvaluable ParseExpression(string expr)
            {
                IEvaluable? LHS = null;
                int i = 0;
                // check LHS only expressions
                if (expr.StartsWith("_("))
                {
                    // find matching )
                    int k = 1;
                    int depth = 1;
                    while (k < expr.Length && depth > 0)
                    {
                        ++k;
                        if (expr[k] == '(')
                            ++depth;
                        if (expr[k] == ')')
                            --depth;
                    }
                    LHS = new Backbone.Expression(ParseExpression(expr[2..k]), Backbone.Expression.ExpressionType.NumberFlooring, null);
                    i = k + 1;
                }
                if (expr.StartsWith("!("))
                {
                    // find matching )
                    int k = 1;
                    int depth = 1;
                    while (k < expr.Length && depth > 0)
                    {
                        ++k;
                        if (expr[k] == '(')
                            ++depth;
                        if (expr[k] == ')')
                            --depth;
                    }
                    LHS = new Backbone.Expression(ParseExpression(expr[2..k]), Backbone.Expression.ExpressionType.BooleanNot, null);
                    i = k + 1;
                }
                if (expr.StartsWith("s("))
                {
                    // find matching )
                    int k = 1;
                    int depth = 1;
                    while (k < expr.Length && depth > 0)
                    {
                        ++k;
                        if (expr[k] == '(')
                            ++depth;
                        if (expr[k] == ')')
                            --depth;
                    }
                    LHS = new Backbone.Expression(ParseExpression(expr[2..k]), Backbone.Expression.ExpressionType.IntToString, null);
                    i = k + 1;
                }
                if (expr.StartsWith("l("))
                {
                    // find matching )
                    int k = 1;
                    int depth = 1;
                    while (k < expr.Length && depth > 0)
                    {
                        ++k;
                        if (expr[k] == '(')
                            ++depth;
                        if (expr[k] == ')')
                            --depth;
                    }
                    LHS = new Backbone.Expression(ParseExpression(expr[2..k]), Backbone.Expression.ExpressionType.StringLowercase, null);
                    i = k + 1;
                }
                if (expr.StartsWith("("))
                {
                    // find matching )
                    int k = 0;
                    int depth = 1;
                    while (k < expr.Length && depth > 0)
                    {
                        ++k;
                        if (expr[k] == '(')
                            ++depth;
                        if (expr[k] == ')')
                            --depth;
                    }
                    LHS = ParseExpression(expr[1..k]);
                    i = k + 1;
                }
                if (LHS == null)
                {
                    // check if LHS is variable
                    if (expr.StartsWith('$'))
                    {
                        ++i;
                        string var_name = "";
                        while (i < expr.Length && expr[i].IsAscii())
                        {
                            var_name += expr[i];
                            ++i;
                        }
                        LHS = new VariableResolver(var_name);
                    }
                    else
                    {
                        if (expr.StartsWith('"'))
                        {
                            // get matching "
                            int end = expr.IndexOf('"', 1);
                            string constant = expr[1..end];
                            i = end + 1;
                            LHS = new StringConstant(constant);
                        }
                        else
                        {
                            int end = i;
                            while (end < expr.Length && expr[end].IsPartOfNumber())
                            {
                                end++;
                            }
                            LHS = new NumberConstant(double.Parse(expr[i..end]));
                            i = end;
                        }
                    }
                }
                // check for operator
                while (i < expr.Length && expr[i] == ' ')
                {
                    ++i;
                }
                if (i >= expr.Length) return LHS;
                Backbone.Expression.ExpressionType? type = null;
                switch (expr[i])
                {
                    case '+':
                        type = Backbone.Expression.ExpressionType.NumberAddition;
                        break;
                    case '-':
                        type = Backbone.Expression.ExpressionType.NumberSubtraction;
                        break;
                    case '*':
                        type = Backbone.Expression.ExpressionType.NumberMultiplication;
                        break;
                    case '/':
                        type = Backbone.Expression.ExpressionType.NumberDivision;
                        break;
                    case '&':
                        type = Backbone.Expression.ExpressionType.BooleanAnd;
                        break;
                    case '|':
                        type = Backbone.Expression.ExpressionType.BooleanOr;
                        break;
                    case '?':
                        type = Backbone.Expression.ExpressionType.NumberEquals;
                        break;
                    case '<':
                        type = Backbone.Expression.ExpressionType.NumberLessThan;
                        break;
                    case '>':
                        type = Backbone.Expression.ExpressionType.NumberGreaterThan;
                        break;
                    case '~':
                        type = Backbone.Expression.ExpressionType.StringAppending;
                        break;
                    case '\'':
                        type = Backbone.Expression.ExpressionType.StringEquals;
                        break;
                }
                if (type == null) return LHS;
                // check if we have RHS
                ++i;
                while (i < expr.Length && expr[i] == ' ')
                    ++i;
                if (i >= expr.Length)
                    return new Backbone.Expression(LHS, type!.Value, null);
                return new Backbone.Expression(LHS, type!.Value, ParseExpression(expr[i..]));
            }
        }

        public class Macro
        {

            public string key;
            public List<string> defArgs;
            public string? defMultiArg;

            public string? repMultiArg;
            public string expansion;
            public string? inplace;

            public Macro(string macro)
            {
                key = "";
                defArgs = new List<string>();
                expansion = "";

                int splitIndex = macro.IndexOf("=>");
                ParseDefinition(macro[6..splitIndex].Trim());
                ParseExpansion(macro[(splitIndex + 2)..].Trim());
            }

            void ParseDefinition(string definition)
            {
                int i = 0;
                key = "";
                while (i < definition.Length && definition[i].IsAscii())
                {
                    key += definition[i];
                    ++i;
                }
                if (i < definition.Length && definition[i] == '(')
                {
                    ++i;
                    int closing = definition.IndexOf(')');
                    Logger.Debug("defArgs: " + definition[i..closing]);
                    var args = definition[i..closing].Split(',');
                    foreach (var arg in args)
                        defArgs.Add(arg.Trim());
                    i = closing + 1;
                }
                if (i < definition.Length && definition[i] == '[')
                {
                    ++i;
                    int closing = definition.IndexOf(']');
                    defMultiArg = definition[i..closing];
                    i = closing + 1;
                }
            }

            void ParseExpansion(string expr)
            {
                expr = expr.Replace("\\n", "\n");
                // check for multiline
                int i = 0;
                if (expr[i] == '[')
                {
                    ++i;
                    int closing = expr.IndexOf(']');
                    repMultiArg = expr[i..closing];
                    i = closing + 1;
                }
                expansion = "";
                while (i < expr.Length && expr[i] != '{')
                {
                    expansion += expr[i];
                    ++i;
                }
                if (i < expr.Length && expr[i] == '{')
                {
                    ++i;
                    int closing = expr.IndexOf('}');
                    inplace = expr[i..closing];
                    i = closing + 1;
                }
            }

            public string Expand(string line)
            {
                // return the replacement of the whole line

                // parse caller
                int macroStart = line.IndexOf('^');
                int i = macroStart + 1;
                string callKey = "";
                while (i < line.Length && line[i].IsAscii())
                {
                    callKey += line[i];
                    ++i;
                }
                if (callKey != key) return ""; // eronous expansion

                List<string> callArgs = new List<string>();
                List<string> callMulti = new List<string>();
                if (i < line.Length && line[i] == '(')
                {
                    ++i;
                    // we need manual parsing, because strings can contain commas
                    string arg = "";
                    bool instr = false;
                    int depth = 1;
                    while (i < line.Length && depth > 0)
                    {
                        if (line[i] == ',' && !instr)
                        {
                            callArgs.Add(arg.Trim());
                            arg = "";
                        }
                        else
                        {
                            arg += line[i];
                            if (line[i] == '"')
                                instr = !instr;
                        }
                        ++i;
                        if (line[i] == '(') ++depth;
                        if (line[i] == ')') --depth;
                    }
                    if (arg.Trim() != "")
                        callArgs.Add(arg.Trim());
                    ++i;
                }
                if (i < line.Length && line[i] == '[')
                {
                    ++i;
                    // we need manual parsing, because strings can contain commas
                    string arg = "";
                    bool instr = false;
                    int depth = 1;
                    while (i < line.Length && depth > 0)
                    {
                        if (line[i] == ',' && !instr)
                        {
                            callMulti.Add(arg.Trim());
                            arg = "";
                        }
                        else
                        {
                            arg += line[i];
                            if (line[i] == '"')
                                instr = !instr;
                        }
                        ++i;
                        if (line[i] == '[') ++depth;
                        if (line[i] == ']') --depth;
                    }
                    if (arg.Trim() != "")
                        callMulti.Add(arg.Trim());
                    ++i;
                }

                string result = "";
                if (defMultiArg != null && repMultiArg != null)
                {
                    Logger.Debug("Expanding multi args");
                    int k = 0;
                    foreach (var arg in callMulti)
                    {
                        string exp = ApplyArgs(repMultiArg.Replace(defMultiArg, arg).Replace("%..%", k.ToString()), callArgs);
                        result += exp;
                        ++k;
                    }
                }
                Logger.Debug("Expanding macro");
                result += ApplyArgs(expansion, callArgs);
                if (inplace != null)
                {
                    Logger.Debug("Expanding in-place");
                    string exp = ApplyArgs(inplace, callArgs);
                    result += "\n" + line[..macroStart] + exp + line[i..];
                }
                return result;
            }

            string ApplyArgs(string str, List<string> callArgs)
            {
                if (callArgs.Count != defArgs.Count)
                    throw new Exception("Macro expansion called with invalid number of arguments"); // TODO: add exception
                for (int i = 0; i < callArgs.Count; ++i)
                {
                    str = str.Replace(defArgs[i], callArgs[i]);
                }
                return str;
            }

            public override string ToString()
            {
                return $"Macro(Key: {key} defArgs: {string.Join(',', defArgs)} multiArg: {defMultiArg} => repMultiArg: {repMultiArg} expansion: {expansion} inplace: {inplace})";
            }
        }

        public static class LibraryData
        {
            public delegate string stringPromise();
            public static Dictionary<string, stringPromise> libs = new Dictionary<string, stringPromise>();

            public static string GetLibrary(string lib)
            {
                if (!libs.ContainsKey(lib))
                {
                    throw new Exception("Unknown library " + lib); // TODO: add exception
                }
                return libs[lib]();
            }
        }

        public static class CharExtensions
        {
            public static bool IsAscii(this char c)
            {
                if (c >= 'a' && c <= 'z') return true;
                if (c >= 'A' && c <= 'Z') return true;
                if (c >= '0' && c <= '9') return true;
                if (c == '_') return true;
                return false;
            }
            public static bool IsPartOfNumber(this char c)
            {
                if (c >= '0' && c <= '9') return true;
                if (c == '.') return true;
                return false;
            }
        }
    }

    namespace Extensions
    {
        public static class ArraysExtension
        {
            public static void Register(ProgramScope scope)
            {
                if (scope.native.ContainsKey("amtaex_arrays_extension"))
                    return; // TODO: throw error
                scope.native.Add("amtaex_arrays_extension", new Dictionary<string, Variable[]>());
                scope.functions.Add("_amtaex_arrays_create", new NativeFunction((scope) => {
                    var namevar = scope.GetVariable("_amtaex_arrays_create_0");
                    if (namevar == null || namevar.var_type != Variable.VariableType.String) return;
                    var lengthvar = scope.GetVariable("_amtaex_arrays_create_1");
                    if (lengthvar == null || lengthvar.var_type != Variable.VariableType.Number) return;
                    var storage = (Dictionary<string, Variable[]>)scope.native["amtaex_arrays_extension"];
                    var array = new Variable[(int)lengthvar.num_value!];
                    for (int i = 0; i < array.Length; ++i)
                        array[i] = new Variable(i.ToString(), 0);
                    storage.Add(namevar.str_value!, array);
                    scope.UnregisterVariable("_amtaex_arrays_create_0");
                    scope.UnregisterVariable("_amtaex_arrays_create_1");
                    scope.native["amtaex_arrays_extension"] = storage;
                }));
                scope.functions.Add("_amtaex_arrays_get", new NativeFunction((scope) => {
                    var namevar = scope.GetVariable("_amtaex_arrays_get_0");
                    if (namevar == null || namevar.var_type != Variable.VariableType.String) return;
                    var idxvar = scope.GetVariable("_amtaex_arrays_get_1");
                    if (idxvar == null || idxvar.var_type != Variable.VariableType.Number) return;
                    var storage = (Dictionary<string, Variable[]>)scope.native["amtaex_arrays_extension"];
                    Variable? retvar = scope.GetVariable("_amtaex_arrays_get_ret");
                    if (retvar == null)
                        scope.RegisterVariable(new Variable("_amtaex_arrays_get_ret", storage[namevar.str_value!][(int)idxvar.num_value!]));
                    else
                        retvar.Assign(storage[namevar.str_value!][(int)idxvar.num_value!]);
                    scope.UnregisterVariable("_amtaex_arrays_get_0");
                    scope.UnregisterVariable("_amtaex_arrays_get_1");
                }));
                scope.functions.Add("_amtaex_arrays_set", new NativeFunction((scope) => {
                    var namevar = scope.GetVariable("_amtaex_arrays_set_0");
                    if (namevar == null || namevar.var_type != Variable.VariableType.String) return;
                    var idxvar = scope.GetVariable("_amtaex_arrays_set_1");
                    if (idxvar == null || idxvar.var_type != Variable.VariableType.Number) return;
                    var valvar = scope.GetVariable("_amtaex_arrays_set_2");
                    if (valvar == null) return;
                    var storage = (Dictionary<string, Variable[]>)scope.native["amtaex_arrays_extension"];
                    storage[namevar.str_value!][(int)idxvar.num_value!].Assign(valvar!);
                    scope.UnregisterVariable("_amtaex_arrays_set_0");
                    scope.UnregisterVariable("_amtaex_arrays_set_1");
                    scope.UnregisterVariable("_amtaex_arrays_set_2");
                    scope.native["amtaex_arrays_extension"] = storage;
                }));
            }
        }

        public static class StringExtension
        {
            public static void Register(ProgramScope scope)
            {
                if (scope.variables.ContainsKey("_amtaex_string__quote"))
                    return; // TODO: throw error
                scope.RegisterVariable(new Variable("_amtaex_string__quote", "\""));
            }
        }
    }
}