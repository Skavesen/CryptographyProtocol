using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Magistr
{
    // Класс для представления клиента, который может отправлять и принимать сообщения
    class Client
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        // Адрес сервера
        private const string ServerAddress = "127.0.0.1";

        // Порт сервера
        private const int ServerPort = 8888;

        // Сокет для соединения с сервером
        private Socket socket;

        // Поток для чтения данных от сервера
        private NetworkStream stream;

        // Конструктор класса
        public Client(string name)
        {
            // Имя клиента
            this.name = name;
            // Экземпляр класса DiffieHellman для обмена ключами с сервером
            this.dh = new DiffieHellman();
            // Экземпляр класса Aes для шифрования и расшифрования сообщений
            this.aes = new AesManaged();
            // Экземпляр класса HMACSHA256 для аутентификации сообщений
            this.hmac = new HMACSHA256();
            // Экземпляр класса KeyScheduler для разделения ключа на временные интервалы и создания новых ключей для каждого сообщения
            this.ks = null;

            // Создаем сокет для соединения с сервером
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Подключаемся к серверу по адресу и порту
            socket.Connect(ServerAddress, ServerPort);

            // Создаем поток для чтения данных от сервера
            stream = new NetworkStream(socket);
        }

        // Метод для подключения к серверу и обмена ключами с ним
        public void Connect(Server server)
        {
            logger.Info($"------------------------------------------");
            logger.Info($"Connect");
            // Отправляем свой открытый ключ серверу и получаем его открытый ключ
            byte[] serverPublicKey = server.ExchangeKeys(dh.GetPublicKey());
            logger.Trace($"serverPublicKey = {BitConverter.ToString(serverPublicKey)};");

            // Вычисляем общий секретный ключ с помощью открытого ключа сервера
            byte[] sharedSecret = dh.GetSharedSecret(serverPublicKey);
            logger.Trace($"sharedSecret = {BitConverter.ToString(sharedSecret)};");

            // Генерируем ключ шифрования и аутентификации с помощью дерева Меркла
            MerkleTree mt = new MerkleTree(4);
            byte[] key = mt.GenerateKey();
            logger.Trace($"key = {BitConverter.ToString(key)};");

            // Перемешиваем ключ шифрования и аутентификации с помощью перестановок и общего секретного ключа
            KeyMixer km = new KeyMixer(key);
            km.MixKey();

            // Разделяем ключ шифрования и аутентификации на две части
            (byte[] encryptionKey, byte[] authenticationKey) = km.SplitKey();

            logger.Trace($"encryptionKey = {BitConverter.ToString(encryptionKey)};");
            logger.Trace($"authenticationKey = {BitConverter.ToString(authenticationKey)};");

            logger.Trace($"Resize");
            // Устанавливаем ключ шифрования для алгоритма AES
            Array.Resize(ref encryptionKey, 32);
            Array.Resize(ref authenticationKey, 16);

            logger.Trace($"encryptionKey = {BitConverter.ToString(encryptionKey)};");
            logger.Trace($"authenticationKey = {BitConverter.ToString(authenticationKey)};");

            aes.Key = encryptionKey;
            // Устанавливаем ключ аутентификации для алгоритма HMACSHA256
            hmac.Key = authenticationKey;

            // Создаем экземпляр класса KeyScheduler для разделения ключа на временные интервалы и создания новых ключей для каждого сообщения
            ks = new KeyScheduler(key, 10);

            // Выводим сообщение о подключении к серверу
            Console.WriteLine($"{name} connected to server.");

            logger.Trace($"{name} connected to server.");
        }
        // Метод для отправки сообщения серверу
        public void SendMessage(Server server, string message)
        {
            logger.Info($"------------------------------------------");
            logger.Info($"SendMessage");

            // Получаем текущий подключ из ключа шифрования и аутентификации
            byte[] subkey = ks.GetCurrentSubkey();

            logger.Trace($"subkey = {BitConverter.ToString(subkey)};");

            GlobalVariables.subkey2 = subkey;

            // Генерируем случайный вектор инициализации для алгоритма AES
            byte[] iv = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }

            logger.Trace($"iv = {BitConverter.ToString(iv)};");

            // Устанавливаем вектор инициализации для алгоритма AES
            Array.Resize(ref subkey, 32);
            aes.Key = subkey;
            aes.IV = iv;

            logger.Trace($"Resize");
            logger.Trace($"subkey = {BitConverter.ToString(subkey)};");
            logger.Trace($"iv = {BitConverter.ToString(iv)};");

            // Шифруем сообщение с помощью алгоритма AES
            byte[] encryptedMessage;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                encryptedMessage = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(message), 0, message.Length);
            }

            logger.Trace($"encryptedMessage = {BitConverter.ToString(encryptedMessage)};");

            // Вычисляем хэш-сумму от сообщения с помощью алгоритма HMACSHA256
            byte[] hash;
            using (HMACSHA256 hmac = new HMACSHA256(subkey))
            {
                hash = hmac.ComputeHash(encryptedMessage);
            }

            logger.Trace($"hash = {BitConverter.ToString(hash)};");

            // Объединяем вектор инициализации, зашифрованное сообщение и хэш-сумму в один массив байтов
            byte[] data = iv.Concat(encryptedMessage).Concat(hash).ToArray();

            logger.Trace($"data = {BitConverter.ToString(data)};");

            // Отправляем массив байтов серверу
            server.ReceiveMessage(this, data);

            // Создаем новый подключ из текущего подключа и обновляем индекс и время
            ks.UpdateSubkey();

            // Выводим сообщение об отправке сообщения
            Console.WriteLine($"{name} sent message: {message}");

            logger.Trace($"{name} sent message.: {message}");
        }

        // Метод для приема сообщения от сервера
        public void ReceiveMessage(Server server, byte[] data)
        {
            logger.Info($"------------------------------------------");
            logger.Info($"ReceiveMessage");
            // Получаем текущий подключ из ключа шифрования и аутентификации
            byte[] subkey = GlobalVariables.subkey2;//но не должно так работать
            //byte[] subkey = ks.GetCurrentSubkey();
            logger.Trace($"subkey = {BitConverter.ToString(subkey)};");

            // Разделяем массив байтов на вектор инициализации, зашифрованное сообщение и хэш-сумму
            byte[] iv = data.Take(16).ToArray();
            byte[] encryptedMessage = data.Skip(16).Take(data.Length - 48).ToArray();
            byte[] hash = data.Skip(data.Length - 32).ToArray();
            logger.Trace($"iv = {BitConverter.ToString(iv)};");
            logger.Trace($"encryptedMessage = {BitConverter.ToString(encryptedMessage)};");
            logger.Trace($"hash = {BitConverter.ToString(hash)};");

            // Устанавливаем вектор инициализации для алгоритма AES
            Array.Resize(ref subkey, 32);
            Array.Resize(ref iv, 16);
            aes.Key = subkey;
            aes.IV = iv;
            //aes.Padding = PaddingMode.None;

            logger.Trace($"Resize");
            logger.Trace($"subkey = {BitConverter.ToString(subkey)};");
            logger.Trace($"iv = {BitConverter.ToString(iv)};");

            // Расшифровываем сообщение с помощью алгоритма AES
            string message;
            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                // Не преобразуем данные в строку, а оставляем в виде массива байтов
                byte[] decryptedMessage = decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);

                logger.Trace($"decryptedMessage = {BitConverter.ToString(decryptedMessage)};");

                // Преобразуем данные в строку только после расшифровки
                message = Encoding.UTF8.GetString(decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length));
                logger.Trace($"message = {message}");
            }

            // Вычисляем хэш-сумму от сообщения с помощью алгоритма HMACSHA256
            byte[] computedHash;
            using (HMACSHA256 hmac = new HMACSHA256(subkey))
            {
                computedHash = hmac.ComputeHash(encryptedMessage);
                logger.Trace($"computedHash = {BitConverter.ToString(computedHash)};");
            }

            // Сравниваем полученную хэш-сумму с вычисленной хэш-суммой
            if (hash.SequenceEqual(computedHash))
            {
                // Выводим сообщение о получении сообщения
                Console.WriteLine($"{name} received message");
            }
            else
            {
                // Выводим сообщение об ошибке аутентификации
                Console.WriteLine($"{name} received invalid message.");
            }

            // Создаем новый подключ из текущего подключа и обновляем индекс и время
            ks.UpdateSubkey();
        }

        // Поля класса
        private string name; // Имя клиента
        private DiffieHellman dh; // Экземпляр класса DiffieHellman для обмена ключами с сервером
        private AesManaged aes; // Экземпляр класса Aes для шифрования и расшифрования сообщений
        private HMACSHA256 hmac; // Экземпляр класса HMACSHA256 для аутентификации сообщений
        private KeyScheduler ks; // Экземпляр класса KeyScheduler для разделения ключа на временные интервалы и создания новых ключей для каждого сообщения

        // Создаем свойства с публичными геттерами и сеттерами
        public KeyScheduler KS { get => ks; set => ks = value; }
        public HMACSHA256 HMAC { get => hmac; set => hmac = value; }
        public AesManaged AES { get => aes; set => aes = value; }
    }
    public static class GlobalVariables
    {
        // Объявляем статическую переменную subkey2
        public static byte[] subkey2;
        // Объявляем другую статическую переменную для примера
        public static string message;
    }
}
