﻿using System;
using Serilog;
using Serilog.Debugging;
using Serilog.Templates;

namespace Sample
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        public static void Main()
        {
            SelfLog.Enable(Console.Error);

            TextFormattingExample();
            JsonFormattingExample();
            PipelineComponentExample();
        }

        static void TextFormattingExample()
        {
            using var log = new LoggerConfiguration()
                .WriteTo.Console(new ExpressionTemplate(
                    "[{@t:HH:mm:ss} " +
                    "{@l:u3} " +
                    "({coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<no source>')})] " +
                    "{@m} " +
                    "(first item is {coalesce(Items[0], '<empty>')})" +
                    "\n" +
                    "{@x}"))
                .CreateLogger();

            log.Information("Running {Example}", nameof(TextFormattingExample));
            
            log.ForContext<Program>()
                .Information("Cart contains {@Items}", new[] { "Tea", "Coffee" });
            
            log.ForContext<Program>()
                .Information("Cart contains {@Items}", new[] { "Apricots" });
        }

        static void JsonFormattingExample()
        {
            using var log = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "Example")
                .WriteTo.Console(new ExpressionTemplate(
                    "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, ..@p} }\n"))
                .CreateLogger();

            log.Information("Running {Example}", nameof(JsonFormattingExample));
            
            log.ForContext<Program>()
                .Information("Cart contains {@Items}", new[] { "Tea", "Coffee" });
            
            log.ForContext<Program>()
                .Warning("Cart is empty");
        }

        static void PipelineComponentExample()
        {
            using var log = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "Example")
                .Enrich.WithComputed("FirstItem", "coalesce(Items[0], '<empty>')")
                .Enrich.WithComputed("SourceContext", "coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<no source>')")
                .Filter.ByIncludingOnly("Items is null or Items[?] like 'C%'")
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3} ({SourceContext})] {Message:lj} (first item is {FirstItem}){NewLine}{Exception}")
                .CreateLogger();
            
            log.Information("Running {Example}", nameof(PipelineComponentExample));
            
            log.ForContext<Program>()
                .Information("Cart contains {@Items}", new[] { "Tea", "Coffee" });
            
            log.ForContext<Program>()
                .Information("Cart contains {@Items}", new[] { "Apricots" });
        }
    }
}
