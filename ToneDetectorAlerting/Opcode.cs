namespace ToneDetectorAlerting
{
    public enum Opcode
    {
        AUTH,
        AUTH_OK,
        AUTH_FAIL,
        AUTH_DUPE_NODE,
        TONE_REPORT,
        PING,
        PONG,
        DISCONNECT
    }
}