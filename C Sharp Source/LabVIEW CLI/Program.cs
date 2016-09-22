﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LabVIEW_CLI
{
    class Program
    {
        static int Main(string[] args)
        {

            bool stop = false;
            int exitCode = 0;
            Output output = Output.Instance;

            string[] cliArgs, lvArgs;
            lvComms lvInterface = new lvComms();
            lvMsg latestMessage = new lvMsg("NOOP", "");
            LvLauncher launcher;
            CliOptions options = new CliOptions();

            lvVersion current = LvVersions.CurrentVersion;

            splitArguments(args, out cliArgs, out lvArgs);
            CommandLine.Parser.Default.ParseArguments(cliArgs, options);

            if (cliArgs.Length < 1)
            {
                output.writeError("No Arguments Used");
                return -1;
            }
            else
            {
                string launchPath = cliArgs[cliArgs.Length - 1];

                output.setVerbose(options.Verbose);
                output.writeInfo("LabVIEW CLI Started - Verbose Mode");
                output.writeInfo("Version " + Assembly.GetExecutingAssembly().GetName().Version);
                output.writeInfo("LabVIEW CLI Arguments: " + String.Join(" ", cliArgs));


                // Args don't include the exe name.
                if (options.noLaunch)
                {
                    output.writeMessage("Auto Launch Disabled");
                }
                else
                {
                    try
                    {
                        launcher = new LvLauncher(launchPath, lvPathFinder(options), lvInterface.port, lvArgs);
                        launcher.Start();
                    }
                    catch(KeyNotFoundException ex)
                    {
                        // Fail gracefully if lv-ver option cannot be resolved
                        string bitness = options.x64 ? " 64bit" : string.Empty;
                        output.writeError("LabVIEW version \"" + options.lvVer + bitness + "\" not found!");
                        output.writeMessage("Available LabVIEW versions are:");
                        foreach(var ver in LvVersions.Versions)
                        {
                            output.writeMessage(ver.ToString());
                        }
                        return 1;
                    }
                    catch(FileNotFoundException ex)
                    {
                        output.writeError(ex.Message);
                        return 1;
                    }                    
                }

                lvInterface.waitOnConnection();

                do
                {
                    latestMessage = lvInterface.readMessage();

                    switch (latestMessage.messageType)
                    {
                        case "OUTP":
                            Console.Write(latestMessage.messageData);
                            break;
                        case "EXIT":
                            exitCode = lvInterface.extractExitCode(latestMessage.messageData);
                            output.writeMessage("Recieved Exit Code " + exitCode);
                            stop = true;
                            break;
                        case "RDER":
                            exitCode = 1;
                            output.writeError("Read Error");
                            stop = true;
                            break;
                        default:
                            output.writeError("Unknown Message Type Recieved:" + latestMessage.messageType);
                            break;
                    }


                } while (!stop);

                lvInterface.Close();
                return exitCode;
            }
        }

        private static void splitArguments(string[] args, out string[] cliArgs, out string[] lvArgs)
        {

            int splitterLocation = -1;

            for(int i = 0; i < args.Length; i++)
            {
                if(args[i] == "--")
                {
                    splitterLocation = i;
                }
            }

            if(splitterLocation > 0)
            {
                cliArgs = args.Take(splitterLocation).ToArray();
                lvArgs = args.Skip(splitterLocation + 1).ToArray();
            }
            else
            {
                cliArgs = args;
                lvArgs = new string[0];
            }

        }
        private static string lvPathFinder(CliOptions options)
        {
            if (options.lvExe != null)
            {
                if (!File.Exists(options.lvExe))
                    throw new FileNotFoundException("specified executable not found", options.lvExe);
                return options.lvExe;
            }
            if (options.lvVer != null)
            {
                try
                {
                    return LvVersions.ResolveVersionString(options.lvVer, options.x64).ExePath;
                }
                catch(KeyNotFoundException ex)
                {
                    throw; // So the exception makes it to the handler above.
                }
            }
            if (LvVersions.CurrentVersion != null)
            {
                return LvVersions.CurrentVersion.ExePath;
            }
            else
            {
                throw new FileNotFoundException("No LabVIEW.exe found...", "LabVIEW.exe");
            }
        }

    }


}
