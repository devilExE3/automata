using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
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
                raw_lines = amtascript.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
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
                return new Backbone.CodeLines.VariableDeletion(line["delete$".Length..].Trim());
            }
            public static ICodeLine ParseFunctionCall(string line)
            {
                return new Backbone.CodeLines.FunctionCall(line[1..].Trim());
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
                expr = expr.Trim();
                while (expr[0] == '(' && expr.Last() == ')') // extract from bracketed expression
                    expr = expr[1..(expr.Length - 1)];
                // parse acording to operator orders
                // firstly, check for operators in at root level
                int depth = 0;
                bool instr = false;
                List<int> operators = new List<int>();
                for (int i = 0; i < expr.Length; ++i)
                {
                    if (expr[i] == '(') ++depth;
                    if (expr[i] == ')') --depth;
                    if (expr[i] == '"') instr = !instr;
                    if (expr[i] == '$')
                    {
                        // skip variable name
                        while (i + 1 < expr.Length && expr[i + 1].IsAscii())
                            ++i;
                        continue;
                    }
                    if (expr[i].IsOperator() && depth == 0 && !instr)
                        operators.Add(i);
                }
                if (operators.Count == 0)
                {
                    // default parsing (number, string, variable)
                    if (expr.StartsWith('$'))
                    {
                        // variable
                        string var_name = "";
                        for (int i = 1; i < expr.Length && expr[i].IsAscii(); ++i)
                        {
                            var_name += expr[i];
                        }
                        return new VariableResolver(var_name);
                    }
                    if (expr.StartsWith('"'))
                    {
                        // string
                        // get matching "
                        int end = expr.IndexOf('"', 1);
                        string constant = expr[1..end];
                        return new StringConstant(constant);
                    }
                    // number
                    string number_const = "";
                    for (int i = 0; i < expr.Length && expr[i].IsPartOfNumber(); ++i)
                    {
                        number_const += expr[i];
                    }
                    if (number_const == "")
                    {
                        throw new Exception("[TODO: add exception] Expected number, but got " + expr);
                    }
                    return new NumberConstant(double.Parse(number_const));
                }
                // determine last-most evaluated operator
                int ord = -1;
                List<int> splitIndexes = new List<int>();
                foreach (var op in operators)
                {
                    var this_ord = expr[op].OperatorOrder();
                    if (this_ord > ord)
                    {
                        ord = this_ord;
                        splitIndexes.Clear();
                        splitIndexes.Add(op);
                    }
                    else if (this_ord == ord && ord >= 0)
                    {
                        splitIndexes.Add(op);
                    }
                }
                if (ord == 0)
                {
                    return new Backbone.Expression(ParseExpression(expr[(splitIndexes[0] + 1)..]), expr[splitIndexes[0]].OperatorType(), null);
                }
                List<IEvaluable> expressions = new List<IEvaluable>();
                int last_idx = 0;
                splitIndexes.Add(expr.Length);
                foreach (var split_idx in splitIndexes)
                {
                    expressions.Add(ParseExpression(expr[last_idx..split_idx]));
                    last_idx = split_idx + 1;
                }
                splitIndexes.RemoveAt(splitIndexes.Count - 1);
                var old_LHS = new Backbone.Expression(expressions[0], expr[splitIndexes[0]].OperatorType(), expressions[1]);
                for (int i = 1; i < splitIndexes.Count; ++i)
                {
                    old_LHS = new Backbone.Expression(old_LHS, expr[splitIndexes[i]].OperatorType(), expressions[i + 1]);
                }
                return old_LHS;
            }

            public static readonly List<string> operator_order = new List<string> {
                // order from first to last to be evaluated
                "!_sl",
                "*/",
                "+-~",
                "?'<>",
                "&",
                "|"
            };
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
                    int par_depth = 0;
                    int depth = 1;
                    while (i < line.Length && depth > 0)
                    {
                        if (line[i] == '(') ++par_depth;
                        if (line[i] == ')') --par_depth;
                        if (line[i] == ',' && !instr && par_depth == 0)
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
                        if (i >= line.Length) continue;
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
                    int par_depth = 0;
                    int depth = 1;
                    while (i < line.Length && depth > 0)
                    {
                        if (line[i] == '(') ++par_depth;
                        if (line[i] == ')') --par_depth;
                        if (line[i] == ',' && !instr && par_depth == 0)
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
            public static bool IsOperator(this char c)
            {
                return "*/+-~<>?'&|!_sl".Contains(c);
            }
            public static int OperatorOrder(this char c)
            {
                int i = 0;
                foreach (var list in ExpressionParser.operator_order)
                {
                    if (list.Contains(c))
                        return i;
                    i++;
                }
                return -1;
            }
            public static Backbone.Expression.ExpressionType OperatorType(this char c)
            {
                switch (c)
                {
                    case '+':
                        return Backbone.Expression.ExpressionType.NumberAddition;
                    case '-':
                        return Backbone.Expression.ExpressionType.NumberSubtraction;
                    case '*':
                        return Backbone.Expression.ExpressionType.NumberMultiplication;
                    case '/':
                        return Backbone.Expression.ExpressionType.NumberDivision;
                    case '&':
                        return Backbone.Expression.ExpressionType.BooleanAnd;
                    case '|':
                        return Backbone.Expression.ExpressionType.BooleanOr;
                    case '?':
                        return Backbone.Expression.ExpressionType.NumberEquals;
                    case '<':
                        return Backbone.Expression.ExpressionType.NumberLessThan;
                    case '>':
                        return Backbone.Expression.ExpressionType.NumberGreaterThan;
                    case '~':
                        return Backbone.Expression.ExpressionType.StringAppending;
                    case '\'':
                        return Backbone.Expression.ExpressionType.StringEquals;
                    // unary operators
                    case '!':
                        return Backbone.Expression.ExpressionType.BooleanNot;
                    case '_':
                        return Backbone.Expression.ExpressionType.NumberFlooring;
                    case 's':
                        return Backbone.Expression.ExpressionType.IntToString;
                    case 'l':
                        return Backbone.Expression.ExpressionType.StringLowercase;
                }
                throw new Backbone.LogicEngine.UnknownOperatorException("Unknown operator " + c);
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

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_arrays_create_0", Variable.VariableType.String)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_arrays_create_1", Variable.VariableType.Number)) return;
                    var name = Utilities.GetString(scope, "_amtaex_arrays_create_0");
                    var length = (int)Utilities.GetDouble(scope, "_amtaex_arrays_create_1");
                    scope.UnregisterVariable("_amtaex_arrays_create_0");
                    scope.UnregisterVariable("_amtaex_arrays_create_1");
                    if (length < 1) return;

                    var storage = (Dictionary<string, Variable[]>)scope.native["amtaex_arrays_extension"];
                    var array = new Variable[length];
                    for (int i = 0; i < array.Length; ++i)
                        array[i] = new Variable(i.ToString(), 0);
                    storage.Add(name, array);
                    scope.native["amtaex_arrays_extension"] = storage;
                }));
                scope.functions.Add("_amtaex_arrays_get", new NativeFunction((scope) => {

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_arrays_get_0", Variable.VariableType.String)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_arrays_get_1", Variable.VariableType.Number)) return;
                    var name = Utilities.GetString(scope, "_amtaex_arrays_get_0");
                    var idx = (int)Utilities.GetDouble(scope, "_amtaex_arrays_get_1");
                    scope.UnregisterVariable("_amtaex_arrays_get_0");
                    scope.UnregisterVariable("_amtaex_arrays_get_1");
                    if (idx < 0)
                    {
                        Utilities.ReturnValue(scope, "_amtaex_arrays_get_ret", "[idx < 0]");
                        return;
                    }
                    var storage = (Dictionary<string, Variable[]>)scope.native["amtaex_arrays_extension"];
                    if(!storage.ContainsKey(name))
                    {
                        Utilities.ReturnValue(scope, "_amtaex_arrays_get_ret", "[non-existing array]");
                        return;
                    }
                    if (idx >= storage[name].Length)
                    {
                        Utilities.ReturnValue(scope, "_amtaex_arrays_get_ret", "[idx >= array length]");
                        return;
                    }
                    Utilities.ReturnValue(scope, "_amtaex_arrays_get_ret", storage[name][idx]);
                }));
                scope.functions.Add("_amtaex_arrays_set", new NativeFunction((scope) => {

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_arrays_set_0", Variable.VariableType.String)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_arrays_set_1", Variable.VariableType.Number)) return;
                    if (!Utilities.EnforceArgument(scope, "_amtaex_arrays_set_2")) return;
                    var name = Utilities.GetString(scope, "_amtaex_arrays_set_0");
                    var idx = (int)Utilities.GetDouble(scope, "_amtaex_arrays_set_1");
                    var value = scope.GetVariable("_amtaex_arrays_set_2")!;
                    scope.UnregisterVariable("_amtaex_arrays_set_0");
                    scope.UnregisterVariable("_amtaex_arrays_set_1");
                    scope.UnregisterVariable("_amtaex_arrays_set_2");
                    if (idx < 0) return;

                    var storage = (Dictionary<string, Variable[]>)scope.native["amtaex_arrays_extension"];
                    storage[name][idx].Assign(value);
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
                scope.functions.Add("_amtaex_string_length", new NativeFunction((scope) => {
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_length_0", Variable.VariableType.String)) return;
                    var str = Utilities.GetString(scope, "_amtaex_string_length_0");
                    scope.UnregisterVariable("_amtaex_string_length_0");

                    Utilities.ReturnValue(scope, "_amtaex_string_length_ret", str.Length);
                }));
                scope.functions.Add("_amtaex_string_charat", new NativeFunction((scope) => {

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_charat_0", Variable.VariableType.String)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_charat_1", Variable.VariableType.Number)) return;
                    var str = Utilities.GetString(scope, "_amtaex_string_charat_0");
                    var idx = (int)Utilities.GetDouble(scope, "_amtaex_string_charat_1");
                    scope.UnregisterVariable("_amtaex_string_charat_0");
                    scope.UnregisterVariable("_amtaex_string_charat_1");
                    if (idx < 0) {
                        Utilities.ReturnValue(scope, "_amtaex_string_charat_ret", "idx < 0");
                        return;
                    }
                    if (idx >= str.Length)
                    {
                        Utilities.ReturnValue(scope, "_amtaex_string_charat_ret", "idx >= string length");
                        return;
                    }

                    Utilities.ReturnValue(scope, "_amtaex_string_charat_ret", str[idx].ToString());
                }));
                scope.functions.Add("_amtaex_string_setchar", new NativeFunction((scope) => {

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_setchar_0", Variable.VariableType.String)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_setchar_1", Variable.VariableType.Number)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_setchar_2", Variable.VariableType.String)) return;
                    var str = Utilities.GetString(scope, "_amtaex_string_setchar_0");
                    var idx = (int)Utilities.GetDouble(scope, "_amtaex_string_setchar_1");
                    var newchr = Utilities.GetString(scope, "_amtaex_string_setchar_2");
                    scope.UnregisterVariable("_amtaex_string_setchar_0");
                    scope.UnregisterVariable("_amtaex_string_setchar_1");
                    scope.UnregisterVariable("_amtaex_string_setchar_2");
                    if (idx < 0)
                    {
                        Utilities.ReturnValue(scope, "_amtaex_string_setchar_ret", "idx < 0");
                        return;
                    }
                    if (idx >= str.Length)
                    {
                        Utilities.ReturnValue(scope, "_amtaex_string_setchar_ret", "idx >= string length");
                        return;
                    }
                    if (newchr.Length != 1)
                    {
                        Utilities.ReturnValue(scope, "_amtaex_string_setchar_ret", $"\"{newchr}\" is not a single character");
                        return;
                    }

                    var charr = str.ToCharArray();
                    charr[idx] = newchr[0];
                    Utilities.ReturnValue(scope, "_amtaex_string_setchar_ret", new string(charr));
                }));
                scope.functions.Add("_amtaex_string_pad", new NativeFunction((scope) => {

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_pad_0", Variable.VariableType.String)) return;
                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_pad_1", Variable.VariableType.Number)) return;
                    var str = Utilities.GetString(scope, "_amtaex_string_pad_0");
                    var padding = (int)Utilities.GetDouble(scope, "_amtaex_string_pad_1");
                    scope.UnregisterVariable("_amtaex_string_pad_0");
                    scope.UnregisterVariable("_amtaex_string_pad_1");
                    if (padding < 0) return;

                    while (str.Length < padding)
                        str = " " + str;

                    Utilities.ReturnValue(scope, "_amtaex_string_pad_ret", str);
                }));
                scope.functions.Add("_amtaex_string_number", new NativeFunction((scope) => {

                    if (!Utilities.EnforceArgumentType(scope, "_amtaex_string_number_0", Variable.VariableType.String)) return;
                    var str = Utilities.GetString(scope, "_amtaex_string_number_0");
                    scope.UnregisterVariable("_amtaex_string_number_0");

                    if (double.TryParse(str, out double n))
                        Utilities.ReturnValue(scope, "_amtaex_string_number_ret", n);
                    else
                        Utilities.ReturnValue(scope, "_amtaex_string_number_ret", 0);
                }));
                scope.functions.Add("_amtaex_string_readline", new NativeFunction((scope) => {
                    Utilities.ReturnValue(scope, "_amtaex_string_readline_ret", Console.ReadLine()!);
                }));
            }
        }

        static class Utilities
        {
            public static void ReturnValue(ProgramScope scope, string variable_name, double value)
            {
                Variable? retvar = scope.GetVariable(variable_name);
                if (retvar == null)
                    scope.RegisterVariable(new Variable(variable_name, value));
                else
                    retvar.Assign(value);
            }
            public static void ReturnValue(ProgramScope scope, string variable_name, string value)
            {
                Variable? retvar = scope.GetVariable(variable_name);
                if (retvar == null)
                    scope.RegisterVariable(new Variable(variable_name, value));
                else
                    retvar.Assign(value);
            }
            public static void ReturnValue(ProgramScope scope, string variable_name, Variable copyFrom)
            {
                Variable? retvar = scope.GetVariable(variable_name);
                if (retvar == null)
                    scope.RegisterVariable(new Variable(variable_name, copyFrom));
                else
                    retvar.Assign(copyFrom);
            }
            public static bool EnforceArgument(ProgramScope scope, string variable_name)
            {
                Variable? argvar = scope.GetVariable(variable_name);
                if (argvar == null)
                    return false;
                return true;
            }
            public static bool EnforceArgumentType(ProgramScope scope, string variable_name, Backbone.Variable.VariableType type)
            {
                Variable? argvar = scope.GetVariable(variable_name);
                if (argvar == null || argvar.var_type != type)
                    return false;
                return true;
            }
            public static string GetString(ProgramScope scope, string variable_name)
            {
                return scope.GetVariable(variable_name)!.str_value!;
            }
            public static double GetDouble(ProgramScope scope, string variable_name)
            {
                return scope.GetVariable(variable_name)!.num_value!.Value;
            }
        }
    }
}