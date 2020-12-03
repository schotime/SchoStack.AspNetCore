using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StructureMap;
using StructureMap.AspNetCore;

namespace SchoStack.AspNetCore.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IHost BuildWebHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new StructureMapServiceProviderFactory(new Registry()))
                .ConfigureWebHostDefaults(x =>
                {
                    x.UseStartup<Startup>();
                })
                .Build();
    }
}
