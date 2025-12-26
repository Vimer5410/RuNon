window.VoiceChat = {
    connection: null,
    localStream: null,

    async joinRoom(dotNetRef) {
        console.log("[JS] Присоединяюсь к комнате...");

        try {
            this.localStream = await navigator.mediaDevices.getUserMedia({audio: true});
            console.log("[JS] Микрофон получен");

            await dotNetRef.invokeMethodAsync('JoinRoomOnServer');

        } catch (error) {
            console.error("[JS] Не удалось получить доступ к микрофону:", error);
            alert("Не удалось получить доступ к микрофону!");
        }
    },
    
    async leaveRoom() {
        console.log("[JS] Покидаю комнату");

        if (this.connection) {
            this.connection.close();
            delete this.connection;
        }

        if (this.localStream) {
            this.localStream.getTracks().forEach(track => {
                track.stop();
                console.log("[JS] Остановлен трек:", track.kind);
            });
            this.localStream = null;
        }

        const audio = document.getElementById('remote-audio');
        if (audio) {
            audio.remove();
            console.log("[JS] Audio элемент удалён");
        }
    },
    
    async createConnectionForUser(userId, dotNetRef) {
        console.log("[JS] Создание connection для", userId);

        const pc = new RTCPeerConnection({
            iceServers: [
                {urls: "stun:stun.l.google.com:19302"},
                {urls: "stun:stun1.l.google.com:19302"},

                {
                    urls: "turn:46.44.26.21:3478",
                    username: "voiceuser",
                    credential: "VoicePass123"

                },

                {
                    urls: "turn:46.44.26.21:3478?transport=tcp",
                    username: "voiceuser",
                    credential: "VoicePass123"
                }

            ],
            iceCandidatePoolSize: 10
        });

        this.localStream.getTracks().forEach(track => {
            pc.addTrack(track, this.localStream);
            console.log("[JS] Трек добавлен");
        });

        pc.ontrack = async (event) => {
            console.log("[JS]  Получен аудио трек от", userId);
            console.log("[JS] Stream:", event.streams[0]);
            console.log("[JS] Track:", event.track);

            let audio = document.getElementById('remote-audio');
            if (!audio) {
                audio = document.createElement('audio');
                audio.id = 'remote-audio';
                audio.autoplay = true;
                audio.controls = false;
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
            } catch (error) {
                console.error("[JS] Ошибка воспроизведения аудио:", error);
                alert("Нажмите OK чтобы разрешить воспроизведение аудио");
                try {
                    await audio.play();
                    console.log("[JS] Аудио воспроизводится после разрешения");
                } catch (e) {
                    console.error("[JS] Ошибка воспроизведения аудио:", e);
                }
            }
        };

        pc.onicecandidate = async (event) => {
            if (event.candidate) {
                console.log("[JS] ICE candidate:", event.candidate.type, event.candidate.protocol);
                await dotNetRef.invokeMethodAsync('SendIce', userId, JSON.stringify(event.candidate));
            } else {
                console.log("[JS] Все ICE кандидаты отправлены");
            }
        };

        pc.oniceconnectionstatechange = () => {
            console.log("[JS] ICE connection state:", pc.iceConnectionState);
        };

        pc.onconnectionstatechange = () => {
            console.log("[JS] Connection state:", pc.connectionState);
        };

        pc.onicegatheringstatechange = () => {
            console.log("[JS] ICE gathering state:", pc.iceGatheringState);
        };

        this.connection = pc;
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

        console.log("[JS] Отправка answer");
        await dotNetRef.invokeMethodAsync('SendAnswer', fromUserId, JSON.stringify(answer));
    },

    async handleAnswer(answerJson, fromUserId, dotNetRef) {
        console.log("[JS] Получен answer");

        const answer = JSON.parse(answerJson);

        if (this.connection) {
            await this.connection.setRemoteDescription(answer);
            console.debug("[JS] setRemoteDescription установлен");
        }
    },

    async handleIce(candidateJson, fromUserId, dotNetRef) {
        console.log("[JS] Получен ICE candidate");

        const candidate = JSON.parse(candidateJson);

        try {
            await this.connection.addIceCandidate(candidate);
            console.log("[JS] ICE candidate добавлен");
        } catch (e) {
            console.warn("[JS] Не удалось добавить ICE:", e.message);
        }
    },

    async handleUserLeft(userId) {
        console.log("[JS]  Пользователь вышел:", userId);

        if (this.connection) {
            this.connection.close();
            delete this.connection;
        }

        const audio = document.getElementById('remote-audio');
        if (audio) {
            audio.remove();
        }
    },


    async toggleMic(isMuted) {
        if (this.localStream) {
            this.localStream.getAudioTracks().forEach(track => {
                track.enabled = !isMuted;
                console.info("[JS] Микрофон", track.enabled ? "включен" : "выключен");
            });
        }
    },

    async setVolume(volume) {
        const audio = document.getElementById('remote-audio');
        if (audio) {
            audio.volume = volume;
            console.info("[JS] Громкость собеседника:", volume);
        }
    }
};