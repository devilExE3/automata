using System.Linq.Expressions;
using System.Runtime.InteropServices;
using AutomataProcessor.Backbone;
using AutomataProcessor.Parser;
using Microsoft.VisualBasic;

namespace AutomataProcessor
{
    static class Program
    {
        private static void PrintHelp()
        {
            Console.WriteLine("General Usage:\n  amta file_name [--lib lib_folder] [--debug] [--max_while_loops N]\n\nfile_name is the file which will be interpreted.\nThe function called when running is !main\n\n--lib lib_folder - specify another lib folder.\nDefault: ./lib\n\n--debug - print debug messages from AutoMaTA\n\n--max_while_loops N - specify maximum number of times a while block can run.\nIf you want to disable the limit, use -1\nDefault: 10000\n\namta --help");
        }

        public static void Main(string[] args)
        {
            Logger.Print("Automata Processor\nMade by devilexe\n\n");

            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            string lib_folder = Path.Combine(Directory.GetCurrentDirectory(), "lib");
            string? amta_file = null;
            for(int i = 0; i < args.Length; ++i)
            {
                if (args[i].StartsWith("--"))
                {
                    switch (args[i])
                    {
                        case "--help":
                            PrintHelp();
                            return;
                        case "--lib":
                            lib_folder = args[i + 1];
                            ++i;
                            break;
                        case "--debug":
                            Config.PrintDebugMessages = true;
                            break;
                        case "--max_while_loops":
                            if (int.TryParse(args[i + 1], out int N))
                            {
                                if (N == -1)
                                    Config.MaxWhileLoops = int.MaxValue; // disable while limit
                                else
                                    Config.MaxWhileLoops = N;
                            }
                            else
                            {
                                Console.WriteLine("Invalid max_while_loops parameter: " + args[i + 1]);
                                return;
                            }
                            break;
                    }
                }
                else if(i == 0)
                    amta_file = args[i];
            }
            if(amta_file == null)
            {
                PrintHelp();
                return;
            }
            if(!File.Exists(amta_file))
            {
                Console.WriteLine("File doesn't exist: " + amta_file + "\n\nPlease use \"amta --help\" for see a help page");
                return;
            }

            // load libraries
            foreach (var file in Directory.GetFiles(lib_folder))
            {
                if (file.EndsWith(".amtascript"))
                {
                    LibraryData.libs.Add(Path.GetFileNameWithoutExtension(file), () => File.ReadAllText(file));
                }
            }

            ProgramScope scope = new ProgramScope("AutoMaTA Processor");
            string amtascript = File.ReadAllText(amta_file);
            Logger.Debug("Loaded script:\n" + amtascript);

            // register print function
            scope.RegisterFunction("print", new NativeFunction((scope) => {
                Variable? var_str = scope.GetVariable("print_string");
                if (var_str == null)
                    var_str = scope.GetVariable("print_0");
                if (var_str == null)
                {
                    Logger.Print("[AutoMaTA] Print function called, but no parameter provided. Use $print_string or $print_0");
                    return;
                }
                if (var_str.var_type != Variable.VariableType.String)
                {
                    Logger.Print("[AutoMaTA] Print function called, but argument was not string");
                    return;
                }
                Logger.Print(var_str.str_value!);
                scope.UnregisterVariable(var_str.name);
            }));
            scope.RegisterFunction("print_scope", new NativeFunction((scope) => {
                Logger.Print("[AutoMaTA] Current Scope: ");
                Logger.Print(scope.ToString());
            }));
            // register extensions
            Extensions.ArraysExtension.Register(scope);
            Extensions.StringExtension.Register(scope);

            ProgramParser parser = new ProgramParser(amtascript);
            parser.Parse();
            foreach (var function in parser.functions)
                Logger.Debug(function.ToString());
            scope.ImportParser(parser);
            ICallable? main_function = scope.GetFunction("main");
            if (main_function == null)
                Logger.Print("[AutoMaTA] No main function");
            else
                main_function.Call(scope);
        }
    }
}