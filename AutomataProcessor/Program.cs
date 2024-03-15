using System.Linq.Expressions;
using System.Runtime.InteropServices;
using AutomataProcessor.Backbone;
using AutomataProcessor.Parser;
using Microsoft.VisualBasic;

namespace AutomataProcessor {
    static class Program {
        public static void Main(string[] args) {
            Logger.Print("Automata Processor\nMade by devilexe");
            Logger.Debug(Directory.GetCurrentDirectory());

            // load libraries
            foreach(var file in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "lib"))) {
                if(file.EndsWith(".amtascript")) {
                    LibraryData.libs.Add(Path.GetFileNameWithoutExtension(file), () => File.ReadAllText(file));
                }
            }

            Config.MaxWhileLoops = int.MaxValue; // disable while limit
            // Config.PrintDebugMessages = true;
            ProgramScope scope = new ProgramScope("Automata CLI");
            string amtascript = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "script.amtascript"));
            Logger.Debug("Loaded script:\n" + amtascript);

            // register print function
            scope.RegisterFunction("print", new NativeFunction((scope) => {
                Variable? var_str = scope.GetVariable("print_string");
                if(var_str == null)
                    var_str = scope.GetVariable("print_0");
                if(var_str == null) {
                    Logger.Print("[Automoata] Print function called, but no parameter provided. Use $print_string of $print_0");
                    return;
                }
                if(var_str.var_type != Variable.VariableType.String) {
                    Logger.Print("[Automata] Print function called, but argument was not string");
                    return;
                }
                Logger.Print(var_str.str_value!);
                scope.UnregisterVariable(var_str.name);
            }));
            scope.RegisterFunction("print_scope", new NativeFunction((scope) => {
                Logger.Print(scope.ToString());
            }));
            // register arrays extension
            Extensions.ArraysExtension.Register(scope);

            ProgramParser parser = new ProgramParser(amtascript);
            parser.Parse();
            foreach(var function in parser.functions)
                Logger.Debug(function.ToString());
            scope.ImportParser(parser);
            ICallable? main_function = scope.GetFunction("main");
            if(main_function == null)
                Logger.Print("No main function");
            else
                main_function.Call(scope);
        }
    }
}