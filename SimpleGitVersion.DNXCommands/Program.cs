﻿using Microsoft.Dnx.Runtime.Common.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleGitVersion.DNXCommands
{
    class Program
    {
        static int Main( string[] args )
        {
            try
            {
                var app = new CommandLineApplication();
                app.Name = "sgv";
                app.Description = "SimpleGitVersion commands.";
                app.HelpOption( "-?|-h|--help" );
                app.VersionOption( "--version", GetVersion, GetInformationalVersion );
                var optVerbose = app.Option( "-v|--verbose", "Verbose output", CommandOptionType.NoValue );

                app.Command( "info", c =>
                {
                    c.Description = "Display SimpleGitVersion information from Git repository.";
                    var optionProject = c.Option( "-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue );
                    c.HelpOption( "-?|-h|--help" );

                    c.OnExecute( () =>
                    {
                        var ctx = new CommandContext( optionProject.Value() ?? Directory.GetCurrentDirectory(), optVerbose.HasValue() );
                        if( ctx.SolutionDir != null )
                        {
                            ctx.Logger.Verbose = true;
                            SimpleRepositoryInfo info = SimpleRepositoryInfo.LoadFromPath( ctx.Logger, ctx.SolutionDir, ( log, hasRepoXml, options ) =>
                            {
                                options.IgnoreDirtyWorkingFolder = true;
                            } );
                        }
                        return 0;
                    } );
                } );

                app.Command( "prebuild", c =>
                {
                    c.Description = "Updates Properties/SGVVersionInfo.cs files from Git (ignoring their own changes).";
                    var optionProject = c.Option( "-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue );
                    c.HelpOption( "-?|-h|--help" );

                    c.OnExecute( () =>
                    {
                        var ctx = new CommandContext( optionProject.Value() ?? Directory.GetCurrentDirectory(), optVerbose.HasValue() );
                        if( ctx.SolutionDir != null )
                        {
                            ctx.UpdateProjectFiles();
                            string f = ctx.ProjectSGVVersionInfoFile;
                            if( f == null )
                            {
                                ctx.Logger.Warn( "File SGVVersionInfo.cs not found. Creating it." );
                                f = ctx.TheoreticalProjectSGVVersionInfoFile;
                            }
                            string text = ctx.RepositoryInfo.BuildAssemblyVersionAttributesFile( "'sgv prebuild'" );
                            File.WriteAllText( f, text );
                        }
                        return 0;
                    } );
                } );

                app.Command( "prepack", c =>
                {
                    c.Description = "Updates version property in project.json files from Git (ignoring the version property itself).";
                    var optionProject = c.Option( "-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue );
                    c.HelpOption( "-?|-h|--help" );

                    c.OnExecute( () =>
                    {
                        var ctx = new CommandContext( optionProject.Value() ?? Directory.GetCurrentDirectory(), optVerbose.HasValue() );
                        if( ctx.SolutionDir != null )
                        {
                            ctx.UpdateProjectFiles();
                        }
                        return 0;
                    } );
                } );

                app.Command( "restore", c =>
                {
                    c.Description = "Restores project.json files that differ only by version property for this solution.";
                    var optionProject = c.Option( "-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue );
                    c.HelpOption( "-?|-h|--help" );

                    c.OnExecute( () =>
                    {
                        var ctx = new CommandContext( optionProject.Value() ?? Directory.GetCurrentDirectory(), optVerbose.HasValue() );
                        if( ctx.SolutionDir != null )
                        {
                            ctx.RestoreProjectFilesThatDifferOnlyByVersion();
                        }
                        return 0;
                    } );
                } );

                // Show help information if no subcommand/option was specified.
                app.OnExecute( () =>
                {
                    app.ShowHelp();
                    return 2;
                } );

                return app.Execute( args );
            }
            catch( Exception exception )
            {
                Console.WriteLine( "Error: {0}", exception.Message );
                return 1;
            }
        }

        static string GetInformationalVersion()
        {
            StringBuilder b = new StringBuilder();
            b.Append( "AssemblyVersion: " ).Append( GetVersion() ).AppendLine();
            var attributes = typeof( Program ).Assembly.GetCustomAttributes().ToArray();
            string info = attributes.OfType<AssemblyInformationalVersionAttribute>().Single().InformationalVersion;
            b.Append( "InformationalVersion: " ).Append( info ).AppendLine();
            string sgvInfo = attributes.Where( a => a.GetType().Name == "SimpleGitVersionInfoAttribute" ).Single().ToString();
            b.Append( "SimpleGitVersion: " ).Append( sgvInfo ).AppendLine();
            return b.ToString();
        }

        private static string GetVersion()
        {
            return typeof( Program ).Assembly.GetName().Version.ToString( 2 );
        }

        /// <summary>
        /// Finds a named directory above or next to the specified <paramref name="start"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="start">Starting directory.</param>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>Null if not found, otherwise the path of the directory.</returns>
        static string FindDirectoryFrom( string start, string directoryName )
        {
            if( start == null ) throw new ArgumentNullException( "start" );
            if( directoryName == null ) throw new ArgumentNullException( "directortyName" );
            string p = start;
            string pF;
            while( !Directory.Exists( pF = Path.Combine( p, directoryName ) ) )
            {
                p = Path.GetDirectoryName( p );
                if( String.IsNullOrEmpty( p ) ) return null;
            }
            return pF;
        }
    }
}
