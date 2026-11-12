using UnityEngine;

// Shared by every scene for as long as the app is running.
public static class Session
{
    public static string Prefix = "P";        // "S" for simulator participants
    public static int ParticipantNumber = 1;
    public static string Participant => $"{Prefix}{ParticipantNumber:00}";
}