using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Order;

[MemoryDiagnoser]
[Orderer (SummaryOrderPolicy.FastestToSlowest)]
[RankColumn] 
public class FullCryptoBenchmarks
{
    
    private Aes aes;
    private RSA rsa;
    private ECDiffieHellman alice;
    private ECDiffieHellman bob;
    private ECDiffieHellmanPublicKey bobPublicKey;
    private byte[] dsaSignature;
    private byte[] AESdata;
    private byte[] RSAdata;
    private byte[] iv;
    
    
    // метрики под globalSetup не учитываются в бенчмарке
    [GlobalSetup]
    public void Setup()
    {
        //AES
        // настройка общих Aes параметров
        // подготовка данных которые будут тестироваться
        AESdata = new byte[1024*1024]; // средняя длина шифр-текста
        new Random().NextBytes(AESdata);
        aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        
        iv = aes.IV;
        
        
        //RSA
        //отдельно генерируем и импортируем ключи
        // подготовка данных которые будут тестироваться
        RSAdata = new byte[32]; // средняя длина шифр-текста
        new Random().NextBytes(RSAdata);
        rsa = RSA.Create(2048);
        RSAParameters publickey = rsa.ExportParameters(false);
        RSAParameters privatekey = rsa.ExportParameters(true);
        rsa.ImportParameters(privatekey);
        rsa.ImportParameters(publickey);
        
        
        //ECDH
        // подготовка данных которые будут тестироваться
        alice=ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        bob= ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        bobPublicKey = bob.PublicKey;
        alice.DeriveKeyFromHash(bobPublicKey, HashAlgorithmName.SHA256);
    }

    // тест AES только с шифрованием текста(без генерации ключей)
    [Benchmark (Description = "Aes Encrypt Only")]
    public byte[] AesEncryptOnly()
    {
        return aes.EncryptCbc(AESdata, iv);
    }
    
    // тест AES с шифрованием и генерацией ключей
    [Benchmark(Description = "Aes Encrypt With Key Generation")]
    public byte[] AesEncryptWithKeyGeneration()
    {
        aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        iv = aes.IV;
        return aes.EncryptCbc(AESdata, iv);
    }

    // тест RSA только с шифрованием AES ключа(32 байта)
    [Benchmark (Description = "Rsa Encrypt Only")]
    public byte[] RsaEncryptOnly()
    {
        return rsa.Encrypt(RSAdata, RSAEncryptionPadding.Pkcs1);
    }

    
    // тест RSA с шифрованием AES ключа(32 байта) + генерация RSA ключей
    [Benchmark(Description = "Rsa Encrypt With Key Generation")]
    public byte[] RsaEncryptWithKeyGeneration()
    {
        rsa = RSA.Create(2048);
        RSAParameters privatekey = rsa.ExportParameters(true);
        RSAParameters publickey = rsa.ExportParameters(false);
        rsa.ImportParameters(privatekey);
        rsa.ImportParameters(publickey);
        return rsa.Encrypt(RSAdata, RSAEncryptionPadding.Pkcs1);
    }


    [Benchmark (Description = "ECDH Encrypt Only")]
    public byte[] ECDHEncryptOnly()
    {
        // формируем общий секрет (можно использовать в гибридном алгоритме как ключ для AES)
        return alice.DeriveKeyFromHash(bobPublicKey, HashAlgorithmName.SHA256);
    }

    // тест ECDH используя старую кривую P-256
    [Benchmark (Description = "ECDH Encrypt With Key Generation")]
    public byte[] ECDHEncryptWithKeyGeneration()
    {
        alice=ECDiffieHellman.Create();
        bob= ECDiffieHellman.Create();
        bobPublicKey = bob.PublicKey;
        return alice.DeriveKeyFromHash(bobPublicKey, HashAlgorithmName.SHA256);
    }


    // гибридное шифрование AES+RSA без генерации пары ключей
    [Benchmark (Description = "HPKE AES+RSA Encrypt Only")]
    public byte[] HPKE_AES_RSA_EncryptOnly()
    {
        var aesKey = aes.Key;
        var aesIV = aes.IV;
        aes.EncryptCbc(AESdata, aesIV);
        
        return rsa.Encrypt(aesKey, RSAEncryptionPadding.Pkcs1);
    }

    
    // гибридное шифрование AES+ECDH с кривой nistP384 без генерации ключей
    [Benchmark (Description = "HPKE AES+ECDH Encrypt Only")]
    public byte[] HPKE_AES_ECDH_EncryptOnly()
    {
        var aesKey= alice.DeriveKeyFromHash(bobPublicKey, HashAlgorithmName.SHA256);
        aes.Key = aesKey;
        var aesIV = aes.IV;
        
        return aes.EncryptCbc(AESdata, aesIV);
    }
    

    //полный тест гибридного шифрования AES+RSA
    [Benchmark (Description = "HPKE AES+RSA Encrypt With Key Generation")]
    public byte[] HPKE_AES_RSA_EncryptWithKeyGeneration()
    {
        aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        iv = aes.IV;
        aes.EncryptCbc(AESdata, iv);
        
        
        rsa = RSA.Create(2048);
        
        RSAParameters privatekey = rsa.ExportParameters(true);
        RSAParameters publickey = rsa.ExportParameters(false);
        rsa.ImportParameters(privatekey);
        rsa.ImportParameters(publickey);
        
        return rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);
    }

    
    // полный тест гибридного шифрования AES+ECDH
    [Benchmark (Description = "HPKE AES+ECDH Encrypt With Key Generation")]
    public byte[] HPKE_AES_ECDH_EncryptWithKeyGeneration()
    {
        aes = Aes.Create();
        
        // меняем кривую
        alice=ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        bob= ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        bobPublicKey = bob.PublicKey;
        
        aes.Key=alice.DeriveKeyFromHash(bobPublicKey, HashAlgorithmName.SHA256);
        aes.GenerateIV();
        iv = aes.IV;
        
        
        return aes.EncryptCbc(AESdata, iv);
    }
}