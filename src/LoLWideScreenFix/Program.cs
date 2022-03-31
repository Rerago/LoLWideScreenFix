using CommandLine;
using System;
using System.Reflection;

namespace LoLWideScreenFix
{
    class Program
    {
        /// <summary>
        /// Possible return codes of the application
        /// </summary>
        private enum ReturnCodes
        {
            RUN_SUCCES = 0,

            UNKNOWN_OUTPUT_MODE = -1,
            CREATE_MOD_FAILED = -2,
            ERROR_PARSING_ARGUMENTS = -3,

            UNKNOWN_ERROR = -666
        }

        /// <summary>
        /// Possible output options for the mod
        /// </summary>
        private enum OutputMode
        {
            RAW_MOD_FOLDER = 1,
            LOLCUSTOMSKIN_MOD = 2,
        }

        /// <summary>
        /// Defines the arguments that the user can use to control the application.
        /// </summary>
        private class ArgumentsOptions
        {
            [Option('l', "leaguepath", Required = true, HelpText = "Path to Leagues of Legends folder")]
            public string LeaguesOfLegendsPath { get; set; }

            [Option('o', "outputpath", Required = true, HelpText = "Path where the result should be generated")]
            public string OutputPath { get; set; }

            [Option('m', "outputmode", HelpText = "Mode in which the output should be done")]
            public OutputMode? OutputMode { get; set; }

            [Option('t', "targetres", Required = true, HelpText = "target resolution width")]
            public uint TargetResolutionWidth { get; set; }
        }

        public static int Main(string[] args)
        {
            // Return variable
            int returncode = (int)ReturnCodes.RUN_SUCCES;

            // Set program title
            Console.Title = GetTitleString();

#if DEBUG
            // Set fake values for testing
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //args = new string[] {
                //    @"-l D:\Riot Games\League of Legends\",
                //    Environment.ExpandEnvironmentVariables(@"-o %UserProfile%\Desktop\LoLModding\WideScreenFix\"),
                //    "-t 1920",
                //    $"-m {OutputMode.RAW_MOD_FOLDER}"
                //};

                args = new string[] {
                    @"-l E:\Riot Games\League of Legends\",
                    Environment.ExpandEnvironmentVariables(@"-o %UserProfile%\Desktop\LoLModding\cslol-manager\installed\"),
                    "-t 1920",
                    $"-m {OutputMode.LOLCUSTOMSKIN_MOD}"
                };
            }
#endif

            try
            {
                // Parse parameters and start the task accordingly
                returncode = Parser.Default.ParseArguments<ArgumentsOptions>(args).MapResult(
                    (ArgumentsOptions opts) => MainTask(opts),
                    errs => (int)ReturnCodes.ERROR_PARSING_ARGUMENTS);

                // If the task was not successful make output if necessary
                if (returncode != (int)ReturnCodes.RUN_SUCCES)
                {
                    // Depending on the return code output error message
                    if (returncode == (int)ReturnCodes.UNKNOWN_OUTPUT_MODE)
                        WriteLog($"Error: An unknown output mode was selected.");
                    else
                        WriteLog($"Error: {(ReturnCodes)returncode}");
                }
            }
            catch (Exception exc)
            {
                // Output message
                WriteLog($"Unknown-Error: {exc.InnerException?.Message ?? exc.Message}");

                // Adjust return code
                returncode = (int)ReturnCodes.UNKNOWN_ERROR;
            }

            // Return code
            return returncode;
        }

        private static int MainTask(ArgumentsOptions argsParsed)
        {
            // Return variable
            int returncode = (int)ReturnCodes.RUN_SUCCES;

            // Determine Title
            var title = GetTitleString();

            // Write Title/Header
            Console.WriteLine(title);
            Console.WriteLine(new string('=', title.Length));
            Console.WriteLine();

            // Trim parameters to reduce input errors, if any.
            argsParsed.LeaguesOfLegendsPath = argsParsed.LeaguesOfLegendsPath?.Trim().Trim('"');
            argsParsed.OutputPath = argsParsed.OutputPath?.Trim().Trim('"');

            // Output settings for the user
            WriteLog($"LoL directory:\t{argsParsed.LeaguesOfLegendsPath}");
            WriteLog($"Output directory:\t{argsParsed.OutputPath}");
            WriteLog($"target resolution :\t{argsParsed.TargetResolutionWidth}p");
            WriteLog();

            // Check if a correct output mode has been selected
            if (!argsParsed.OutputMode.HasValue || !Enum.IsDefined(typeof(OutputMode), argsParsed.OutputMode))
            {
                // Adjust return code
                returncode = (int)ReturnCodes.UNKNOWN_OUTPUT_MODE;
            }
            else
            {
                // Variable for result of mod creation
                var createModResult = false;

                // Start the correct job depending on the mode
                if (argsParsed.OutputMode == OutputMode.RAW_MOD_FOLDER)
                    createModResult = GenericExtResult("Create RAW-ModFolder", () => ModCreater.CreateRawModFolder(argsParsed.LeaguesOfLegendsPath, argsParsed.OutputPath, argsParsed.TargetResolutionWidth));
                else if (argsParsed.OutputMode == OutputMode.LOLCUSTOMSKIN_MOD)
                    createModResult = GenericExtResult("Create LoLCustomSkinMod", () => ModCreater.CreatLoLCustomSkinMod(argsParsed.LeaguesOfLegendsPath, argsParsed.OutputPath, argsParsed.TargetResolutionWidth));

                // Adjust return code if necessary
                if (!createModResult)
                    returncode = (int)ReturnCodes.UNKNOWN_OUTPUT_MODE;
            }

            // Return
            return returncode;
        }

        #region Helper
        /// <summary>
        /// Executes a task, displays it in the console and returns the result.
        /// </summary>
        /// <param name="taskTitle">Title used for the output</param>
        /// <param name="func"><see cref="Action"/> to be executed</param>
        /// <returns>TRUE if the action was successful, FALSE otherwise.</returns>
        private static bool GenericExtResult(string taskTitle, Action func)
        {
            // Return variable
            var returnVal = true;

            // Variable for finale output
            var resultMessage = string.Empty;

            // Idle output
            WriteLog(string.Concat(taskTitle, "..."), false);

            try
            {
                // Execute task
                func?.Invoke();

                // Adjust output
                resultMessage = "DONE";
            }
            catch (Exception exc)
            {
                // Adjust output
                resultMessage = $"FAILED ({exc.Message})";
            }

            // Final output
            WriteLog(string.Concat(taskTitle, ":\t", resultMessage), false, true);

            // Return
            return returnVal;
        }

        /// <summary>
        /// Determines the title for the console and the header of the output
        /// </summary>
        /// <returns>Title for the console and the header of the output</returns>
        private static string GetTitleString()
        {
            // Determine current assembly
            var currentAsm = Assembly.GetExecutingAssembly().GetName();

            // Compose title
            return $"{currentAsm.Name} v{currentAsm.Version.ToString(currentAsm.Version.Revision != 0 || currentAsm.Version.MinorRevision != 0 ? 4 : (currentAsm.Version.Build != 0) ? 3 : 2)}";
        }

        /// <summary>
        /// Writes the contents to the console (specifying the time) and allows the line to be updated.
        /// </summary>
        /// <param name="message">Message that is to be output.</param>
        /// <param name="endLine">Specifies whether the line should be closed.</param>
        /// <param name="newline">Indicates whether this is a new line</param>
        private static void WriteLog(string message = "", bool endLine = true, bool newline = false)
        {
            // Determines the content to be output (if there is an empty line, no time is output).
            var content = !string.IsNullOrEmpty(message) ? $"[{DateTime.Now.ToLongTimeString()}] {message}" : string.Empty;

            // Wird die Zeile abgeschlossen? => Zeile normal ausgeben
            if (endLine)
                Console.WriteLine(content);
            else
            {
                // Write line but do not close it to update it later if necessary.
#if NETCOREAPP
                System.Console.Write($"\r{new string(' ', System.Console.BufferWidth)}\r{content}");
#else
                System.Console.Write("\r" + content);
#endif
            }

            // Complete an unfinished line?
            if (!endLine && newline)
                Console.WriteLine();
        }
        #endregion
    }
}
