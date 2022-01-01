using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// https://forum.unity.com/threads/vector3-is-not-marked-serializable.435303

struct DataManager
{
    static readonly string SaveFile = Application.persistentDataPath + "/saveData.bin";
    static readonly BinaryFormatter MyBinaryFormatter = new ();

    public static void Save()
    {
        // var settings = new JsonSerializerSettings
        // {
        //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        // };
        // Debug.Log(JsonConvert.SerializeObject(new Data(), settings));

        // JsonSerializer serializer = new JsonSerializer();
        // serializer.NullValueHandling = NullValueHandling.Ignore;
        //
        // using (StreamWriter sw = new StreamWriter(@"c:\temp\json.txt"))
        // using (JsonWriter writer = new JsonTextWriter(sw))
        // {
        //     serializer.Serialize(writer, new Coord());
        // }
        
        // return;
        
        // TODO: Check if there's anything to save
        
        // if (!Grid3D.IsValid())
        if (!TrackEditor.canTransformBeApplied)
        {
            Debug.Log("not valid, not saving");
            // TODO: Show message to user
            return;
        }

        TrackData trackData = new TrackData();
        
        if (trackData.partsSaveData.Count == 0)
        {
            Debug.Log("track is empty, not saving");
            // TODO: Show message to user
            return;
        }
        
        FileStream file = File.Create(SaveFile);
        MyBinaryFormatter.Serialize(file, trackData);
        file.Close();
        Debug.Log(JsonConvert.SerializeObject(new TrackData()));
        // TODO: Show message to user
    }

    public static void Load()
    {
        if (!File.Exists(SaveFile))
        {
            // TODO: Show message to user
            Debug.Log($"Save file {SaveFile} not found.");
            return;
        }
        
        FileStream file = File.Open(SaveFile, FileMode.Open);
        TrackData trackData = (TrackData)MyBinaryFormatter.Deserialize(file);
        file.Close();

        TrackEditor.GenerateLoadedTrack(trackData.partsSaveData);
        
        // TODO: Show message to user
        Debug.Log("Game data loaded!");
        
        // Get part from prefabs by tag
    }
}

[Serializable]
public class TrackData
{
    // public List<GridCubeSaveData> gridCubesData;
    public List<PartSaveData> partsSaveData;

    /*  To save:  */
    // List of GridCubes, which are occupied (parts.Count = 1)  // Not used, all data is in partsSaveData
    //      • coordinates
    //      _parts will be filled from ↓
    // List of parts
    //      • occupiedGridCubes
    //      • _rotation
    //      • tag
    // TODO: Camera information?

    public TrackData()
    {
        // gridCubesData = Grid3D.GetPartsSaveData();
        partsSaveData = Part.GetPartsSaveData();
    }
}