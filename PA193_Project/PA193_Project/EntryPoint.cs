﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PA193_Project.CommandLine;
using PA193_Project.Entities;
using PA193_Project.Modules;
using PA193_Project.Services;

namespace PA193_Project
{
    class EntryPoint
    {
        private readonly IParserService _parserService;
        private readonly ILogger<EntryPoint> _logger;

        public EntryPoint(IParserService parserService, ILogger<EntryPoint> logger)
        {
            this._parserService = parserService;
            this._logger = logger;
        }

        public void Run(String[] args)
        {
            // Set up global exception handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrap;

            // Set up CLI options
            CommandLineOptions commandLineOptions = new CommandLineOptions();
            commandLineOptions.AddOption(new CommandLineOption("h", CommandLineOptionType.Switch, "Print this help"));
            commandLineOptions.AddOption(new CommandLineOption("v", CommandLineOptionType.Switch, "Print version info"));
            commandLineOptions.AddOption(new CommandLineOption("output", CommandLineOptionType.Option, "Output JSON file, \"<textfile>.json\" by default"));
            commandLineOptions.AddOption(new CommandLineOption("arg", CommandLineOptionType.Argument, "A text file of the certificate generated by pdftotext"));

            try
            {
                ParsedOptions parseResults = commandLineOptions.Parse(args);

                if (parseResults.IsEmpty()) { Console.WriteLine(commandLineOptions.GetHelp()); Environment.Exit(0); }

                string filepath = parseResults.Get<List<string>>("arg")[0]; // TODO process multiple
                string output = parseResults.Get<string>("output");
                bool help = parseResults.Get<bool>("h");
                bool version = parseResults.Get<bool>("v");

                if (help) { Console.WriteLine(commandLineOptions.GetHelp()); Environment.Exit(0); }
                if (version) { Console.WriteLine("0.1"); Environment.Exit(0); }


                _logger.LogDebug($"Filepath: {filepath}");
                _logger.LogDebug($"Output: {output}");


                _parserService.RegisterModule(new HeaderFooterModule());
                _parserService.RegisterModule(new TitleModule());
                _parserService.RegisterModule(new VersionsModule());
                var results = _parserService.Parse(document);


                _logger.LogDebug($"{filepath} -[ all ]-> {output}");
                Document document = new Document
                {
                    Filepath = filepath
                };

                _logger.LogDebug($"Generated {document.Indices.Count} indices");

                _parserService.RegisterModule(new TitleModule()); ;
                var results = _parserService.Parse(document);

                JsonSerializerOptions serializerOptions = new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                };
                //Console.WriteLine(results.Title);
                Console.WriteLine(JsonSerializer.Serialize(results, serializerOptions));

            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(commandLineOptions.GetHelp());
            }
        }

        private void UnhandledExceptionTrap(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            _logger.LogError(unhandledExceptionEventArgs.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}
