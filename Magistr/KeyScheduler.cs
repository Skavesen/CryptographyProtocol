using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Magistr
{
    // Класс для разделения ключа на временные интервалы и создания новых ключей для каждого сообщения
    class KeyScheduler
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        // Конструктор класса
        public KeyScheduler(byte[] key, int interval)
        {
            // Ключ шифрования и аутентификации
            this.key = key;
            // Временной интервал в секундах
            this.interval = interval;
            // Текущий индекс ключа
            this.index = 0;
            // Текущее время в секундах с начала эпохи Unix
            this.time = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        // Метод для получения текущего подключа из ключа шифрования и аутентификации
        public byte[] GetCurrentSubkey()
        {
            logger.Info($"+++++++++++++++++++++++++++++++++++++++++++++++++");
            logger.Info($"GetCurrentSubkey");
            // Вычисляем количество прошедших интервалов с начала эпохи Unix до текущего времени
            int intervals = time / interval;
            logger.Trace($"intervals = {intervals};");

            // Вычисляем длину подключа в байтах
            int subkeyLength = key.Length / 4;
            logger.Trace($"subkeyLength = {subkeyLength};");

            // Вычисляем начальную позицию подключа в массиве ключа шифрования и аутентификации
            int start = (index + intervals) % 4 * subkeyLength;
            logger.Trace($"start = {start};");

            // Создаем подключ из части массива ключа шифрования и аутентификации
            byte[] subkey = new byte[subkeyLength];
            Array.Copy(key, start, subkey, 0, subkeyLength);
            logger.Trace($"subkey = {BitConverter.ToString(subkey)};");

            return subkey;
        }

        // Метод для создания нового подключа из текущего подключа и обновления индекса и времени
        public void UpdateSubkey()
        {
            logger.Info($"+++++++++++++++++++++++++++++++++++++++++++++++++");
            logger.Info($"UpdateSubkey");
            byte[] subkey = GetCurrentSubkey();

            logger.Trace($"subkey = {BitConverter.ToString(subkey)};");

            byte[] hash;
            int subkeyLength = 32;
            //int subkeyLength = key.Length / 4;
            //Array.Resize(ref subkey, 32);
            logger.Trace($"!!subkeyLength = {subkeyLength};");
            // Создаем экземпляр класса SHA256Managed
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                // Вычисляем хэш-сумму от подключа с помощью алгоритма SHA-256
                hash = sha256.ComputeHash(subkey);
            }
            logger.Trace($"hash = {BitConverter.ToString(hash)};");
            logger.Trace($"!!hash.Length = {hash.Length};");

            /*if (hash.Length < subkeyLength)
            {
                throw new Exception("Hash length is too small.");
            }*/
            int start = index * subkeyLength;
            logger.Trace($"start = {start};");
            Array.Copy(hash, 0, key, start, subkeyLength);
            index = (index + 1) % 4;
            logger.Trace($"index = {index};");
            time = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            logger.Trace($"time = {time};");
        }

        // Поля класса
        private byte[] key; // Ключ шифрования и аутентификации
        private int interval; // Временной интервал в секундах
        private int index; // Текущий индекс ключа
        private int time; // Текущее время в секундах с начала эпохи Unix
    }
}
