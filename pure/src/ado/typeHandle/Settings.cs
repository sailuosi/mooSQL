using System;
using System.Data;
using System.Threading;

namespace mooSQL.data
{

    /// <summary>
    /// Permits specifying certain SqlMapper values globally.
    /// </summary>
    public static class Settings
    {
        // disable single result by default; prevents errors AFTER the select being detected properly
        private const CommandBehavior DefaultAllowedCommandBehaviors = ~CommandBehavior.SingleResult;
        internal static CommandBehavior AllowedCommandBehaviors { get; private set; } = DefaultAllowedCommandBehaviors;



        /// <summary>
        /// Indicates whether nulls in data are silently ignored (default) vs actively applied and assigned to members
        /// </summary>
        public static bool ApplyNullValues { get; set; }



        /// <summary>
        /// If set, pseudo-positional parameters (i.e. ?foo?) are passed using auto-generated incremental names, i.e. "1", "2", "3"
        /// instead of the original name; for most scenarios, this is ignored since the name is redundant, but "snowflake" requires this.
        /// </summary>
        public static bool UseIncrementalPseudoPositionalParameterNames { get; set; }


    }
    
}
