namespace rabbitmq_trace_dump
{
    /// <summary>
    /// Defines the comparison operators available for search filtering.
    /// </summary>
    internal enum SearchOperator
    {
        /// <summary>No search filter active.</summary>
        None = 0,

        /// <summary>Case-insensitive equals (= or ==).</summary>
        Equals = 1,

        /// <summary>Case-insensitive contains (~==).</summary>
        Contains = 2,

        /// <summary>Case-insensitive not equals (!= or &lt;&gt;).</summary>
        NotEquals = 3,

        /// <summary>Case-insensitive starts with (^=).</summary>
        StartsWith = 4,

        /// <summary>Case-insensitive ends with ($=).</summary>
        EndsWith = 5,

        /// <summary>Regular expression match (~=).</summary>
        Regex = 6
    }
}