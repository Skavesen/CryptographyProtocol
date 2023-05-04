using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magistr
{
    // Класс для перемешивания ключа шифрования и аутентификации с помощью перестановок
    class KeyMixer
    {
        // Конструктор класса
        public KeyMixer(byte[] key)
        {
            // Ключ шифрования и аутентификации
            this.key = key;
            // Длина ключа в битах
            this.length = key.Length * 8;
        }

        // Метод для перемешивания ключа с помощью перестановок
        public void MixKey()
        {
            // Преобразуем ключ в битовый массив
            BitArray bits = new BitArray(key);

            // Для каждой четверти ключа выполняем перестановку битов внутри нее
            for (int i = 0; i < 4; i++)
            {
                // Вычисляем случайное число из четверти ключа
                int start = i * length / 4;
                int end = (i + 1) * length / 4 - 1;
                int random1 = GetRandomNumber(bits, start, end);

                // Выполняем перестановку битов внутри четверти ключа с помощью случайного числа
                PermuteBits(bits, start, end, random1);
            }

            // Для каждой половины ключа выполняем перестановку битов между ними
            for (int i = 0; i < 2; i++)
            {
                // Вычисляем случайное число из половины ключа
                int start = i * length / 2;
                int end = (i + 1) * length / 2 - 1;
                int random2 = GetRandomNumber(bits, start, end);

                // Выполняем перестановку битов между половинами ключа с помощью случайного числа
                PermuteBits(bits, start, end, random2);
            }

            // Для всего ключа выполняем перестановку битов внутри него
            int random3 = GetRandomNumber(bits, 0, length - 1);
            PermuteBits(bits, 0, length - 1, random3);

            // Преобразуем битовый массив обратно в ключ
            bits.CopyTo(key, 0);
        }

        // Метод для получения случайного числа из части битового массива
        private int GetRandomNumber(BitArray bits, int start, int end)
        {
            // Выбираем подмассив битов из заданного диапазона
            BitArray subarray = new BitArray(end - start + 1);
            for (int i = start; i <= end; i++)
            {
                subarray[i - start] = bits[i];
            }

            // Преобразуем подмассив битов в целое число
            byte[] bytes = new byte[(subarray.Length + 7) / 8];
            subarray.CopyTo(bytes, 0);

            // Указываем начальный индекс 0
            int number = BitConverter.ToInt32(bytes, 0);

            // Возвращаем абсолютное значение числа
            return Math.Abs(number);
        }

        // Метод для перестановки битов в части битового массива с помощью заданного числа
        private void PermuteBits(BitArray bits, int start, int end, int number)
        {
            // Вычисляем количество битов в части массива
            int count = end - start + 1;

            // Для каждого бита в части массива находим его новую позицию с помощью числа
            for (int i = start; i <= end; i++)
            {
                // Вычисляем новую позицию бита в пределах части массива
                int newPos = (i - start + number) % count;

                // Меняем местами биты в старой и новой позициях
                bool temp = bits[i];
                bits[i] = bits[start + newPos];
                bits[start + newPos] = temp;
            }
        }

        // Метод для разделения ключа на две части: одна для шифрования, а другая для аутентификации
        public (byte[] encryptionKey, byte[] authenticationKey) SplitKey()
        {
            // Создаем две части ключа равной длины
            byte[] encryptionKey = new byte[key.Length / 2];
            byte[] authenticationKey = new byte[key.Length / 2];

            // Копируем первую половину ключа в ключ шифрования
            Array.Copy(key, 0, encryptionKey, 0, key.Length / 2);

            // Копируем вторую половину ключа в ключ аутентификации
            Array.Copy(key, key.Length / 2, authenticationKey, 0, key.Length / 2);

            return (encryptionKey, authenticationKey);
        }

        // Поля класса
        private byte[] key; // Ключ шифрования и аутентификации
        private int length; // Длина ключа в битах
    }
}
