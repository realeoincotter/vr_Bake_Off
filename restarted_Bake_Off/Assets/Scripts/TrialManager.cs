using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TrialManager : MonoBehaviour
{
    [SerializeField] XRSocketInteractor socket;
    [SerializeField] List<XRGrabInteractable> cubes;   // Cube_1 .. Cube_9, in arc order
    [SerializeField] Material targetMaterial;
    [SerializeField] Material distractorMaterial;
    [SerializeField] GameObject endPanel;              // shown when the block is complete
    [SerializeField] string variant = "A";
    [SerializeField] int trialsPerBlock = 10;
    [SerializeField] string menuScene = "MainMenu";

    const string DownloadsCopy = "/sdcard/Download/BakeOff_trials.csv";

    readonly List<Pose> homePoses = new();
    string participant;
    int trial;
    int target;
    float startTime;
    bool blockDone;

    void Awake()
    {
        foreach (var cube in cubes)
            homePoses.Add(new Pose(cube.transform.position, cube.transform.rotation));
    }

    void OnEnable() => socket.selectEntered.AddListener(OnPlaced);
    void OnDisable() => socket.selectEntered.RemoveListener(OnPlaced);

    void Start()
    {
        participant = Session.Participant;  // chosen in the main menu
        if (endPanel) endPanel.SetActive(false);
        NextTrial();
    }

    void NextTrial()
    {
        target = Random.Range(0, cubes.Count);
        for (int i = 0; i < cubes.Count; i++)
            cubes[i].GetComponent<Renderer>().sharedMaterial = i == target ? targetMaterial : distractorMaterial;
        startTime = Time.time;
    }

    void OnPlaced(SelectEnterEventArgs args)
    {
        if (blockDone) return;  // ignore anything placed after the block has ended

        var placed = args.interactableObject as XRGrabInteractable;
        int placedIndex = cubes.IndexOf(placed);
        float seconds = Time.time - startTime;
        int error = placedIndex == target ? 0 : 1;

        WriteRow($"{participant},{variant},{trial + 1},{target + 1},{placedIndex + 1},{seconds:F3},{error}\n");

        StartCoroutine(ResetCube(placed, placedIndex));
    }

    // App data folder (always) plus a copy in Downloads that MQDH's File Manager can see.
    void WriteRow(string row)
    {
        File.AppendAllText(Path.Combine(Application.persistentDataPath, "trials.csv"), row);

        if (Application.platform != RuntimePlatform.Android) return;
        try
        {
            File.AppendAllText(DownloadsCopy, row);
        }
        catch (System.Exception e)   // e.g. the file was created by an earlier install of the app
        {
            Debug.LogWarning($"Could not write to Downloads: {e.Message}");
        }
    }

    IEnumerator ResetCube(XRGrabInteractable cube, int index)
    {
        yield return null;  // let the socket finish its select before we release it
        socket.interactionManager.SelectExit(socket, (IXRSelectInteractable)cube);
        cube.transform.SetPositionAndRotation(homePoses[index].position, homePoses[index].rotation);

        trial++;
        if (trial < trialsPerBlock)
            NextTrial();
        else
            EndBlock();
    }

    void EndBlock()
    {
        blockDone = true;
        foreach (var cube in cubes)
        {
            cube.GetComponent<Renderer>().sharedMaterial = distractorMaterial;  // no target left highlighted
            cube.enabled = false;                                               // nothing can be grabbed
        }
        socket.socketActive = false;
        if (endPanel) endPanel.SetActive(true);
        Debug.Log($"Block {variant} complete for {participant}");
    }

    // Called by the Back to Main Menu button on the end panel.
    public void BackToMenu() => SceneManager.LoadScene(menuScene);
}