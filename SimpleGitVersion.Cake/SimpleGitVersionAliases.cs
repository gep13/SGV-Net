﻿using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using System;

namespace SimpleGitVersion
{
    /// <summary>
    /// Contains functionality related to SimpleGitVersion.
    /// </summary>
    [CakeAliasCategory( "SimpleGitVersion" )]
    public static class SimpleGitVersionAliases
    {
        class Logger : ILogger
        {
            readonly ICakeContext _ctx;

            public Logger( ICakeContext ctx )
            {
                _ctx = ctx;
            }

            public void Error( string msg )
            {
                _ctx.Log.Error( Verbosity.Quiet, msg );
            }

            public void Warn( string msg )
            {
                _ctx.Log.Warning( Verbosity.Quiet, msg );
            }

            public void Info( string msg )
            {
                _ctx.Log.Information( Verbosity.Quiet, msg );
            }

            public void Trace( string msg )
            {
                _ctx.Log.Verbose( Verbosity.Quiet, msg );
            }
        }

        /// <summary>
        /// Gets a <see cref="RepositoryInfo"/> immutable object computed from the current head of the Git repository.
        /// Use <see cref="GetSimpleRepositoryInfo"/> to obtain a simpler object.
        /// Do NOT use this in a solution with DNX projects since this does not handle differences between project.json without 
        /// taking versions properties inot account. Use the <see cref="DNXSolution.RepositoryInfo"/> property instead.
        /// </summary>
        /// <param name="context">The Cake context.</param>
        /// <param name="options">Optional options.</param>
        /// <returns>A RepositoryInformation object.</returns>
        [CakeMethodAlias]
        public static RepositoryInfo GetRepositoryInfo( this ICakeContext context, RepositoryInfoOptions options = null )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            return RepositoryInfo.LoadFromPath( context.Environment.WorkingDirectory.FullPath, options );
        }

        /// <summary>
        /// Gets a <see cref="SimpleRepositoryInfo"/> immutable object computed from the current head of the Git repository.
        /// Do NOT use this in a solution with DNX projects since this does not handle differences between project.json without 
        /// taking versions properties inot account. Use the <see cref="DNXSolution.RepositoryInfo"/> property instead.
        /// </summary>
        /// <param name="context">The Cake context.</param>
        /// <returns>A SimpleRepositoryInfo object.</returns>
        [CakeMethodAlias]
        public static SimpleRepositoryInfo GetSimpleRepositoryInfo( this ICakeContext context )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            return SimpleRepositoryInfo.LoadFromPath( new Logger( context ), context.Environment.WorkingDirectory.FullPath, ( log, hasOptionFile, options ) =>
            {
                if( !hasOptionFile ) log.Info( "Using default options to read repository information." );
                else log.Info( "Using RepositoryInfo.xml: " + options.ToXml().ToString() );
            } );
        }

        /// <summary>
        /// Gets a <see cref="DNXSolution"/> for the current solution.
        /// </summary>
        /// <param name="context">The Cake context.</param>
        /// <param name="projectFilter">Optionally filters returned projects.</param>
        /// <returns>A DNXSolution object.</returns>
        [CakeMethodAlias]
        public static DNXSolution GetDNXSolution( this ICakeContext context, Func<DNXProjectFile, bool> projectFilter = null )
        {
            return new DNXSolution( context.Environment.WorkingDirectory.FullPath, new Logger( context ), projectFilter );
        }
    }

}
