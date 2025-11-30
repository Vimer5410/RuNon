using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Security.Cryptography;
using System.Text;

[MemoryDiagnoser]
[RankColumn] 
public class FullCryptoBenchmarks
{
    
    private Aes aes;
    private RSA rsa;
    private byte[] data;
    private byte[] iv;
    
    
    // метрики под globalSetup не учитываются в бенчмарке
    [GlobalSetup]
    public void Setup()
    {
        //AES
        // подготовка данных которые будут тестироваться
        data = new byte[200]; // средняя длина шифр-текста
        new Random().NextBytes(data);

        // настройка общих Aes параметров
        aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        
        iv = aes.IV;
        
        
        //RSA
        //отдельно генерируем и импортируем ключи
        rsa = RSA.Create(2048);
        RSAParameters publickey = rsa.ExportParameters(false);
        RSAParameters privatekey = rsa.ExportParameters(true);
        rsa.ImportParameters(privatekey);
        rsa.ImportParameters(publickey);
        
        
    }

    // тест AES только с шифрованием текста(без генерации ключей)
    [Benchmark (Description = "Aes Encrypt Only")]
    public byte[] AesEncryptOnly()
    {
        return aes.EncryptCbc(data, iv);
    }
    // тест AES с шифрованием и генерацией ключей
    [Benchmark(Description = "Aes Encrypt With Key Generation")]
    public byte[] AesEncryptWithKeyGeneration()
    {
        aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        iv = aes.IV;
        return aes.EncryptCbc(data, iv);
    }

    // тест RSA только с шифрованием ключа(поменять с 200 на 32 байта)
    [Benchmark (Description = "Rsa Encrypt Only")]
    public byte[] RsaEncryptOnly()
    {
        return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
    }

    // тест RSA c шифрованием и генерацией ключей(поменять с 200 на 32 байта)
    [Benchmark(Description = "Rsa Encrypt With Key Generation")]
    public byte[] RsaEncryptWithKeyGeneration()
    {
        rsa = RSA.Create(2048);
        RSAParameters privatekey = rsa.ExportParameters(true);
        RSAParameters publickey = rsa.ExportParameters(false);
        rsa.ImportParameters(privatekey);
        rsa.ImportParameters(publickey);
        return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
    }
    
}