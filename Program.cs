using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace OBODataConverter
{
    /// <summary>
    /// This program reads an Ontology file in the OBO format and converts the data to a tab-delimited text file.
    /// </summary>
    /// <remarks>
    /// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
    ///
    /// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
    /// Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov
    /// </remarks>
    class Program
    {
        public const string PROGRAM_DATE = "March 27, 2020";

        private static string mInputFilePath;
        private static string mOutputFilePath;
        private static clsOboConverter.udtOutputOptions mOutputOptions;
        private static string mPrimaryKeySuffix;

        static int Main(string[] args)
        {
            var commandLineParser = new clsParseCommandLine();

            mInputFilePath = string.Empty;
            mOutputFilePath = string.Empty;

            mOutputOptions = clsOboConverter.DefaultOutputOptions();

            mPrimaryKeySuffix = clsOboConverter.DEFAULT_PRIMARY_KEY_SUFFIX;

            try
            {

                var success = false;

                if (commandLineParser.ParseCommandLine())
                {
                    if (SetOptionsUsingCommandLineParameters(commandLineParser))
                        success = true;
                }

                if (!success ||
                    commandLineParser.NeedToShowHelp ||
                    string.IsNullOrWhiteSpace(mInputFilePath))
                {
                    ShowProgramHelp();
                    return -1;
                }

                var converter = new clsOboConverter(mPrimaryKeySuffix)
                {
                    OutputOptions = mOutputOptions
                };

                converter.ErrorEvent += Converter_ErrorEvent;
                converter.StatusEvent += Converter_StatusEvent;
                converter.WarningEvent += Converter_WarningEvent;

                success = converter.ConvertOboFile(mInputFilePath, mOutputFilePath);

                if (!success)
                {
                    ConsoleMsgUtils.ShowError("ConvertOboFile returned false");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            return 0;

        }

        private static void Converter_ErrorEvent(string message, Exception ex)
        {
            ConsoleMsgUtils.ShowError(message);
        }

        private static void Converter_StatusEvent(string message)
        {
            Console.WriteLine(message);
        }
        private static void Converter_WarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }

        private static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false
            var lstValidParameters = new List<string> {
                "I", "O",
                "PK", "NoP", "NoG",
                "Def", "Definition", "StripQuotes",
                "Com", "Comm", "Comment",
                "NoObsolete"};

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(lstValidParameters))
                {
                    var badArguments = new List<string>();
                    foreach (var item in commandLineParser.InvalidParameters(lstValidParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ConsoleMsgUtils.ShowErrors("Invalid command line parameters", badArguments);

                    return false;
                }

                // Query commandLineParser to see if various parameters are present
                if (commandLineParser.NonSwitchParameterCount > 0)
                {
                    mInputFilePath = commandLineParser.RetrieveNonSwitchParameter(0);
                }

                if (commandLineParser.NonSwitchParameterCount > 1)
                {
                    mOutputFilePath = commandLineParser.RetrieveNonSwitchParameter(1);
                }

                if (commandLineParser.RetrieveValueForParameter("I", out var paramValue))
                {
                    mInputFilePath = string.Copy(paramValue);
                }

                if (commandLineParser.RetrieveValueForParameter("O", out paramValue))
                {
                    mOutputFilePath = string.Copy(paramValue);
                }

                if (commandLineParser.RetrieveValueForParameter("PK", out paramValue))
                {
                    mPrimaryKeySuffix = string.Copy(paramValue);
                }

                if (commandLineParser.IsParameterPresent("NoP"))
                {
                    mOutputOptions.IncludeParentTerms = false;
                    mOutputOptions.IncludeGrandparentTerms = false;
                }

                if (commandLineParser.IsParameterPresent("NoG"))
                    mOutputOptions.IncludeGrandparentTerms = false;

                if (commandLineParser.IsParameterPresent("Def") ||
                    commandLineParser.IsParameterPresent("Definition"))
                    mOutputOptions.IncludeDefinition = true;

                if (commandLineParser.IsParameterPresent("StripQuotes"))
                {
                    mOutputOptions.IncludeDefinition = true;
                    mOutputOptions.StripQuotesFromDefinition = true;
                }

                if (commandLineParser.IsParameterPresent("Com") ||
                    commandLineParser.IsParameterPresent("Comm") ||
                    commandLineParser.IsParameterPresent("Comment"))
                    mOutputOptions.IncludeComment = true;

                if (commandLineParser.IsParameterPresent("NoObsolete"))
                {
                    mOutputOptions.ExcludeObsolete = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error parsing the command line parameters", ex);
            }

            return false;
        }

        private static void ShowProgramHelp()
        {
            var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            try
            {
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "This program reads an Ontology file in the OBO format and converts the data to a tab-delimited text file."));
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + exeName);
                Console.WriteLine(" InputFilePath [/O:OutputFilePath] [/PK:Suffix] [/NoP] [/NoG] [/Def] [/StripQuotes] [/Com] [/NoObsolete]");
                Console.WriteLine();
                Console.WriteLine("The input file is the OBO file to convert");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Optionally use /O to specify the output path. " +
                                      "If not provided the output file will have extension .txt or .txt.new"));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "Use /PK to specify the string to append to the ontology term identifier" +
                                      "when creating the primary key for the Term_PK column. By default uses /PK:MS1"));
                Console.WriteLine();
                Console.WriteLine("By default the output file includes parent terms; remove them with /NoP");
                Console.WriteLine("By default the output file includes grandparent terms; remove them with /NoG");
                Console.WriteLine("Using /NoP auto-enables /NoG");
                Console.WriteLine();
                Console.WriteLine("By default the output file will not include the term definitions; include them with /Def");
                Console.WriteLine();
                Console.WriteLine("When using /Def, use /StripQuotes to look for definitions of the form");
                Console.WriteLine(@"  ""Description of term"" [Ontology:Source]");
                Console.WriteLine("and only include the text between the double quotes as the definition");
                Console.WriteLine("Using /StripQuotes auto-enables /Def");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                                      "By default the output file will not include the term comments; include them with /Com"));
                Console.WriteLine();
                Console.WriteLine("Use /NoObsolete to exclude obsolete terms");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2016");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://panomics.pnnl.gov/ or https://omics.pnl.gov");
                Console.WriteLine();

                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                System.Threading.Thread.Sleep(750);

            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error displaying the program syntax", ex);
            }

        }

    }
}
