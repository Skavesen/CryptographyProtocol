using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Magistr
{
    // Класс для представления сервера, который принимает входящие соединения и отправляет сообщения другим клиентам
    class Server
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        // Сокет сервера
        TCPClient client;

        // Конструктор класса
        public Server()
        {
            // Создаем объект TCPClient с адресом 127.0.0.1 и портом 8080
            client = new TCPClient("127.0.0.1", 8080);

            // Список подключенных клиентов
            this.clients = new List<Client>();
        }

        // Метод для обмена ключами с клиентом и добавления его в список подключенных клиентов
        public byte[] ExchangeKeys(byte[] clientPublicKey)
        {
            logger.Info($"------------------------------------------");
            logger.Info($"ExchangeKeys");
            // Создаем экземпляр класса DiffieHellman для обмена ключами с клиентом
            DiffieHellman dh = new DiffieHellman();

            // Получаем свой открытый ключ и отправляем его клиенту
            byte[] serverPublicKey = dh.GetPublicKey();

            // Вычисляем общий секретный ключ с помощью открытого ключа клиента
            byte[] sharedSecret = dh.GetSharedSecret(clientPublicKey);

            // Генерируем ключ шифрования и аутентификации с помощью дерева Меркла
            MerkleTree mt = new MerkleTree(4);
            byte[] key = mt.GenerateKey();

            // Перемешиваем ключ шифрования и аутентификации с помощью перестановок и общего секретного ключа
            KeyMixer km = new KeyMixer(key);
            km.MixKey();

            // Разделяем ключ шифрования и аутентификации на две части
            (byte[] encryptionKey, byte[] authenticationKey) = km.SplitKey();

            // Создаем экземпляр класса Client для представления подключенного клиента
            Client client = new Client("Client" + (clients.Count + 1));

            // Устанавливаем ключ шифрования для алгоритма AES
            // Изменяем размеры массивов для ключа и вектора
            Array.Resize(ref encryptionKey, 32);
            Array.Resize(ref authenticationKey, 16);
            client.AES.Key = encryptionKey;

            logger.Trace($"client.AES.Key = {BitConverter.ToString(encryptionKey)};");

            // Устанавливаем ключ аутентификации для алгоритма HMACSHA256
            client.HMAC.Key = authenticationKey;

            logger.Trace($"client.HMAC.Key = {BitConverter.ToString(authenticationKey)};");

            // Создаем экземпляр класса KeyScheduler для разделения ключа на временные интервалы и создания новых ключей для каждого сообщения
            client.KS = new KeyScheduler(key, 10);

            // Добавляем клиента в список подключенных клиентов
            clients.Add(client);
            logger.Trace($"client = {clients.Count};");

            // Возвращаем свой открытый ключ клиенту
            return serverPublicKey;
        }

        // Метод для приема сообщения от клиента и отправки его другим клиентам
        public void ReceiveMessage(Client sender, byte[] data)
        {
            // Для каждого клиента в списке подключенных клиентов, кроме отправителя
            foreach (Client client in clients)
            {
                if (client != sender)
                {
                    // Отправляем сообщение клиенту
                    client.ReceiveMessage(this, data);
                }
            }
        }

        // Поля класса
        private List<Client> clients; // Список подключенных клиентов
    }
}
