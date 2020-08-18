using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Colorful;
using CO = Colorful.Console;
using Figgle;
using CommandLine;
using CommandLine.Text;

namespace SMApp
{
    #region Enums
    public enum ExitResult
    {
        SUCCESS = 0,
        UNHANDLED_EXCEPTION = 1,
        INVALID_OPTIONS = 2,
        UNKNOWN_ERROR = 3,
        NOT_FOUND_OR_SERVER_ERROR = 4
    }
    #endregion

    class Program : Api
    {
        #region Entry-point
        static void Main(string[] args)
        {
            Args = args;
            if (Args.Contains("cui"))
            {
                SetLogger(new SerilogLogger(console: false, debug: true));
            }
            else if (Args.Contains("--debug"))
            {
                SetLogger(new SerilogLogger(console: true, debug: true));
            }
            else
            {
                SetLogger(new SerilogLogger(console: true, debug: false));
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Console.CancelKeyPress += Console_CancelKeyPress;
            PrintLogo();
            if (args.Contains("--debug"))
            {
                Info("Debug mode set.");
            }
            ParserResult<object> result = new Parser().ParseArguments<Options, CUIOptions>(args);
            result.WithNotParsed((IEnumerable<Error> errors) =>
            {
                HelpText help = GetAutoBuiltHelpText(result);
                help.Copyright = string.Empty;
                help.AddPreOptionsLine(string.Empty);

                if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
                {
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.HelpVerbRequestedError))
                {
                    HelpVerbRequestedError error = (HelpVerbRequestedError)errors.First(e => e.Tag == ErrorType.HelpVerbRequestedError);
                    if (error.Type != null)
                    {
                        help.AddVerbs(error.Type);
                    }
                    else
                    {
                        help.AddVerbs(typeof(CUIOptions));
                    }
                    Info(help);
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError))
                {
                    HelpRequestedError error = (HelpRequestedError)errors.First(e => e.Tag == ErrorType.HelpRequestedError);
                    help.AddVerbs(typeof(CUIOptions));
                    Info(help);
                    Exit(ExitResult.SUCCESS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.NoVerbSelectedError))
                {
                    help.AddVerbs(typeof(CUIOptions));
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.MissingRequiredOptionError))
                {
                    MissingRequiredOptionError error = (MissingRequiredOptionError)errors.First(e => e.Tag == ErrorType.MissingRequiredOptionError);
                    Error("A required option is missing: {0}.", error.NameInfo.NameText);
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else if (errors.Any(e => e.Tag == ErrorType.UnknownOptionError))
                {
                    UnknownOptionError error = (UnknownOptionError)errors.First(e => e.Tag == ErrorType.UnknownOptionError);
                    help.AddVerbs(typeof(CUIOptions));
                    Error("Unknown option: {error}.", error.Token);
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                else
                {
                    Error("An error occurred parsing the program options: {errors}.", errors);
                    help.AddVerbs(typeof(CUIOptions));
                    Info(help);
                    Exit(ExitResult.INVALID_OPTIONS);
                }
            })
            .WithParsed<CUIOptions>(o =>
            {
                CUI(o).Wait();
                Exit(ExitResult.SUCCESS);
            });

        }
        #endregion

        #region Methods
        static async Task CUI(CUIOptions o)
        {
            EDDIClient c = new EDDIClient(Config("CUI_EDDI_SERVER_URL"), HttpClient);
            if (o.GetBots)
            {
                Info("Querying for bots...");
                try
                {
                    var descriptors = await c.BotstoreBotsDescriptorsGetAsync(null, null, null);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(descriptors));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(descriptors));
                    }
                    else
                    {
                        foreach (var d in descriptors)
                        {
                            
                            WriteInfo("{0} {1} {2} Created: {3} Modified: {4} Version: {5}.", d.ResourceId, d.Name, d.Description, d.CreatedOn, d.LastModifiedOn, d.Resource.Query.Split('=').Last());
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get bot descriptors: {0}.", eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving bot descriptors.");
                    Exit(ExitResult.UNKNOWN_ERROR);
                }
            }
            else if (!string.IsNullOrEmpty(o.GetBot))
            {
                Info("Querying for bot {0}...", o.GetBot);
                try
                {
                    var bot = await c.BotstoreBotsGetAsync(o.GetBot, o.Version);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(bot));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(bot));
                    }
                    else
                    {
                        foreach (var p in bot.Packages)
                        {
                            WriteInfo("Package: {0}, Version: {1}", p.Segments.Last(), p.Query.Split('=').Last());
                        }

                        if (bot.Channels != null)
                        {
                            foreach (var channel in bot.Channels)
                            {
                                WriteInfo("Channel: {0}", channel.Type);
                            }
                        }

                        if (bot.GitBackupSettings != null && bot.GitBackupSettings.RepositoryUrl != null)
                        {
                            WriteInfo("Git repo: {0}", bot.GitBackupSettings.RepositoryUrl.ToString());
                        }

                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get bot {0}: {1}.", o.GetBot, eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving bot {0}.", o.GetBot);
                    Exit(ExitResult.UNKNOWN_ERROR);
                }
            }

            else if (!string.IsNullOrEmpty(o.ExportBot))
            {
                try
                {
                    var r = await c.BackupExportPostAsync(o.ExportBot, 1);
                    WriteInfo("Bot {0} exported to location: {1}.", o.ExportBot, r);
                    Exit(ExitResult.SUCCESS);
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not export bot: {0}: {1}", o.ExportBot, eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error exporting bot {0}.", o.ExportBot);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
            }

            else if (o.GetPackages)
            {
                Info("Querying for packages...");
                try
                {
                    var descriptors = await c.PackagestorePackagesDescriptorsGetAsync(null, null, null);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(descriptors));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(descriptors));
                    }
                    else
                    {
                        foreach (var d in descriptors)
                        {
                            WriteInfo("{0} {1} {2} Created: {3} Modified: {4}.", d.ResourceId, d.Name, d.Description, d.CreatedOn, d.LastModifiedOn);
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not list packages: {0}", eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving packages.");
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
            }

            else if (!string.IsNullOrEmpty(o.GetPackage))
            {
                Info("Querying for package {0}...", o.GetPackage);
                try
                {
                    var package = await c.PackagestorePackagesGetAsync(o.GetPackage, o.Version);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(package));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(package));
                    }
                    else
                    {
                        foreach (var pe in package.PackageExtensions)
                        {
                            WriteInfo("Extension: {0}", pe.Type.ToString());
                            if (pe.Config.Count > 0)
                            {
                                System.Console.Write("  Config: ");
                                foreach (var config in pe.Config)
                                {
                                    System.Console.Write("{0}: {1}", config.Key, config.Value);
                                }
                                WriteInfo("\n");
                            }
                            if (pe.Extensions.Count > 0)
                            {
                                System.Console.Write("  Extensions: ");
                                foreach (var ex in pe.Extensions)
                                {
                                    System.Console.Write("{0}: {1}", ex.Key, ex.Value);
                                }
                                WriteInfo("\n");
                            }
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get package {0}: {1}", o.GetPackage, eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving package: {0}.", o.GetPackage);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
            }
            else if (!string.IsNullOrEmpty(o.GetDescriptor))
            {
                Info("Querying for descriptor {0}...", o.GetDescriptor);
                try
                {
                    var desc = await c.DescriptorstoreDescriptorsGetAsync(o.GetDescriptor, o.Version);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(desc));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(desc));
                    }
                    else
                    {

                        WriteInfo("Resource: {0}\nId: {1}\nName: {2}\nDescription: {3}\nCreated on: {4}\nLast modified on: {5}\n",
                            desc.Resource, desc.ResourceId, desc.Name, desc.Description, desc.CreatedOn, desc.LastModifiedOn);

                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get descriptor {0}: {1}.", o.GetDescriptor, eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving descriptor {0}.", o.GetDescriptor);
                    Exit(ExitResult.UNKNOWN_ERROR);
                }
            }
            else if (o.GetDictionaries)
            {
                Info("Querying for dictionaries...");
                try
                {
                    var descriptors = await c.RegulardictionarystoreRegulardictionariesDescriptorsAsync(null, null, null);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(descriptors));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(descriptors));
                    }
                    else
                    {
                        foreach (var d in descriptors)
                        {
                            WriteInfo("{0} {1} {2} Created: {3} Modified: {4}.", d.ResourceId, d.Name, d.Description, d.CreatedOn, d.LastModifiedOn);
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not list dictionaries: {0}", eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving .");
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }

            }
            else if (!string.IsNullOrEmpty(o.GetDictionary))
            {
                Info("Querying for dictionary {0}...", o.GetDictionary);
                try
                {
                    var dictionary = await c.RegulardictionarystoreRegulardictionariesGetAsync(o.GetDictionary, o.Version, o.Filter, null, null, null);
                    var version = await c.RegulardictionarystoreRegulardictionariesCurrentversionGetAsync(o.GetDictionary);
                    WriteInfo("Version: {0}.", version);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(dictionary));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(dictionary));
                    }
                    else
                    {
                        if (dictionary.Words.Count > 0)
                        {
                            WriteInfo("Words:");
                            foreach (var w in dictionary.Words)
                            {
                                WriteInfo("  Word: {0}", w.Word);
                                WriteInfo("  Frequency: {0}", w.Frequency);
                                WriteInfo("  Expressions: {0}", w.Expressions);
                            }
                        }
                        if (dictionary.Phrases.Count > 0)
                        {
                            WriteInfo("Phrases:");
                            foreach (var p in dictionary.Phrases)
                            {
                                WriteInfo("  Phrase: {0}", p.Phrase);
                                WriteInfo("  Expressions: {0}", p.Expressions);
                            }
                            WriteInfo("");
                        }
                        if (dictionary.RegExs.Count > 0)
                        {
                            WriteInfo("RegExs:");
                            foreach (var r in dictionary.RegExs)
                            {
                                WriteInfo("  RegEx: {0}", r.RegEx);
                                WriteInfo("  Expressions: {0}", r.Expressions);

                            }
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error(eae, "Could not get dictionary {0}.", o.GetDictionary);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving dictionary {0}", o.GetDictionary);
                    Exit(ExitResult.UNKNOWN_ERROR);
                }
            }

            else if (!string.IsNullOrEmpty(o.GetBehavior))
            {
                Info("Querying for behavior set {0}...", o.GetBehavior);
                try
                {
                    var behavior = await c.BehaviorstoreBehaviorsetsGetAsync(o.GetBehavior, o.Version);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(behavior));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(behavior));
                    }
                    else
                    {
                        if (behavior.BehaviorGroups.Count > 0)
                        {
                            WriteInfo("Groups:");
                            foreach (var bg in behavior.BehaviorGroups)
                            {
                                WriteInfo("  Name: {0}", bg.Name);
                                WriteInfo("  Execution Strategy: {0}", bg.ExecutionStrategy);
                                if (bg.BehaviorRules.Count > 0)
                                {
                                    WriteInfo("  Rules:");
                                    foreach (var r in bg.BehaviorRules)
                                    {
                                        WriteInfo("      Phrase: {0}", r.Name);
                                        if (r.Actions.Count > 0)
                                        {
                                            WriteInfo("      Actions: {0}", r.Actions.Aggregate((s1, s2) => s1 + " " + s2));
                                        }
                                        if (r.Conditions.Count > 0)
                                        {
                                            foreach (var condition in r.Conditions)
                                            {
                                                WriteInfo("      Conditions:");
                                                PrintBehaviorRuleCondition(condition, "         ");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get behavior {0} : {1}.", o.GetBehavior, eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
            }

            else if (!string.IsNullOrEmpty(o.GetOutput))
            {
                Info("Querying for output set {0}...", o.GetOutput);
                try
                {
                    var outputSet = await c.OutputstoreOutputsetsGetAsync(o.GetOutput, o.Version, o.Filter, null, null, null);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(outputSet));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(outputSet));
                    }
                    else
                    {
                        if (outputSet.OutputSet != null && outputSet.OutputSet.Count > 0)
                        {
                            foreach (var oc in outputSet.OutputSet)
                            {
                                WriteInfo("Action: {0}", oc.Action);
                                WriteInfo("Times Occurred: {0}", oc.TimesOccurred);
                                if (oc.Outputs.Count > 0)
                                {
                                    WriteInfo("Outputs: ");
                                    foreach (var output in oc.Outputs)
                                    {
                                        WriteInfo("  Type: {0}. Alternatives: {1}", output.Type, output.ValueAlternatives.Select(va => va.ToString()).Aggregate((s1, s2) => s1 + "=" + s2));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get output {0}: {1}", o.GetOutput, eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving output {0}.", o.GetOutput);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
            }
            else if (!string.IsNullOrEmpty(o.GetProperty))
            {
                Info("Querying for property {0}...", o.GetProperty);
                try
                {
                    var prop = await c.PropertysetterstorePropertysettersGetAsync(o.GetProperty, o.Version);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(prop));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(prop));
                    }
                    else
                    {
                        if (prop.SetOnActions != null && prop.SetOnActions.Count > 0)
                        {
                            foreach (var soa in prop.SetOnActions)
                            {
                                WriteInfo("Actions: {0}", soa.Actions.Aggregate((a1, a2) => a1 + " " + a2));
                                if (soa.SetProperties.Count > 0)
                                {
                                    WriteInfo("Set Properties: ");
                                    foreach (var ap in soa.SetProperties)
                                    {
                                        WriteInfo("  " + EDDIClient.Serialize(ap));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get property {0}: {1}", o.GetProperty, eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving property {0}.", o.GetProperty);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }

            }
            else if (!string.IsNullOrEmpty(o.GetHttpCall))
            {
                Info("Querying for HTTP call {0}...", o.GetHttpCall);
                try
                {
                    var call = await c.HttpcallsstoreHttpcallsGetAsync(o.GetHttpCall, o.Version);
                    WriteInfo(EDDIClient.Serialize(call));
                    WriteToFileIfRequired(o, EDDIClient.Serialize(call));
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get HTTP call {0}: {1}", o.GetProperty, eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving HTTP call {0}.", o.GetProperty);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }

            }
            else if (o.CreateDictionary)
            {
                Info("Creating dictionary...");
                await c.RegulardictionarystoreRegulardictionariesPostAsync(ReadFromFileIfRequired<RegularDictionaryConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Created dictionary at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "dictionary." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not create dictionary. HTTP status code {0}.", s);
                }
            }
            else if (o.CreateBehavior)
            {
                Info("Creating behavior...");
                await c.BehaviorstoreBehaviorsetsPostAsync(ReadFromFileIfRequired<BehaviorConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Created behavior set at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "behavior." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not create behavior set. HTTP status code {0}.", s);
                }
            }
            else if (o.CreateOutput)
            {
                Info("Creating output...");
                await c.OutputstoreOutputsetsPostAsync(ReadFromFileIfRequired<OutputConfigurationSet>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Created output configuration set at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "output." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not create output configuration set. HTTP status code {0}.", s);
                }
            }
            else if (o.CreateHttp)
            {
                Info("Creating HTTP call...");
                await c.HttpcallsstoreHttpcallsPostAsync(ReadFromFileIfRequired<HttpCallsConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Created HTTP call at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "output." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not create HTTP call. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdateOutput))
            {
                Info("Updating output set {0}...", o.UpdateOutput);
                await c.OutputstoreOutputsetsPatchAsync(o.UpdateOutput, o.Version,
                    new PatchInstructionOutputConfigurationSet[]
                    {
                    new PatchInstructionOutputConfigurationSet()
                    {
                        Operation = PatchInstructionOutputConfigurationSetOperation.SET,
                        Document = ReadFromFileIfRequired<OutputConfigurationSet>(o)
                    }
                    });
                int s = EDDIClient.LastStatusCode;
                if (s == 200)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Updated output set at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "output." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not update output configuration set. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdateBehavior))
            {
                Info("Updating behavior set {0}...", o.UpdateBehavior);
                await c.BehaviorstoreBehaviorsetsPutAsync(o.UpdateBehavior, o.Version, ReadFromFileIfRequired<BehaviorConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 200)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Updated behavior set at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "behavior." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdateHttpCall))
            {
                Info("Updating HTTP call {0}...", o.UpdateHttpCall);
                await c.HttpcallsstoreHttpcallsPutAsync(o.UpdateHttpCall, o.Version,
                    ReadFromFileIfRequired<HttpCallsConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 200)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Updated HTTP call at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "http." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not update HTTP call. HTTP status code {0}.", s);
                }
            }
            else if (o.CreatePackage)
            {
                Info("Creating package...");
                await c.PackagestorePackagesPostAsync(ReadFromFileIfRequired<PackageConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Created package at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "package." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not create package. HTTP status code {0}.", s);
                }
            }
            else if (o.CreateBot)
            {
                Info("Creating bot...");
                await c.BotstoreBotsPostAsync(ReadFromFileIfRequired<BotConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Created bot at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "bot." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not create package. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdateDictionary))
            {
                Info("Updating dictionary {0}...", o.UpdateDictionary);
                await c.RegulardictionarystoreRegulardictionariesPatchAsync(o.UpdateDictionary, o.Version,
                    new PatchInstructionRegularDictionaryConfiguration[]
                    {
                        new PatchInstructionRegularDictionaryConfiguration()
                        {
                            Operation = PatchInstructionRegularDictionaryConfigurationOperation.SET,
                            Document = ReadFromFileIfRequired<RegularDictionaryConfiguration>(o)
                        }
                    });
                int s = EDDIClient.LastStatusCode;
                if (s == 200)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Updated dictionary at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "dictionary." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not update dictionary. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdatePackage))
            {
                Info("Updating package {0}...", o.UpdatePackage);
                await c.PackagestorePackagesPutAsync(o.UpdatePackage, o.Version, ReadFromFileIfRequired<PackageConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 200)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Updated package at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "package." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdateDescriptor))
            {
                Info("Updating descriptor {0}...", o.UpdateDescriptor);
                await c.DescriptorstoreDescriptorsPatchAsync(o.UpdateDescriptor, o.Version,
                    new PatchInstructionDocumentDescriptor()
                    {
                        Operation = PatchInstructionDocumentDescriptorOperation.SET,
                        Document = ReadFromFileIfRequired<DocumentDescriptor>(o)
                    });
                int s = EDDIClient.LastStatusCode;
                if (s == 204)
                {
                    Info("Updated descriptor {0}.", o.UpdateDescriptor);
                    
                }
                else
                {
                    Error("Did not update descriptor. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.UpdateBot))
            {
                Info("Updating bot {0}...", o.UpdateBot);
                await c.BotstoreBotsPutAsync(o.UpdateBot, o.Version, ReadFromFileIfRequired<BotConfiguration>(o));
                int s = EDDIClient.LastStatusCode;
                if (s == 200)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Updated bot at {0}.", l);
                    if (!string.IsNullOrEmpty(o.File))
                    {
                        var u = new Uri(l);
                        var id = u.Segments.Last();
                        var v = u.Query.Split('=').Last();
                        var f = new FileInfo(o.File);
                        var name = Path.Combine(f.Directory.FullName, "bot." + id + "." + v + ".json");
                        File.Move(f.FullName, name);
                        Info("Renamed {0} to {1}.", f.FullName, name);
                    }
                }
                else
                {
                    Error("Did not update bot. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.DeployBot))
            {
                Info("Deploying bot {0} to test environment...", o.DeployBot);
                await c.AdministrationDeployAsync(EDDIEnvironment.Test, o.DeployBot, o.Version, true);
                int s = EDDIClient.LastStatusCode;
                if (s == 202)
                {
                    var url = Config("CUI_EDDI_SERVER_URL") +
                        "/administration/test/deploymentstatus/" + o.DeployBot + "?version=" + o.Version.ToString();
                    Info("Deployed bot. Deployment status at {0}", url);
                    Info("Waiting for 5 seconds while bot deploys.");
                    Thread.Sleep(5 * 1000);
                    var response = HttpClient.GetAsync(url).Result;
                    Info("Deployment status: {0}.", response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    Error("Did not update bot. HTTP status code {0}.", s);
                }
            }
            else if (!string.IsNullOrEmpty(o.StartConversation))
            {
                Info("Starting conversation with bot {0}...", o.StartConversation);
                await c.BotsPostAsync(Environment8.Unrestricted, o.StartConversation, "", null);
                int s = EDDIClient.LastStatusCode;
                if (s == 201)
                {
                    string l = EDDIClient.GetLastResponseHeader("Location").First();
                    Info("Started conversation at {0}.", l);
                }
                else
                {
                    Error("Did not start conversation. HTTP status code {0}.", s);
                }
            }

            else if (!string.IsNullOrEmpty(o.GetConversation))
            {
                try
                {
                    Info("Getting conversation {0}...", o.GetConversation);
                    var convo = await c.ConversationstoreConversationsSimpleAsync(o.GetConversation, false, true, null);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(convo));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(convo));
                    }
                    else
                    {
                        WriteInfo("Bot Id: {0}", convo.BotId);
                        WriteInfo("Bot Version: {0}", convo.BotVersion);
                        WriteInfo("Bot Deployment Environment: {0}", convo.Environment.ToString());
                        WriteInfo("User Id: {0}", convo.UserId);
                        WriteInfo("Converstation State: {0}", convo.ConversationState.ToString());
                        for (int i = 0; i < convo.ConversationOutputs.Count; i++)
                        {
                            WriteInfo("\nStep: {0}", i);
                            var output = convo.ConversationOutputs.ElementAt(i);
                            if (output.ContainsKey("input"))
                            {
                                WriteInfo("Input: {0}", output["input"]);
                            }
                            if (output.ContainsKey("expressions"))
                            {
                                WriteInfo("Expressions: {0}", output["expressions"]);
                            }
                            if (output.ContainsKey("expressions"))
                            {
                                WriteInfo("Intents: {0}", EDDIClient.Serialize(output["expressions"]));
                            }
                            if (output.ContainsKey("actions"))
                            {
                                WriteInfo("Actions: {0}", EDDIClient.Serialize(output["actions"]));
                            }
                            if (output.ContainsKey("httpCalls"))
                            {
                                WriteInfo("HTTP Calls: {0}", EDDIClient.Serialize(output["httpCalls"]));
                            }
                            if (output.ContainsKey("properties"))
                            {
                                WriteInfo("Properties: {0}", EDDIClient.Serialize(output["properties"]));
                            }
                            if (output.ContainsKey("output"))
                            {
                                WriteInfo("Output: {0}", output["output"]);
                            }
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get conversation: {0}: {1}", o.GetConversation, eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error getting conversation {0}.", o.GetConversation);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
            }
            else if (!string.IsNullOrEmpty(o.Talk))
            {
                if (string.IsNullOrEmpty(o.Input))
                {
                    Error("You must specify an input to the conversation.");
                    Exit(ExitResult.INVALID_OPTIONS);
                }
                try
                {
                    string[] ids = o.Talk.Split(':');
                    string botid = ids[0], cid = ids[1];
                    Info("Sending input to conversation {0} with bot {1}...", cid, botid);
                    await c.BotsPostAsync(Environment7.Unrestricted, botid, cid, false, true, null, body: new InputData() { Input = o.Input });
                    var s = EDDIClient.LastStatusCode;
                    if (s == 200)
                    {
                        Info("Input sent to {0}.", o.Talk);
                    }
                    else
                    {
                        Error("Did not talk to {0}. HTTP status code {0}.", s);
                    }

                }
                catch (EDDIApiException eae)
                {
                    Error("Could not get conversation: {0}: {1}", o.GetConversation, eae.Message);
                    Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error getting conversation {0}.", o.GetConversation);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
            }
            else if (!string.IsNullOrEmpty(o.GetConversations))
            {
                Info("Querying for conversations...");
                try
                {
                    var convos = await c.ConversationstoreConversationsActiveAsync(o.GetConversations, o.Version, 0, 100);
                    if (o.Json)
                    {
                        WriteInfo(EDDIClient.Serialize(convos));
                        WriteToFileIfRequired(o, EDDIClient.Serialize(convos));
                    }
                    else
                    {
                        foreach (var convo in convos)
                        {
                            WriteInfo("Id: {0}, Last interaction: {1},  State: {2}.", convo.ConversationId, convo.LastInteraction.Value, convo.ConversationState.ToString());
                        }
                    }
                }
                catch (EDDIApiException eae)
                {
                    Error("Could not list conversations: {0}", eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error retrieving conversations.");
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }

            }
            else if (!string.IsNullOrEmpty(o.DeleteConversation))
            {
                try
                {
                    Info("Delete conversation {0}...", o.DeleteConversation);
                    await c.ConversationstoreConversationsDeleteAsync(o.DeleteConversation, true);
                    var code = EDDIClient.LastStatusCode;
                    if (code == 200 || code == 204)
                    {
                        Info("Successfully deleted conversation.");
                    }
                    else
                    {
                        Error("Did not successfully delete conversation {0}. Status code: {1}.", o.DeleteConversation, code);
                    }

                }
                catch (EDDIApiException eae)
                {
                    Error("Could not delete conversation: {0}", eae.Message);
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }
                catch (Exception e)
                {
                    Error(e, "Unknown error deleting conversation.");
                    Exit(ExitResult.UNHANDLED_EXCEPTION);
                }

            }
            else
            {
                Error("Select the CUI operation and options you want to use.");
                HelpText help = new HelpText();
                help.Copyright = string.Empty;
                help.AddPreOptionsLine(string.Empty);
                help.AddVerbs(typeof(CUIOptions));
                help.AddOptions(new Parser().ParseArguments<CUIOptions>(Args));
                Info(help);
            }
        }

        static void PrintBehaviorRuleCondition(BehaviorRuleConditionConfiguration condition, string indent)
        {
            WriteInfo(indent + "Type: {0}", condition.Type);
            if (condition.Configs.Count > 0)
            {
                WriteInfo(indent + "Config: " + condition.Configs.Select(cfg => cfg.Key + ":" + cfg.Value).Aggregate((cfg1, cfg2) => cfg1 + " " + cfg2));
            }
            if (condition.Conditions != null && condition.Conditions.Count > 0)
            {
                foreach(var c in condition.Conditions)
                {
                    PrintBehaviorRuleCondition(c, indent + "   ");
                }
            }
        }

        static void WriteToFileIfRequired(CUIOptions o, string s)
        {
            if (!string.IsNullOrEmpty(o.File))
            {
                if (File.Exists(o.File) && !o.Overwrite)
                {
                    Error("The file {0} already exists.", o.File);
                }
                else
                {
                    File.WriteAllText(o.File, s);
                    Info("Wrote to {0}.", o.File);
                }
            }
        }

        static T ReadFromFileIfRequired<T>(CUIOptions o)
        {
            if (!string.IsNullOrEmpty(o.File))
            {
                Info("Using {0} as input file.", o.File);
                return EDDIClient.Deserialize<T>(File.ReadAllText(o.File)); 
            }
            else if (string.IsNullOrEmpty(o.Input))
            {
                Error("No file or input specified.");
                Exit(ExitResult.NOT_FOUND_OR_SERVER_ERROR);
            }
            {
                return EDDIClient.Deserialize<T>(o.Input);
            }
        }
            
        static void PrintLogo()
        {
            CO.WriteLine(FiggleFonts.Chunky.Render("SMApp"), Color.Blue);
            CO.WriteLine("v{0}", AssemblyVersion.ToString(3), Color.Blue);
        }

        public static void Exit(ExitResult result)
        {

            if (Cts != null && !Cts.Token.CanBeCanceled)
            {
                Cts.Cancel();
                Cts.Dispose();
            }

            Environment.Exit((int)result);
        }

        static HelpText GetAutoBuiltHelpText(ParserResult<object> result)
        {
            return HelpText.AutoBuild(result, h =>
            {
                h.AddOptions(result);
                return h;
            },
            e =>
            {
                return e;
            });
        }

        static void WriteInfo(string template, params object[] args) => CO.WriteLineFormatted(template, Color.AliceBlue, Color.PaleGoldenrod, args);
        #endregion

        #region Properties
        static string [] Args { get; set; }
        #endregion

        #region Event Handlers
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

            Error((Exception)e.ExceptionObject, "Unhandled error occurred during operation. SMApp CLI will now shutdown.");
            Exit(ExitResult.UNHANDLED_EXCEPTION);
        }
        
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Info("Ctrl-C pressed. Exiting.");
            Cts.Cancel();
            Exit(ExitResult.SUCCESS);
        }
        #endregion

        #region Fields
        #endregion
    }
}
