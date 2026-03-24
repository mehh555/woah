import { useSignalRGroup } from "./useSignalRGroup.js";

export function useLobbySubscription(lobbyCode, handlers = {}) {
    return useSignalRGroup("JoinLobby", "LeaveLobby", lobbyCode, handlers);
}
