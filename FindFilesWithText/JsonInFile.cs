using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UpdatePackages
{
    public static class JsonInFile
    {
        /// <summary>
        /// Сохранение данных в файл
        /// </summary>
        public static async Task<bool> SaveToFileAsync<T>(string FilePath, T data)
        {
            try
            {
                await using var file = File.Create(FilePath);
                await JsonSerializer.SerializeAsync(file, data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary> загрузка данных из файла </summary>
        public static async Task<T> LoadFromFile<T>(string FilePath)
        {
            if (!File.Exists(FilePath)) return default;
            try
            {
                using var file = File.OpenRead(FilePath);
                return await JsonSerializer.DeserializeAsync<T>(file);
            }
            catch (Exception)
            {
                return default;
            }
        }

    }
}