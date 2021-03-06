﻿// <copyright file="Program.cs" company="Techyian">
// Copyright (c) Ian Auty. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MMALSharp.Common.Utility;
using Newtonsoft.Json;
using NLog.Extensions.Logging;

namespace Bobcat.Client
{
    class Program
    {
        private static CancellationTokenSource _applicationTokenSource;
        private static PiCamService _service;

        public static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .ClearProviders()
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddNLog("NLog.config");
            });

            var configText = File.ReadAllText($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/appsettings.json");
            var config = JsonConvert.DeserializeObject<ClientConfiguration>(configText);
            
            if (string.IsNullOrEmpty(config.RelayServerHostname) || string.IsNullOrEmpty(config.UniqueId))
            {
                throw new NullReferenceException("Could not parse configuration from appsettings.json");
            }

            MMALLog.LoggerFactory = loggerFactory;

            _applicationTokenSource = new CancellationTokenSource();
            
            _service = new PiCamService(loggerFactory.CreateLogger<PiCamService>(), _applicationTokenSource, config);

            Console.CancelKeyPress += Console_OnCancelKeyPress;

            _service.ConfigureCaptureHandler();
            _service.InitialiseCamera();
            _service.InitialiseClient();

            await _applicationTokenSource.Token.AsTask();
        }

        private static void Console_OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _applicationTokenSource?.Cancel();
            _service.Dispose();
        }
    }
}
