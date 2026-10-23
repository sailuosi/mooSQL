


namespace mooSQL.data.taos
{
    /// <summary>
    ///     Represents the type affinities used by columns in Taos tables.
    /// </summary>
  
    public enum TaosType
    {
        /// <summary>
        ///     A signed integer.
        /// </summary>
        Integer , 

        /// <summary>
        ///     A floating point value.
        /// </summary>
        Real  ,

        /// <summary>
        ///     A text string.
        /// </summary>
        Text ,

        /// <summary>
        ///     A blob of data.
        /// </summary>
        Blob 
    }
}
