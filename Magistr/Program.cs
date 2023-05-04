using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magistr
{
    class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            // Создаем экземпляр класса Server для представления сервера
            Server server = new Server();

            // Создаем два экземпляра класса Client для представления клиентов
            Client client1 = new Client("Alice");
            logger.Info($"Alice");
            Client client2 = new Client("Bob");
            logger.Info($"Bob");

            // Подключаем клиентов к серверу и обмениваемся ключами с ним
            client1.Connect(server);
            logger.Info($"client1.Connect");
            client2.Connect(server);
            logger.Info($"client2.Connect");

            // Отправляем сообщения между клиентами через сервер
            client1.SendMessage(server, "Hello, Bob!");
            logger.Info($"SendMessage Hello, Bob!");
            client2.SendMessage(server, "Hello, Alice!");
            logger.Info($"SendMessage Hello, Alice!");
            client1.SendMessage(server, "How are you?");
            logger.Info($"SendMessage How are you?");
            client2.SendMessage(server, "I'm fine, thank you. And you?");
            logger.Info($"SendMessage I'm fine, thank you. And you?");
            Console.ReadLine();
        }
    }
}
