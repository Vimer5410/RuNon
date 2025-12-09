RuNon is an anonymous chat messenger built on modern Microsoft technologies (Blazor Server).

Key Features:

- Modern Cryptography: Utilizes a hybrid encryption scheme with cryptographically strong algorithms. 

- No Mandatory Registration: The server assigns each client a unique identifier not based on personal data or authentication.
  
- Modern Multi-Page UI: A contemporary and user-friendly web interface.
  
- Real-Time Communication: Features real-time messaging and an auto-reconnect system powered by SignalR. 🔁
  
- Content Moderation: Includes a message filtering and moderation system to prevent spam and censure inappropriate content.

(the current, simplified UI)
<img width="2546" height="1439" alt="image" src="https://github.com/user-attachments/assets/19dff6d2-8672-4292-9f80-f72d5046744c" />

---

✅ Implemented

- HPKE (Hybrid Public Key Encryption) integration.
    
- Storage of authorized and banned users in a database.
  
- Peer-to-Peer Connection: A system for pairing users built on the SignalR protocol.
  
- Unique User Identification: An identifier assignment system based on GUID + IP EndPoint.
  
- Ban & Feedback System: A robust ban system with feedback mechanisms for administrators, designed to prevent ban evasion.
  
- Adaptive UI: A responsive user interface (testing is not yet fully complete).

- Matchmaking Algorithm: With parameters for gender preference and age range.

- Voice Chat: Simple Voice Chat

---
(how the matchmaking algorithm works)
<img width="1280" height="835" alt="image" src="https://github.com/user-attachments/assets/1fe31898-00e4-43f4-8309-14d3fb1580d0" />

---

📅 Planned for Implementation
  
- Optional Registration: Adding sign-up with phone number or email binding (pending final approval).
  
- Voice Chat: Implementation of voice communication functionality (noise reduction, volume control, auto-reconnection system, etc.).
  
- Web-Based Admin Panel: An integrated administration panel within the web client for convenient moderation commands.
  
- Dedicated Info Pages: Creation of a separate page for rules of use and technical information.
  
- Cryptography Upgrade: Potential transition to a different, robust algorithm (e.g., ChaCha20 + Diffie-Hellman + Digital Signature).

---

(the final, intended ui, without backend)
<img width="2541" height="1440" alt="image" src="https://github.com/user-attachments/assets/bf341293-0193-4b0c-8dda-1bc0c7d02bde" />

---

# Benchmarks
```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.6 (9.0.6, 9.0.625.26613), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 9.0.6 (9.0.6, 9.0.625.26613), X64 RyuJIT x86-64-v3


```
| Method                                      | Mean         | Error        | StdDev       | Rank | Gen0     | Gen1     | Gen2     | Allocated |
|-------------------------------------------- |-------------:|-------------:|-------------:|-----:|---------:|---------:|---------:|----------:|
| &#39;Rsa Encrypt Only&#39;                          |     17.60 μs |     0.328 μs |     0.459 μs |    1 |   0.0305 |        - |        - |     280 B |
| &#39;ECDH Encrypt Only&#39;                         |  1,016.88 μs |    19.391 μs |    17.190 μs |    2 |        - |        - |        - |     777 B |
| &#39;HPKE AES+RSA Encrypt Only&#39;                 |  1,243.47 μs |     3.647 μs |     3.233 μs |    3 | 248.0469 | 248.0469 | 248.0469 | 1051355 B |
| &#39;Aes Encrypt With Key Generation&#39;           |  1,273.82 μs |    18.367 μs |    16.282 μs |    3 | 248.0469 | 248.0469 | 248.0469 | 1051410 B |
| &#39;Aes Encrypt Only&#39;                          |  1,343.73 μs |    23.611 μs |    20.931 μs |    4 | 248.0469 | 248.0469 | 248.0469 | 1051004 B |
| &#39;HPKE AES+ECDH Encrypt Only&#39;                |  2,270.10 μs |     4.946 μs |     4.385 μs |    5 | 246.0938 | 246.0938 | 246.0938 | 1052070 B |
| &#39;HPKE AES+ECDH Encrypt With Key Generation&#39; |  4,572.87 μs |    16.318 μs |    14.465 μs |    6 | 242.1875 | 242.1875 | 242.1875 | 1054138 B |
| &#39;ECDH Encrypt With Key Generation&#39;          |  5,849.60 μs |     7.692 μs |     6.424 μs |    7 |        - |        - |        - |    2893 B |
| &#39;HPKE AES+RSA Encrypt With Key Generation&#39;  | 62,924.36 μs | 1,844.895 μs | 5,381.644 μs |    8 | 125.0000 | 125.0000 | 125.0000 | 1052606 B |
| &#39;Rsa Encrypt With Key Generation&#39;           | 63,014.30 μs | 1,659.558 μs | 4,814.679 μs |    8 |        - |        - |        - |    2144 B |


