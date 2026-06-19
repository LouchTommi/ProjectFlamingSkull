using Unity.Cinemachine;
using UnityEngine;

public enum IntercatableType { HidingSpot, Pickup, Cassette, Other, Key, Generator, FuelCan, Basement }
public class Interactable : MonoBehaviour
{
    public IntercatableType intercatableType;
    public string interactableName;

    //HidingSpots
    public GameObject cameraObj;
    //Pickups
    public bool isBatteries;
    //Cassette and hiding spots
    public AudioClip audio;
    [TextArea]
    public string cassetteText;
}
