import { useSignalRGroup } from "./useSignalRGroup.js";

export function useSessionSubscription(sessionId, handlers = {}) {
    return useSignalRGroup("JoinSession", "LeaveSession", sessionId, handlers);
}
