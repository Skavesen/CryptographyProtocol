using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Magistr
{
    // Класс для обмена ключами между клиентом и сервером, используя решения задачи Диффи-Хеллмана
    class DiffieHellman
    {
        // Конструктор класса
        public DiffieHellman()
        {
            // Создаем экземпляр класса ECDiffieHellmanCng для генерации пары ключей
            this.ecdh = new ECDiffieHellmanCng();
            // Устанавливаем кривую для генерации ключей
            this.ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            this.ecdh.HashAlgorithm = CngAlgorithm.Sha256;
        }

        // Метод для получения открытого ключа
        public byte[] GetPublicKey()
        {
            // Возвращаем открытый ключ в формате DER
            return ecdh.PublicKey.ToByteArray();
        }

        // Метод для получения общего секрета с другой стороной
        public byte[] GetSharedSecret(byte[] otherPublicKey)
        {
            // Преобразуем открытый ключ другой стороны в объект CngKey
            CngKey key = CngKey.Import(otherPublicKey, CngKeyBlobFormat.EccPublicBlob);

            // Вычисляем общий секрет с помощью метода DeriveKeyMaterial
            return ecdh.DeriveKeyMaterial(key);
        }

        // Поле класса
        private ECDiffieHellmanCng ecdh; // Экземпляр класса ECDiffieHellmanCng для генерации пары ключей
    }
}
