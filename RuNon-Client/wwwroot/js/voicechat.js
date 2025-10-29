window.VoiceChat = {
    connections: {},
    localStream: null,

    async joinRoom(dotNetRef) {
        console.log("[JS] Присоединяюсь к комнате...");

        try {
            this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true });
            console.log("[JS] Микрофон получен");

            await dotNetRef.invokeMethodAsync('JoinRoomOnServer');

        } catch (error) {
            console.error("[JS] ОШИБКА получения микрофона:", error);
            alert("Не удалось получить доступ к микрофону!");
        }
    },

    async createConnectionForUser(userId, dotNetRef) {
        console.log("[JS] Создание connection для", userId);
        
        
        
        
        const pc = new RTCPeerConnection({
            iceServers: [
                { urls: "stun:stun.l.google.com:19302" },
                { urls: "stun:stun1.l.google.com:19302" },

                // Twilio 
                {
                    urls: "turn:global.turn.twilio.com:3478?transport=udp",
                    username: "",
                    credential: ""
                },

                // Numb
                {
                    urls: "turn:numb.viagenie.ca",
                    username: "webrtc@live.com",
                    credential: "muazkh"
                },
                
                
            ],
            iceCandidatePoolSize: 10
        });

        this.localStream.getTracks().forEach(track => {
            pc.addTrack(track, this.localStream);
            console.log("[JS] Трек добавлен:", track.kind);
        });

        pc.ontrack = async (event) => {
            console.log("[JS]  Получен аудио трек от", userId);
            console.log("[JS] Stream:", event.streams[0]);
            console.log("[JS] Track:", event.track);

            let audio = document.getElementById('remote-audio-' + userId);
            if (!audio) {
                audio = document.createElement('audio');
                audio.id = 'remote-audio-' + userId;
                audio.autoplay = true;
                audio.controls = true;
                audio.volume = 1.0;
                document.body.appendChild(audio);
                console.log("[JS] Audio элемент создан и добавлен в DOM");
            }

            audio.srcObject = event.streams[0];

            event.streams[0].getTracks().forEach(track => {
                console.log("[JS] Track state:", track.kind, "enabled:", track.enabled, "readyState:", track.readyState, "muted:", track.muted);
            });

            try {
                await audio.play();
                console.log("[JS] ✅ Аудио ВОСПРОИЗВОДИТСЯ!");
            } catch (error) {
                console.error("[JS] ❌ ОШИБКА воспроизведения:", error);
                alert("Нажмите OK чтобы разрешить воспроизведение аудио");
                try {
                    await audio.play();
                    console.log("[JS] ✅ Аудио воспроизводится после разрешения");
                } catch (e) {
                    console.error("[JS] ❌ Всё ещё не работает:", e);
                }
            }
        };

        pc.onicecandidate = async (event) => {
            if (event.candidate) {
                console.log("[JS] 📡 ICE candidate:", event.candidate.type, event.candidate.protocol);
                await dotNetRef.invokeMethodAsync('SendIce', userId, JSON.stringify(event.candidate));
            } else {
                console.log("[JS] ✅ Все ICE candidates отправлены");
            }
        };

        pc.oniceconnectionstatechange = () => {
            console.log("[JS] 🔌 ICE connection state:", pc.iceConnectionState);
            if (pc.iceConnectionState === 'failed') {
                console.error("[JS] ❌ ICE соединение провалилось! Возможно нужен TURN сервер.");
            }
            if (pc.iceConnectionState === 'connected' || pc.iceConnectionState === 'completed') {
                console.log("[JS] ✅ ICE соединение установлено!");
            }
        };

        pc.onconnectionstatechange = () => {
            console.log("[JS] 🔗 Connection state:", pc.connectionState);
            if (pc.connectionState === 'failed') {
                console.error("[JS] ❌ WebRTC соединение провалилось!");
            }
            if (pc.connectionState === 'connected') {
                console.log("[JS] ✅ WebRTC соединение установлено!");
            }
        };
        
        pc.onicegatheringstatechange = () => {
            console.log("[JS] ICE gathering state:", pc.iceGatheringState);
        };

        this.connections[userId] = pc;
        return pc;
    },

    async handleUserJoined(userId, dotNetRef) {
        console.log("[JS] Новый пользователь:", userId);

        const pc = await this.createConnectionForUser(userId, dotNetRef);
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);

        console.log("[JS] Отправка offer новому пользователю");
        await dotNetRef.invokeMethodAsync('SendOfferToRoom', JSON.stringify(offer));
    },

    async handleOffer(offerJson, fromUserId, dotNetRef) {
        console.log("[JS] Получен offer от", fromUserId);

        const offer = JSON.parse(offerJson);
        const pc = await this.createConnectionForUser(fromUserId, dotNetRef);

        await pc.setRemoteDescription(offer);
        const answer = await pc.createAnswer();
        await pc.setLocalDescription(answer);

        console.log("[JS] 📤 Отправка answer");
        await dotNetRef.invokeMethodAsync('SendAnswer', fromUserId, JSON.stringify(answer));
    },

    async handleAnswer(answerJson, fromUserId, dotNetRef) {
        console.log("[JS] 📥 Получен answer");

        const answer = JSON.parse(answerJson);

        const connections = Object.values(this.connections);
        if (connections.length > 0) {
            await connections[0].setRemoteDescription(answer);
            console.log("[JS] ✅ Answer установлен");
        }
    },

    async handleIce(candidateJson, fromUserId, dotNetRef) {
        console.log("[JS] Получен ICE candidate");

        const candidate = JSON.parse(candidateJson);

        for (const pc of Object.values(this.connections)) {
            try {
                await pc.addIceCandidate(candidate);
                console.log("[JS] ✅ ICE candidate добавлен");
            } catch (e) {
                console.log("[JS] ⚠️ Не удалось добавить ICE:", e.message);
            }
        }
    },

    async handleUserLeft(userId) {
        console.log("[JS]  Пользователь вышел:", userId);

        if (this.connections[userId]) {
            this.connections[userId].close();
            delete this.connections[userId];
        }

        const audio = document.getElementById('remote-audio-' + userId);
        if (audio) {
            audio.remove();
        }
    }
};