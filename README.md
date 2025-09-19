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

---

📅 Planned for Implementation

- Matchmaking Algorithm: With parameters for gender preference and age range.
  
- Optional Registration: Adding sign-up with phone number or email binding (pending final approval).
  
- Voice Chat: Implementation of voice communication functionality.
  
- Web-Based Admin Panel: An integrated administration panel within the web client for convenient moderation commands.
  
- Dedicated Info Pages: Creation of a separate page for rules of use and technical information.
  
- Cryptography Upgrade: Potential transition to a different, robust algorithm (e.g., ChaCha20 + Diffie-Hellman + Digital Signature).

---

(the final, intended ui, without backend)
<img width="2541" height="1440" alt="image" src="https://github.com/user-attachments/assets/bf341293-0193-4b0c-8dda-1bc0c7d02bde" />

