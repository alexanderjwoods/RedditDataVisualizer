namespace SDV.Data.Interfaces
{
    public interface IDataStore
    {
        /// <summary>
        /// Get an object from the data store
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="key">Storage Key</param>
        /// <returns>Object of type T from Data Storage or null if no object found</returns>
        Task<T?> GetTAsync<T>(string key);

        /// <summary>
        /// Write an object to the data store
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="key">Storage Key</param>
        /// <param name="value">Value to write</param>
        /// <returns>Boolean indicator of if the write was successful</returns>
        Task<bool> WriteTAsync<T>(string key, T value);
    }
}