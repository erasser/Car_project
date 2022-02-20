using UnityEngine;
using static UnityEngine.GameObject;
using static TrackEditor;
using Button = UnityEngine.UI.Button;
using static OrbitCamera;
using static Part;

public class UiController
{
    public static GameObject uiTrackEditor;
    public static GameObject uiGame;
    public static GameObject partThumbnailsWrapper;

    public UiController()
    {
        var ui = Find("UI");
        uiGame = ui.transform.Find("UI_game").gameObject;
        uiTrackEditor = ui.transform.Find("UI_track_editor").gameObject;
        uiTrackEditor.transform.Find("buttonLoad").GetComponent<Button>().onClick.AddListener(DataManager.Load);
        uiTrackEditor.transform.Find("buttonSave").GetComponent<Button>().onClick.AddListener(DataManager.Save);
        uiTrackEditor.transform.Find("buttonToggleGridHelper").GetComponent<Button>().onClick.AddListener(Grid3D.ToggleGridHelper);
        uiTrackEditor.transform.Find("buttonGo!").GetComponent<Button>().onClick.AddListener(GameStateManager.Play);
        uiTrackEditor.transform.Find("buttonShowParts").GetComponent<Button>().onClick.AddListener(TogglePartsThumbnails);
        uiGame.transform.Find("buttonStop").GetComponent<Button>().onClick.AddListener(GameStateManager.Stop);
        uiGame.SetActive(false);
        partThumbnailsWrapper = uiTrackEditor.transform.Find("partThumbnailsWrapper").gameObject;
        partThumbnailsWrapper.SetActive(false);
        Find("arrowUp").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowUp"); });
        Find("arrowDown").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowDown"); });
        Find("arrowLeft").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowLeft"); });
        Find("arrowRight").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowRight"); });
        Find("arrowFront").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowFront"); });
        Find("arrowBack").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowBack"); });
        Find("buttonSelection").GetComponent<Button>().onClick.AddListener(ProcessSelectionButton);
    }

    void TogglePartsThumbnails()
    {
        partThumbnailsWrapper.SetActive(!partThumbnailsWrapper.activeSelf);
    }

    void ProcessSelectionButton()
    {
        if (selectedPart)
            TryUnselectPart();
        else
            SelectPart(GetPartAtCoords(selectionCubeCoords).gameObject);
    }

    /// <summary>
    ///     Aligns Y rotation with camera Y rotation.
    /// </summary>
    public static void Update3dUiTransform()
    {
        if (!orbitCamera.uiRotateHorizontalUiElement) return;

        var cameraLocalEulerAngles = cameraTargetTransform.localEulerAngles;
        orbitCamera.uiRotateHorizontalUiElement.transform.localEulerAngles = new(90 - cameraLocalEulerAngles.x, 0, cameraLocalEulerAngles.y);
        // orbitCamera.uiRotateHorizontalUiElement.transform.localEulerAngles.x, 0, _cameraTargetTransform.localEulerAngles.y);  // just for y rotation

        // This transforms vertical arrows too. Needs to have VerticalPanel with arrowDown and VerticalPanel (1) with arrowUp
        // var trans = GameObject.Find("VerticalPanel").transform;
        // var angles = trans.localEulerAngles;
        // angles.x = - cameraLocalEulerAngles.x;
        // trans.localEulerAngles = angles;
        // GameObject.Find("VerticalPanel (1)").transform.localEulerAngles = angles;

        // Used to rotate UP and DOWN arrows a bit. It works, but ruins onClick on down arrow, since it's backfaced to camera.
        // var trans = GameObject.Find("arrowUp").transform;
        // var angles = trans.localEulerAngles;
        // angles.x = - cameraLocalEulerAngles.x * .8f;
        // trans.localEulerAngles = angles;
        // var arrowDownTransform = GameObject.Find("arrowDown").transform;
        // arrowDownTransform.localEulerAngles = angles;
        // arrowDownTransform.Rotate(Vector3.right * 180 + Vector3.right * 20); // inefficient calculation and bad angle
    }
}
