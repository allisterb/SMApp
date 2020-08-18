using System;
using System.Collections.Generic;
using System.Text;

using CommandLine;
using CommandLine.Text;

namespace SMApp
{
    public class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Enable debug mode.")]
        public bool Debug { get; set; }
    }

    [Verb("cui", HelpText = "Administer the CUI features of SMApp (admin only.)")]
    public class CUIOptions : Options
    {
        [Option('v', "version", Required = false, Default = 1, HelpText = "Set the version of the object to be retrieved.")]
        public int Version { get; set; }

        [Option("filter", Required = false, Default = null, HelpText = "Filter the current query on this value.")]
        public string Filter { get; set; }

        [Option('i', "input", Required = false, Default = null, HelpText = "Input for the current operation.")]
        public string Input { get; set; }

        [Option('f', "file", Required = false, Default = null, HelpText = "File to use for input or output for the current operation.")]
        public string File { get; set; }

        [Option('w', "overwrite", Required = false, Default = false, HelpText = "Overwrite file to use for input or output for the current operation.")]
        public bool Overwrite { get; set; }

        [Option('j', "json", Required = false, Default = false, HelpText = "Print the JSON recieved for the current operation.")]
        public bool Json { get; set; }

        [Option("get-bots", Required = false, HelpText = "List the current chatbots on the EDDI server.")]
        public bool GetBots { get; set; }

        [Option("get-bot", Required = false, HelpText = "Get the details of the bot with the speicified id.")]
        public string GetBot { get; set; }

        [Option("export-bot", Required = false, HelpText = "Export a chatbot on the EDDI server.")]
        public string ExportBot { get; set; }

        [Option("get-packages", Required = false, HelpText = "List the current packages on the EDDI server.")]
        public bool GetPackages { get; set; }

        [Option("get-package", Required = false, HelpText = "Get the package with the specified id.")]
        public string GetPackage { get; set; }

        [Option("get-dictionaries", Required = false, HelpText = "Get all dictionaries on the EDDI server.")]
        public bool GetDictionaries { get; set; }

        [Option("get-dictionary", Required = false, HelpText = "Get the dictionary with the specified id.")]
        public string GetDictionary { get; set; }

        [Option("get-behavior", Required = false, HelpText = "Get the behavior set with the specified id.")]
        public string GetBehavior { get; set; }

        [Option("get-output", Required = false, HelpText = "Get the output with the specified id.")]
        public string GetOutput { get; set; }

        [Option("get-property", Required = false, HelpText = "Get the property with the specified id.")]
        public string GetProperty { get; set; }

        [Option("get-http-call", Required = false, HelpText = "Get the HTTP call with the specified id.")]
        public string GetHttpCall { get; set; }

        [Option("get-convo", Required = false, HelpText = "Get the conversation with the specified id.")]
        public string GetConversation { get; set; }

        [Option("get-convos", Required = false, HelpText = "List conversations for the specified bot id.")]
        public string GetConversations { get; set; }

        [Option("create-dictionary", Required = false, HelpText = "Create a dictionary from the specified input.")]
        public bool CreateDictionary { get; set; }

        [Option("create-behavior", Required = false, HelpText = "Create a behavior set from the specified input.")]
        public bool CreateBehavior { get; set; }

        [Option("create-output", Required = false, HelpText = "Create an output set from the specified input.")]
        public bool CreateOutput { get; set; }

        [Option("create-http-call", Required = false, HelpText = "Create an HTTP call from the specified input.")]
        public bool CreateHttp { get; set; }

        [Option("update-output", Required = false, HelpText = "Update an output set with the specified id from the specified input.")]
        public string UpdateOutput { get; set; }

        [Option("update-behavior", Required = false, HelpText = "Update a behavior set with the specified id from the specified input.")]
        public string UpdateBehavior { get; set; }

        [Option("create-package", Required = false, HelpText = "Create a package from the specified input.")]
        public bool CreatePackage { get; set; }

        [Option("update-package", Required = false, HelpText = "Update a package with the specified id from the specified input.")]
        public string UpdatePackage { get; set; }

        [Option("update-http-call", Required = false, HelpText = "Update a HTTP call with the specified id from the specified input.")]
        public string UpdateHttpCall { get; set; }

        [Option("create-bot", Required = false, HelpText = "Create a bot from the specified input.")]
        public bool CreateBot { get; set; }

        [Option("update-bot", Required = false, HelpText = "Update a bot with the specified id from the specified input.")]
        public string UpdateBot { get; set; }

        [Option("get-descriptor", Required = false, HelpText = "Get the descriptor of the bot with the specified id.")]
        public string GetDescriptor { get; set; }

        [Option("update-descriptor", Required = false, HelpText = "Update a descriptor with the specified id from the specified input.")]
        public string UpdateDescriptor { get; set; }

        [Option("deploy-bot", Required = false, HelpText = "Deploy a bot with the specified id to the unrestricted environment.")]
        public string DeployBot { get; set; }

        [Option("update-dictionary", Required = false, HelpText = "Update a dictionary with the specified id from the specified input.")]
        public string UpdateDictionary { get; set; }

        [Option("start-convo", Required = false, HelpText = "Start a conversation with a bot.")]
        public string StartConversation { get; set; }

        [Option('t', "talk", Required = false, HelpText = "Respond to the conversation with the specified bot id:convo id .")]
        public string Talk { get; set; }

        [Option("delete-convo", Required = false, HelpText = "Delete a conversation with the specified if.")]
        public string DeleteConversation { get; set; }

    }
}
