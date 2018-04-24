﻿using Castle.DynamicProxy;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Zametek.Utility.Logging
{
    public class LogProxy
    {
        private static readonly IProxyGenerator s_ProxyGenerator = new ProxyGenerator();

        public static I Create<I>(
            I instance,
            ILogger logger,
            LogType logType = LogType.All) where I : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Debug.Assert(typeof(I).IsInterface);

            bool useAll = logType == LogType.All;
            var interceptors = new List<IInterceptor>();

            if (useAll || logType.HasFlag(LogType.Tracking))
            {
                interceptors.Add(new AsyncTrackingInterceptor().ToInterceptor());
            }

            if (useAll || logType.HasFlag(LogType.Error))
            {
                interceptors.Add(new AsyncErrorLoggingInterceptor(logger).ToInterceptor());
            }

            if (useAll || logType.HasFlag(LogType.Performance))
            {
                interceptors.Add(new AsyncPerformanceLoggingInterceptor(logger).ToInterceptor());
            }

            if (useAll || logType.HasFlag(LogType.Diagnostic))
            {
                // Check for NoDiagnosticLogging Class scope.
                bool classHasNoDiagnosticAttribute = instance.GetType().GetCustomAttributes(typeof(NoDiagnosticLoggingAttribute), false).Any();

                if (!classHasNoDiagnosticAttribute)
                {
                    interceptors.Add(new AsyncDiagnosticLoggingInterceptor(logger).ToInterceptor());
                }
            }

            return s_ProxyGenerator.CreateInterfaceProxyWithTargetInterface(instance, interceptors.ToArray());
        }
    }
}