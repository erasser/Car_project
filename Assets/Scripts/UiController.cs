using UnityEngine;
using static UnityEngine.GameObject;
using static TrackEditor;
using Button = UnityEngine.UI.Button;

public class UiController
{
    public static GameObject ui;
    public static GameObject uiTrackEditor;
    public static GameObject uiGame;

    public UiController()
    {
        ui = Find("UI");
        uiGame = ui.transform.Find("UI_game").gameObject;
        uiTrackEditor = ui.transform.Find("UI_track_editor").gameObject;
        uiTrackEditor.transform.Find("buttonLoad").GetComponent<Button>().onClick.AddListener(DataManager.Load);
        uiTrackEditor.transform.Find("buttonSave").GetComponent<Button>().onClick.AddListener(DataManager.Save);
        uiTrackEditor.transform.Find("buttonToggleGridHelper").GetComponent<Button>().onClick.AddListener(Grid3D.ToggleGridHelper);
        uiTrackEditor.transform.Find("Go!").GetComponent<Button>().onClick.AddListener(GameStateManager.Play);
        uiGame.transform.Find("Stop").GetComponent<Button>().onClick.AddListener(GameStateManager.Stop);
        uiGame.SetActive(false);
        Find("arrowUp").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowUp"); });
        Find("arrowDown").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowDown"); });
        Find("arrowLeft").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowLeft"); });
        Find("arrowRight").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowRight"); });
        Find("arrowFront").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowFront"); });
        Find("arrowBack").GetComponent<Button>().onClick.AddListener( delegate { MoveSelection("arrowBackward"); });
    }
}
