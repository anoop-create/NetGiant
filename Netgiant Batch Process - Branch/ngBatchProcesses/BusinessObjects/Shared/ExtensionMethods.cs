namespace ngBatchProcesses.BusinessObjects.Shared
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Default value is decimal is null value
        /// </summary>
        public static decimal IsNull(this decimal? value, decimal defaultValue)
        {
            decimal returnValue;

            if (value == null)
            {
                returnValue = defaultValue;
            }
            else
            {
                returnValue = value ?? 0;
            }

            return returnValue;
        }

        /// <summary>
        /// Default value is int is null value
        /// </summary>
        public static int IsNull(this int? value, int defaultValue)
        {
            var returnValue = 0;

            if (value == null)
            {
                returnValue = defaultValue;
            }
            else
            {
                returnValue = value ?? 0;
            }

            return returnValue;
        }
    }
}
