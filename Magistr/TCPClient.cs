using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Magistr
{
    // Класс для работы с TCP
    public class TCPClient
    {
        // Поле для хранения сокета клиента
        private Socket clientSocket;

        // Конструктор класса, принимает адрес и порт сервера
        public TCPClient(string serverAddress, int serverPort)
        {
            // Создаем сокет клиента с протоколом TCP
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Преобразуем адрес сервера в объект IPAddress
            IPAddress serverIP = IPAddress.Parse(serverAddress);

            // Создаем объект IPEndPoint с адресом и портом сервера
            IPEndPoint serverEndPoint = new IPEndPoint(serverIP, serverPort);

            // Подключаемся к серверу
            clientSocket.Connect(serverEndPoint);
        }

        // Метод для отправки сообщения серверу
        public void SendMessage(string message)
        {
            // Преобразуем сообщение в массив байтов
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // Отправляем сообщение серверу
            clientSocket.Send(messageBytes);
        }

        // Метод для получения сообщения от сервера
        public string ReceiveMessage()
        {
            // Создаем буфер для хранения данных
            byte[] buffer = new byte[1024];

            // Получаем количество байтов, принятых от сервера
            int receivedBytes = clientSocket.Receive(buffer);

            // Преобразуем байты в строку
            string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

            // Возвращаем сообщение
            return message;
        }

        // Метод для закрытия соединения с сервером
        public void Close()
        {
            // Закрываем сокет клиента
            clientSocket.Close();
        }
    }
}
