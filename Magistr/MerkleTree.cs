using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Magistr
{
    class MerkleTree
    {
        // Конструктор класса
        public MerkleTree(int depth)
        {
            // Глубина дерева
            this.depth = depth;
            // Количество листовых узлов
            this.leaves = (int)Math.Pow(2, depth);
            // Массив для хранения хэш-сумм узлов дерева
            this.nodes = new byte[2 * leaves - 1][];
            // Генератор случайных чисел
            this.rng = new RNGCryptoServiceProvider();
        }

        // Метод для генерации ключа шифрования и аутентификации
        public byte[] GenerateKey()
        {
            // Генерируем случайные значения для листовых узлов дерева
            for (int i = 0; i < leaves; i++)
            {
                nodes[leaves - 1 + i] = new byte[32];
                rng.GetBytes(nodes[leaves - 1 + i]);
            }

            // Вычисляем хэш-суммы родительских узлов дерева
            for (int i = leaves - 2; i >= 0; i--)
            {
                // Создаем экземпляр класса SHA256Managed
                using (SHA256Managed sha256 = new SHA256Managed())
                {
                    // Вычисляем хэш-сумму от объединения дочерних узлов
                    nodes[i] = sha256.ComputeHash(nodes[2 * i + 1].Concat(nodes[2 * i + 2]).ToArray());
                }
            }

            // Объединяем хэш-суммы родительских узлов дерева, начиная от корневого узла и заканчивая листовым узлом
            byte[] key = new byte[32 * (depth + 1)];
            int index = 0;
            int node = 0;
            while (index < key.Length)
            {
                Array.Copy(nodes[node], 0, key, index, 32);
                index += 32;
                node = 2 * node + 1; // Переходим к левому дочернему узлу
            }

            return key;
        }

        // Поля класса
        private int depth; // Глубина дерева
        private int leaves; // Количество листовых узлов
        private byte[][] nodes; // Массив для хранения хэш-сумм узлов дерева
        private RNGCryptoServiceProvider rng; // Генератор случайных чисел
    }
}
